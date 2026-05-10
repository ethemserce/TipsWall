using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksMatchFactsWriter : ISportMonksMatchFactsWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksMatchFactsWriter> _logger;

        public SportMonksMatchFactsWriter(
            IConfiguration configuration,
            ILogger<SportMonksMatchFactsWriter> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertMatchFactsForFixtureAsync(
            long fixtureId,
            IEnumerable<MatchFact> matchFacts,
            CancellationToken cancellationToken = default)
        {
            var rows = matchFacts
                .Where(m => m != null && m.Id > 0)
                .GroupBy(m => m.Id)
                .Select(g => g.Last())
                .ToList();

            if (rows.Count == 0)
                return;

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var row in rows)
                {
                    await UpsertMatchFactAsync(connection, transaction, row, fixtureId, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {Count} SportMonks match facts for fixture {FixtureId}.",
                    rows.Count,
                    fixtureId);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task UpsertMatchFactAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            MatchFact fact,
            long fixtureId,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_match_facts (
                    id,
                    fixture_id,
                    sport_id,
                    type_id,
                    participant,
                    basis,
                    data,
                    natural_language,
                    category,
                    scope,
                    captured_at,
                    last_synced_at)
                values (
                    @id,
                    @fixture_id,
                    (select id from catalog.sports where id = @sport_id),
                    (select id from catalog.types where id = @type_id),
                    @participant,
                    @basis,
                    @data::jsonb,
                    @natural_language,
                    @category,
                    @scope,
                    now(),
                    now())
                on conflict (id) do update set
                    fixture_id = excluded.fixture_id,
                    sport_id = excluded.sport_id,
                    type_id = excluded.type_id,
                    participant = excluded.participant,
                    basis = excluded.basis,
                    data = excluded.data,
                    natural_language = excluded.natural_language,
                    category = excluded.category,
                    scope = excluded.scope,
                    captured_at = now(),
                    last_synced_at = now(),
                    updated_at = now();
                """;

            var sportId = fact.SportId.HasValue && fact.SportId.Value > 0
                ? (object)fact.SportId.Value
                : DBNull.Value;
            var typeId = fact.TypeId.HasValue && fact.TypeId.Value > 0
                ? (object)fact.TypeId.Value
                : DBNull.Value;
            var dataJson = fact.Data != null
                ? fact.Data.ToString(Formatting.None)
                : null;

            command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Bigint) { Value = fact.Id });
            command.Parameters.Add(new NpgsqlParameter("fixture_id", NpgsqlDbType.Bigint) { Value = fixtureId });
            command.Parameters.Add(new NpgsqlParameter("sport_id", NpgsqlDbType.Bigint) { Value = sportId });
            command.Parameters.Add(new NpgsqlParameter("type_id", NpgsqlDbType.Bigint) { Value = typeId });
            command.Parameters.Add(new NpgsqlParameter("participant", NpgsqlDbType.Text)
                { Value = (object?)NullIfWhiteSpace(fact.Participant) ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("basis", NpgsqlDbType.Text)
                { Value = (object?)NullIfWhiteSpace(fact.Basis) ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("data", NpgsqlDbType.Jsonb)
                { Value = (object?)dataJson ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("natural_language", NpgsqlDbType.Text)
                { Value = (object?)NullIfWhiteSpace(fact.NaturalLanguage) ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("category", NpgsqlDbType.Text)
                { Value = (object?)NullIfWhiteSpace(fact.Category) ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("scope", NpgsqlDbType.Text)
                { Value = (object?)NullIfWhiteSpace(fact.Scope) ?? DBNull.Value });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static string? NullIfWhiteSpace(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value;

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for match facts writer.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
