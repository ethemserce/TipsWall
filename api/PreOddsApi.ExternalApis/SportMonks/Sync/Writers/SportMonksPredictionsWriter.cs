using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksPredictionsWriter : ISportMonksPredictionsWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksPredictionsWriter> _logger;

        public SportMonksPredictionsWriter(
            IConfiguration configuration,
            ILogger<SportMonksPredictionsWriter> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertPredictionsForFixtureAsync(
            long fixtureId,
            IEnumerable<PreMatchPrediction> predictions,
            CancellationToken cancellationToken = default)
        {
            // Dedupe within the batch by SportMonks id — repeats can sneak in
            // when SportMonks returns the same prediction state across paged
            // responses for the same fixture.
            var rows = predictions
                .Where(p => p != null && p.Id > 0)
                .GroupBy(p => p.Id)
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
                    await UpsertPredictionAsync(connection, transaction, row, fixtureId, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {Count} SportMonks predictions for fixture {FixtureId}.",
                    rows.Count,
                    fixtureId);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task UpsertPredictionAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            PreMatchPrediction prediction,
            long fixtureId,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into analytics.sportmonks_predictions (
                    id,
                    fixture_id,
                    type_id,
                    predictions,
                    captured_at,
                    last_synced_at)
                values (
                    @id,
                    @fixture_id,
                    (select id from catalog.types where id = @type_id),
                    @predictions::jsonb,
                    now(),
                    now())
                on conflict (id) do update set
                    fixture_id = excluded.fixture_id,
                    type_id = excluded.type_id,
                    predictions = excluded.predictions,
                    captured_at = now(),
                    last_synced_at = now(),
                    updated_at = now();
                """;

            var typeId = prediction.TypeId.HasValue && prediction.TypeId.Value > 0
                ? (object)prediction.TypeId.Value
                : DBNull.Value;
            var payload = prediction.Predictions != null
                ? prediction.Predictions.ToString(Formatting.None)
                : "{}";

            command.Parameters.Add(new NpgsqlParameter("id", NpgsqlDbType.Bigint) { Value = prediction.Id });
            command.Parameters.Add(new NpgsqlParameter("fixture_id", NpgsqlDbType.Bigint) { Value = fixtureId });
            command.Parameters.Add(new NpgsqlParameter("type_id", NpgsqlDbType.Bigint) { Value = typeId });
            command.Parameters.Add(new NpgsqlParameter("predictions", NpgsqlDbType.Jsonb) { Value = payload });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for predictions writer.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
