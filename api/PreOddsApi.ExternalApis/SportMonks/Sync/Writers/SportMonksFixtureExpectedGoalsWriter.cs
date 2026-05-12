using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    /// <summary>
    /// Persists rows from <c>/v3/football/expected/fixtures</c>. Each row
    /// is one (fixture × team × xG type) — flatten <c>data.value</c> into
    /// a numeric column and keep the wrapper as raw JSON so additive
    /// payload changes are captured without a migration.
    /// </summary>
    public sealed class SportMonksFixtureExpectedGoalsWriter : ISportMonksFixtureExpectedGoalsWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksFixtureExpectedGoalsWriter> _logger;

        public SportMonksFixtureExpectedGoalsWriter(
            IConfiguration configuration,
            ILogger<SportMonksFixtureExpectedGoalsWriter> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertExpectedGoalsAsync(
            IEnumerable<FixtureExpectedGoals> rows,
            CancellationToken cancellationToken = default)
        {
            var list = rows
                .Where(r => r != null && r.Id > 0 && r.FixtureId > 0)
                .GroupBy(r => r.Id)
                .Select(g => g.Last())
                .ToList();

            if (list.Count == 0)
                return;

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var row in list)
                {
                    await UpsertOneAsync(connection, transaction, row, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {Count} SportMonks fixture expected-goals rows.",
                    list.Count);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task UpsertOneAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            FixtureExpectedGoals row,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_expected_goals (
                    id,
                    fixture_id,
                    participant_id,
                    type_id,
                    location,
                    value,
                    raw_data,
                    last_synced_at)
                values (
                    @id,
                    @fixture_id,
                    (select id from football.teams where id = @participant_id),
                    (select id from catalog.types where id = @type_id),
                    @location,
                    @value,
                    @raw_data::jsonb,
                    now())
                on conflict (id) do update set
                    fixture_id     = excluded.fixture_id,
                    participant_id = excluded.participant_id,
                    type_id        = excluded.type_id,
                    location       = excluded.location,
                    value          = excluded.value,
                    raw_data       = excluded.raw_data,
                    last_synced_at = now(),
                    updated_at     = now();
                """;

            var participantId = row.ParticipantId.HasValue && row.ParticipantId.Value > 0
                ? (object)row.ParticipantId.Value
                : DBNull.Value;
            var typeId = row.TypeId.HasValue && row.TypeId.Value > 0
                ? (object)row.TypeId.Value
                : DBNull.Value;
            var rawJson = row.Data != null
                ? JsonConvert.SerializeObject(row.Data)
                : null;

            command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Bigint) { Value = row.Id });
            command.Parameters.Add(new NpgsqlParameter("fixture_id", NpgsqlDbType.Bigint) { Value = row.FixtureId });
            command.Parameters.Add(new NpgsqlParameter("participant_id", NpgsqlDbType.Bigint) { Value = participantId });
            command.Parameters.Add(new NpgsqlParameter("type_id", NpgsqlDbType.Bigint) { Value = typeId });
            command.Parameters.Add(new NpgsqlParameter("location", NpgsqlDbType.Text)
                { Value = (object?)NullIfWhiteSpace(row.Location) ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("value", NpgsqlDbType.Numeric)
                { Value = (object?)row.Data?.Value ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("raw_data", NpgsqlDbType.Jsonb)
                { Value = (object?)rawJson ?? DBNull.Value });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static string? NullIfWhiteSpace(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value;

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for expected-goals writer.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
