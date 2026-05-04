using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksInplayOddsWriter : ISportMonksInplayOddsWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksInplayOddsWriter> _logger;

        public SportMonksInplayOddsWriter(
            IConfiguration configuration,
            ILogger<SportMonksInplayOddsWriter> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertInplayOddsForFixtureAsync(
            long fixtureId,
            IEnumerable<InplayOdd> odds,
            CancellationToken cancellationToken = default)
        {
            var oddList = odds
                .Where(o => o != null && o.Id > 0 && o.MarketId > 0 && o.BookmakerId > 0)
                .GroupBy(o => (o.BookmakerId, o.MarketId, OutcomeKey(o)))
                .Select(g => g.Last())
                .ToList();

            if (oddList.Count == 0)
                return;

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var odd in oddList)
                {
                    var outcomeKey = OutcomeKey(odd);
                    var parsedValue = ParseDecimal(odd.Value);
                    var parsedProbability = ParseDecimal(odd.Probability);
                    var parsedDp3 = ParseDecimal(odd.Dp3);
                    var parsedAmerican = ParseAmerican(odd.American);

                    var previousValue = await GetCurrentValueAsync(
                        connection, transaction, fixtureId, odd.BookmakerId, odd.MarketId, outcomeKey, cancellationToken);

                    await UpsertCurrentOddAsync(
                        connection, transaction,
                        odd, fixtureId, outcomeKey,
                        parsedValue, parsedProbability, parsedDp3, parsedAmerican,
                        cancellationToken);

                    if (previousValue.HasValue && parsedValue.HasValue && previousValue != parsedValue)
                    {
                        await InsertHistoryRowAsync(
                            connection, transaction,
                            odd, fixtureId, outcomeKey,
                            parsedValue, parsedProbability, parsedDp3, parsedAmerican,
                            cancellationToken);
                    }
                }

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Upserted {OddsCount} inplay odds for fixture {FixtureId}.",
                    oddList.Count,
                    fixtureId);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task<decimal?> GetCurrentValueAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long fixtureId,
            long bookmakerId,
            long marketId,
            string outcomeKey,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                select value
                from odds.inplay_odds_current
                where feed_type = 'standard'
                  and fixture_id = @fixture_id
                  and bookmaker_id = @bookmaker_id
                  and market_id = @market_id
                  and outcome_key = @outcome_key
                limit 1;
                """;
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(Parameter("bookmaker_id", bookmakerId));
            command.Parameters.Add(Parameter("market_id", marketId));
            command.Parameters.Add(Parameter("outcome_key", outcomeKey));

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result == DBNull.Value || result == null ? null : Convert.ToDecimal(result);
        }

        private static async Task UpsertCurrentOddAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            InplayOdd odd,
            long fixtureId,
            string outcomeKey,
            decimal? value,
            decimal? probability,
            decimal? dp3,
            int? american,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into odds.inplay_odds_current (
                    id,
                    external_id,
                    feed_type,
                    fixture_id,
                    market_id,
                    bookmaker_id,
                    outcome_key,
                    label,
                    name,
                    market_description,
                    value,
                    probability,
                    dp3,
                    fractional,
                    american,
                    american_text,
                    winning,
                    suspended,
                    stopped,
                    total,
                    handicap,
                    participants,
                    captured_at,
                    last_synced_at)
                values (
                    @id,
                    @external_id,
                    'standard',
                    @fixture_id,
                    @market_id,
                    @bookmaker_id,
                    @outcome_key,
                    @label,
                    @name,
                    @market_description,
                    @value,
                    @probability,
                    @dp3,
                    @fractional,
                    @american,
                    @american_text,
                    @winning,
                    @suspended,
                    @stopped,
                    @total,
                    @handicap,
                    @participants,
                    now(),
                    now())
                on conflict (feed_type, fixture_id, bookmaker_id, market_id, outcome_key) do update set
                    id = excluded.id,
                    external_id = excluded.external_id,
                    label = excluded.label,
                    name = excluded.name,
                    market_description = excluded.market_description,
                    value = excluded.value,
                    probability = excluded.probability,
                    dp3 = excluded.dp3,
                    fractional = excluded.fractional,
                    american = excluded.american,
                    american_text = excluded.american_text,
                    winning = excluded.winning,
                    suspended = excluded.suspended,
                    stopped = excluded.stopped,
                    total = excluded.total,
                    handicap = excluded.handicap,
                    participants = excluded.participants,
                    captured_at = now(),
                    last_synced_at = now(),
                    updated_at = now();
                """;

            command.Parameters.Add(Parameter("id", odd.Id));
            command.Parameters.Add(Parameter("external_id", odd.ExternalId > 0 ? odd.ExternalId : (long?)null));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(Parameter("market_id", odd.MarketId));
            command.Parameters.Add(Parameter("bookmaker_id", odd.BookmakerId));
            command.Parameters.Add(Parameter("outcome_key", outcomeKey));
            command.Parameters.Add(Parameter("label", NullIfWhiteSpace(odd.Label) ?? outcomeKey));
            command.Parameters.Add(Parameter("name", NullIfWhiteSpace(odd.Name)));
            command.Parameters.Add(Parameter("market_description", NullIfWhiteSpace(odd.MarketDescription)));
            command.Parameters.Add(Parameter("value", value));
            command.Parameters.Add(Parameter("probability", probability));
            command.Parameters.Add(Parameter("dp3", dp3));
            command.Parameters.Add(Parameter("fractional", NullIfWhiteSpace(odd.Fractional)));
            command.Parameters.Add(Parameter("american", american));
            command.Parameters.Add(Parameter("american_text", NullIfWhiteSpace(odd.American)));
            command.Parameters.Add(Parameter("winning", odd.Winning));
            command.Parameters.Add(Parameter("suspended", odd.Suspended));
            command.Parameters.Add(Parameter("stopped", odd.Stopped));
            command.Parameters.Add(Parameter("total", NullIfWhiteSpace(odd.Total)));
            command.Parameters.Add(Parameter("handicap", NullIfWhiteSpace(odd.Handicap)));
            command.Parameters.Add(Parameter("participants", NullIfWhiteSpace(odd.Participants)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task InsertHistoryRowAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            InplayOdd odd,
            long fixtureId,
            string outcomeKey,
            decimal? value,
            decimal? probability,
            decimal? dp3,
            int? american,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into odds.inplay_odds_history (
                    sportmonks_odd_id,
                    external_id,
                    feed_type,
                    fixture_id,
                    market_id,
                    bookmaker_id,
                    outcome_key,
                    label,
                    name,
                    value,
                    probability,
                    dp3,
                    fractional,
                    american,
                    american_text,
                    winning,
                    suspended,
                    stopped,
                    total,
                    handicap,
                    participants,
                    bookmaker_update,
                    captured_at)
                values (
                    @sportmonks_odd_id,
                    @external_id,
                    'standard',
                    @fixture_id,
                    @market_id,
                    @bookmaker_id,
                    @outcome_key,
                    @label,
                    @name,
                    @value,
                    @probability,
                    @dp3,
                    @fractional,
                    @american,
                    @american_text,
                    @winning,
                    @suspended,
                    @stopped,
                    @total,
                    @handicap,
                    @participants,
                    now(),
                    now());
                """;

            command.Parameters.Add(Parameter("sportmonks_odd_id", odd.Id));
            command.Parameters.Add(Parameter("external_id", odd.ExternalId > 0 ? odd.ExternalId : (long?)null));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(Parameter("market_id", odd.MarketId));
            command.Parameters.Add(Parameter("bookmaker_id", odd.BookmakerId));
            command.Parameters.Add(Parameter("outcome_key", outcomeKey));
            command.Parameters.Add(Parameter("label", NullIfWhiteSpace(odd.Label) ?? outcomeKey));
            command.Parameters.Add(Parameter("name", NullIfWhiteSpace(odd.Name)));
            command.Parameters.Add(Parameter("value", value));
            command.Parameters.Add(Parameter("probability", probability));
            command.Parameters.Add(Parameter("dp3", dp3));
            command.Parameters.Add(Parameter("fractional", NullIfWhiteSpace(odd.Fractional)));
            command.Parameters.Add(Parameter("american", american));
            command.Parameters.Add(Parameter("american_text", NullIfWhiteSpace(odd.American)));
            command.Parameters.Add(Parameter("winning", odd.Winning));
            command.Parameters.Add(Parameter("suspended", odd.Suspended));
            command.Parameters.Add(Parameter("stopped", odd.Stopped));
            command.Parameters.Add(Parameter("total", NullIfWhiteSpace(odd.Total)));
            command.Parameters.Add(Parameter("handicap", NullIfWhiteSpace(odd.Handicap)));
            command.Parameters.Add(Parameter("participants", NullIfWhiteSpace(odd.Participants)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for inplay odds sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static string OutcomeKey(InplayOdd odd)
        {
            var label = NullIfWhiteSpace(odd.Label);
            if (label != null)
                return label.ToLowerInvariant();

            return odd.Id.ToString();
        }

        private static decimal? ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return decimal.TryParse(value,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var result)
                ? result
                : null;
        }

        private static int? ParseAmerican(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return int.TryParse(value,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var result)
                ? result
                : null;
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static NpgsqlParameter Parameter(string name, object? value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }
    }
}
