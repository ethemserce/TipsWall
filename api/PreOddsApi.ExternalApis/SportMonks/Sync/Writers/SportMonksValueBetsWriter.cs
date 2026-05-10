using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksValueBetsWriter : ISportMonksValueBetsWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksValueBetsWriter> _logger;

        public SportMonksValueBetsWriter(
            IConfiguration configuration,
            ILogger<SportMonksValueBetsWriter> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertValueBetsForFixtureAsync(
            long fixtureId,
            IEnumerable<ValueBet> valueBets,
            CancellationToken cancellationToken = default)
        {
            var rows = valueBets
                .Where(v => v != null && v.Id > 0)
                .GroupBy(v => v.Id)
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
                    await UpsertValueBetAsync(connection, transaction, row, fixtureId, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {Count} SportMonks value bets for fixture {FixtureId}.",
                    rows.Count,
                    fixtureId);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task UpsertValueBetAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            ValueBet valueBet,
            long fixtureId,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into analytics.sportmonks_value_bets (
                    id,
                    fixture_id,
                    type_id,
                    bet,
                    bookmaker,
                    fair_odd,
                    odd,
                    stake,
                    is_value,
                    raw_predictions,
                    captured_at,
                    last_synced_at)
                values (
                    @id,
                    @fixture_id,
                    (select id from catalog.types where id = @type_id),
                    @bet,
                    @bookmaker,
                    @fair_odd,
                    @odd,
                    @stake,
                    @is_value,
                    @raw_predictions::jsonb,
                    now(),
                    now())
                on conflict (id) do update set
                    fixture_id      = excluded.fixture_id,
                    type_id         = excluded.type_id,
                    bet             = excluded.bet,
                    bookmaker       = excluded.bookmaker,
                    fair_odd        = excluded.fair_odd,
                    odd             = excluded.odd,
                    stake           = excluded.stake,
                    is_value        = excluded.is_value,
                    raw_predictions = excluded.raw_predictions,
                    captured_at     = now(),
                    last_synced_at  = now(),
                    updated_at      = now();
                """;

            var typeId = valueBet.TypeId.HasValue && valueBet.TypeId.Value > 0
                ? (object)valueBet.TypeId.Value
                : DBNull.Value;
            var rawJson = valueBet.Predictions != null
                ? JsonConvert.SerializeObject(valueBet.Predictions)
                : null;

            command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Bigint) { Value = valueBet.Id });
            command.Parameters.Add(new NpgsqlParameter("fixture_id", NpgsqlDbType.Bigint) { Value = fixtureId });
            command.Parameters.Add(new NpgsqlParameter("type_id", NpgsqlDbType.Bigint) { Value = typeId });
            command.Parameters.Add(new NpgsqlParameter("bet", NpgsqlDbType.Text)
                { Value = (object?)NullIfWhiteSpace(valueBet.Predictions?.Bet) ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("bookmaker", NpgsqlDbType.Text)
                { Value = (object?)NullIfWhiteSpace(valueBet.Predictions?.Bookmaker) ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("fair_odd", NpgsqlDbType.Numeric)
                { Value = (object?)valueBet.Predictions?.FairOdd ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("odd", NpgsqlDbType.Numeric)
                { Value = (object?)valueBet.Predictions?.Odd ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("stake", NpgsqlDbType.Numeric)
                { Value = (object?)valueBet.Predictions?.Stake ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("is_value", NpgsqlDbType.Boolean)
                { Value = (object?)valueBet.Predictions?.IsValue ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("raw_predictions", NpgsqlDbType.Jsonb)
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
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for value-bets writer.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
