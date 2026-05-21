using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksPrematchOddsWriter : ISportMonksPrematchOddsWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksPrematchOddsWriter> _logger;
        private readonly HashSet<long> _allowedBookmakerIds;

        public SportMonksPrematchOddsWriter(
            IConfiguration configuration,
            ILogger<SportMonksPrematchOddsWriter> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;

            var allowed = configuration
                .GetSection("SportMonksPrematchOddsSync:AllowedBookmakerIds")
                .Get<long[]>() ?? Array.Empty<long>();
            _allowedBookmakerIds = new HashSet<long>(allowed);
        }

        public async Task UpsertPrematchOddsAsync(
            IEnumerable<Fixture> fixtures,
            CancellationToken cancellationToken = default)
        {
            var totalOdds = 0;

            foreach (var fixture in fixtures)
            {
                if (fixture?.Odds == null || fixture.Odds.Count == 0)
                    continue;

                await UpsertPrematchOddsForFixtureAsync(fixture.Id, fixture.Odds, cancellationToken);
                totalOdds += fixture.Odds.Count;
            }

            if (totalOdds > 0)
            {
                _logger.LogInformation(
                    "Processed prematch odds for {FixtureCount} fixtures ({TotalOdds} rows).",
                    fixtures.Count(),
                    totalOdds);
            }
        }

        public async Task UpsertPrematchOddsForFixtureAsync(
            long fixtureId,
            IEnumerable<PreMatchOdd> odds,
            CancellationToken cancellationToken = default)
        {
            var oddList = odds
                .Where(o => o != null && o.Id > 0 && o.MarketId > 0 && o.BookmakerId > 0)
                .Where(o => _allowedBookmakerIds.Count == 0
                            || _allowedBookmakerIds.Contains(o.BookmakerId))
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
                from odds.prematch_odds_current
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
            PreMatchOdd odd,
            long fixtureId,
            string outcomeKey,
            decimal? value,
            decimal? probability,
            decimal? dp3,
            int? american,
            CancellationToken cancellationToken)
        {
            await using (var purgeCmd = connection.CreateCommand())
            {
                purgeCmd.Transaction = transaction;
                purgeCmd.CommandText = """
                    delete from odds.prematch_odds_current
                    where id = @id
                      and (feed_type <> 'standard'
                        or fixture_id <> @fixture_id
                        or bookmaker_id <> @bookmaker_id
                        or market_id <> @market_id
                        or outcome_key <> @outcome_key);
                    """;
                purgeCmd.Parameters.Add(Parameter("id", odd.Id));
                purgeCmd.Parameters.Add(Parameter("fixture_id", fixtureId));
                purgeCmd.Parameters.Add(Parameter("bookmaker_id", odd.BookmakerId));
                purgeCmd.Parameters.Add(Parameter("market_id", odd.MarketId));
                purgeCmd.Parameters.Add(Parameter("outcome_key", outcomeKey));
                await purgeCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into odds.prematch_odds_current (
                    id,
                    feed_type,
                    fixture_id,
                    market_id,
                    bookmaker_id,
                    outcome_key,
                    label,
                    original_label,
                    name,
                    sort_order,
                    market_description,
                    value,
                    probability,
                    dp3,
                    fractional,
                    american,
                    american_text,
                    winning,
                    stopped,
                    total,
                    handicap,
                    participants,
                    source_created_at,
                    latest_bookmaker_update,
                    captured_at,
                    last_synced_at)
                values (
                    @id,
                    'standard',
                    @fixture_id,
                    @market_id,
                    @bookmaker_id,
                    @outcome_key,
                    @label,
                    @original_label,
                    @name,
                    @sort_order,
                    @market_description,
                    @value,
                    @probability,
                    @dp3,
                    @fractional,
                    @american,
                    @american_text,
                    @winning,
                    @stopped,
                    @total,
                    @handicap,
                    @participants,
                    @source_created_at,
                    @latest_bookmaker_update,
                    now(),
                    now())
                on conflict (feed_type, fixture_id, bookmaker_id, market_id, outcome_key) do update set
                    id = excluded.id,
                    label = excluded.label,
                    original_label = excluded.original_label,
                    name = excluded.name,
                    sort_order = excluded.sort_order,
                    market_description = excluded.market_description,
                    value = excluded.value,
                    probability = excluded.probability,
                    dp3 = excluded.dp3,
                    fractional = excluded.fractional,
                    american = excluded.american,
                    american_text = excluded.american_text,
                    winning = excluded.winning,
                    stopped = excluded.stopped,
                    total = excluded.total,
                    handicap = excluded.handicap,
                    participants = excluded.participants,
                    source_created_at = excluded.source_created_at,
                    latest_bookmaker_update = excluded.latest_bookmaker_update,
                    captured_at = now(),
                    last_synced_at = now(),
                    updated_at = now();
                """;

            command.Parameters.Add(Parameter("id", odd.Id));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(Parameter("market_id", odd.MarketId));
            command.Parameters.Add(Parameter("bookmaker_id", odd.BookmakerId));
            command.Parameters.Add(Parameter("outcome_key", outcomeKey));
            command.Parameters.Add(Parameter("label", NullIfWhiteSpace(odd.Label) ?? outcomeKey));
            command.Parameters.Add(Parameter("original_label", NullIfWhiteSpace(odd.original_label)));
            command.Parameters.Add(Parameter("name", NullIfWhiteSpace(odd.Name)));
            command.Parameters.Add(Parameter("sort_order", odd.SortOrder));
            command.Parameters.Add(Parameter("market_description", NullIfWhiteSpace(odd.MarketDescription)));
            command.Parameters.Add(Parameter("value", value));
            command.Parameters.Add(Parameter("probability", probability));
            command.Parameters.Add(Parameter("dp3", dp3));
            command.Parameters.Add(Parameter("fractional", NullIfWhiteSpace(odd.Fractional)));
            command.Parameters.Add(Parameter("american", american));
            command.Parameters.Add(Parameter("american_text", NullIfWhiteSpace(odd.American)));
            command.Parameters.Add(Parameter("winning", odd.Winning));
            command.Parameters.Add(Parameter("stopped", odd.Stopped));
            command.Parameters.Add(Parameter("total", NullIfWhiteSpace(odd.Total)));
            command.Parameters.Add(Parameter("handicap", NullIfWhiteSpace(odd.Handicap)));
            command.Parameters.Add(Parameter("participants", NullIfWhiteSpace(odd.Participants)));
            command.Parameters.Add(Parameter("source_created_at", odd.CreatedAt));
            command.Parameters.Add(Parameter("latest_bookmaker_update",
                odd.LatestBookmakerUpdate == default ? null : (DateTime?)odd.LatestBookmakerUpdate));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task InsertHistoryRowAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            PreMatchOdd odd,
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
                insert into odds.prematch_odds_history (
                    sportmonks_odd_id,
                    feed_type,
                    fixture_id,
                    market_id,
                    bookmaker_id,
                    outcome_key,
                    label,
                    original_label,
                    name,
                    value,
                    probability,
                    dp3,
                    fractional,
                    american,
                    american_text,
                    winning,
                    stopped,
                    total,
                    handicap,
                    participants,
                    source_created_at,
                    bookmaker_update,
                    captured_at)
                values (
                    @sportmonks_odd_id,
                    'standard',
                    @fixture_id,
                    @market_id,
                    @bookmaker_id,
                    @outcome_key,
                    @label,
                    @original_label,
                    @name,
                    @value,
                    @probability,
                    @dp3,
                    @fractional,
                    @american,
                    @american_text,
                    @winning,
                    @stopped,
                    @total,
                    @handicap,
                    @participants,
                    @source_created_at,
                    @bookmaker_update,
                    now());
                """;

            command.Parameters.Add(Parameter("sportmonks_odd_id", odd.Id));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(Parameter("market_id", odd.MarketId));
            command.Parameters.Add(Parameter("bookmaker_id", odd.BookmakerId));
            command.Parameters.Add(Parameter("outcome_key", outcomeKey));
            command.Parameters.Add(Parameter("label", NullIfWhiteSpace(odd.Label) ?? outcomeKey));
            command.Parameters.Add(Parameter("original_label", NullIfWhiteSpace(odd.original_label)));
            command.Parameters.Add(Parameter("name", NullIfWhiteSpace(odd.Name)));
            command.Parameters.Add(Parameter("value", value));
            command.Parameters.Add(Parameter("probability", probability));
            command.Parameters.Add(Parameter("dp3", dp3));
            command.Parameters.Add(Parameter("fractional", NullIfWhiteSpace(odd.Fractional)));
            command.Parameters.Add(Parameter("american", american));
            command.Parameters.Add(Parameter("american_text", NullIfWhiteSpace(odd.American)));
            command.Parameters.Add(Parameter("winning", odd.Winning));
            command.Parameters.Add(Parameter("stopped", odd.Stopped));
            command.Parameters.Add(Parameter("total", NullIfWhiteSpace(odd.Total)));
            command.Parameters.Add(Parameter("handicap", NullIfWhiteSpace(odd.Handicap)));
            command.Parameters.Add(Parameter("participants", NullIfWhiteSpace(odd.Participants)));
            command.Parameters.Add(Parameter("source_created_at", odd.CreatedAt));
            command.Parameters.Add(Parameter("bookmaker_update",
                odd.LatestBookmakerUpdate == default ? null : (DateTime?)odd.LatestBookmakerUpdate));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for prematch odds sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        // SportMonks ships bookmaker labels in whatever locale the bookmaker
        // uses for the specific market. bet365 (id=2) returns Portuguese
        // ("Mais de"/"Menos de"), French ("Plus de"/"Moins de"), Spanish
        // ("Más de"/"Menos de"), and English ("Over"/"Under") rows on the
        // same fixture/market depending on how SportMonks scraped it. Without
        // a canonical label, the unique constraint (feed_type, fixture_id,
        // bookmaker_id, market_id, outcome_key) treats these as distinct
        // outcomes, producing duplicate rows in odds.prematch_odds_current
        // and breaking the analytics JOIN on outcome_key.
        private static readonly Dictionary<string, string> LabelCanon =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // Over / Under (market 80, 53, etc.)
                ["over"] = "over",
                ["mais de"] = "over",
                ["plus de"] = "over",
                ["más de"] = "over",
                ["mas de"] = "over",
                ["sobre"] = "over",
                ["uber"] = "over",
                ["über"] = "over",
                ["under"] = "under",
                ["menos de"] = "under",
                ["moins de"] = "under",
                ["bajo"] = "under",
                ["unter"] = "under",
                ["sotto"] = "under",
                // 1X2 / Match Winner (market 1)
                ["1"] = "1",
                ["x"] = "x",
                ["2"] = "2",
                ["home"] = "1",
                ["draw"] = "x",
                ["away"] = "2",
                // Both Teams to Score (market 14)
                ["yes"] = "yes",
                ["sim"] = "yes",
                ["oui"] = "yes",
                ["sí"] = "yes",
                ["si"] = "yes",
                ["ja"] = "yes",
                ["no"] = "no",
                ["não"] = "no",
                ["nao"] = "no",
                ["non"] = "no",
                ["nein"] = "no",
            };

        private static string OutcomeKey(PreMatchOdd odd)
        {
            // Prefer SportMonks's original_label when present — it's the
            // canonical English form that survives bookmaker localization.
            // Fall back to label, then the lookup table to fold the dozen
            // common non-English variants we've observed.
            var label = NullIfWhiteSpace(odd.original_label) ?? NullIfWhiteSpace(odd.Label);
            if (label == null)
                return odd.Id.ToString();

            var canonical = LabelCanon.GetValueOrDefault(label.Trim(), label.Trim().ToLowerInvariant());

            // O/U lines (market_id=80) ship 18 totals (0.5, 1.5, 2.5, 3.5 …)
            // all with the same label ("Over" / "Under"); Asian Handicap
            // (market_id=28) ships per-line handicap with the same label
            // ("Home" / "Away"). Encoding total + handicap into the key
            // disambiguates so each line gets its own row in
            // prematch_odds_current.
            var key = canonical;
            var total = NullIfWhiteSpace(odd.Total);
            if (total != null) key += "|t:" + total;
            var handicap = NullIfWhiteSpace(odd.Handicap);
            if (handicap != null) key += "|h:" + handicap;
            return key;
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
