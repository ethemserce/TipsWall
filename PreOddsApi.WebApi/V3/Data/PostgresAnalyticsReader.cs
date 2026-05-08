using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresAnalyticsReader : IAnalyticsReader
    {
        private readonly string? _connectionString;

        public PostgresAnalyticsReader(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
        }

        public Task<RateQueryResult> GetHotRateAsync(RateQuery query, CancellationToken ct = default)
            => QueryAsync("analytics.hot_rate_results", query, ct);

        public Task<RateQueryResult> GetWinningRateAsync(RateQuery query, CancellationToken ct = default)
            => QueryAsync("analytics.winning_rate_results", query, ct);

        public Task<RateQueryResult> GetEarningRateAsync(RateQuery query, CancellationToken ct = default)
            => QueryAsync("analytics.earning_rate_results", query, ct);

        private async Task<RateQueryResult> QueryAsync(
            string tableName,
            RateQuery query,
            CancellationToken ct)
        {
            var clauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            if (query.BookmakerId.HasValue)
            {
                clauses.Add("rr.bookmaker_id = @bookmaker_id");
                parameters.Add(new NpgsqlParameter("bookmaker_id", query.BookmakerId.Value));
            }
            if (query.MarketId.HasValue)
            {
                clauses.Add("rr.market_id = @market_id");
                parameters.Add(new NpgsqlParameter("market_id", query.MarketId.Value));
            }
            if (!string.IsNullOrWhiteSpace(query.WindowCode))
            {
                clauses.Add("rr.window_code = @window_code");
                parameters.Add(new NpgsqlParameter("window_code", query.WindowCode.Trim()));
            }
            if (query.MatchState.HasValue)
            {
                clauses.Add("rr.match_state = @match_state");
                parameters.Add(new NpgsqlParameter("match_state", query.MatchState.Value));
            }
            if (query.MinRate.HasValue)
            {
                clauses.Add("rr.odd_value >= @min_rate");
                parameters.Add(new NpgsqlParameter("min_rate", query.MinRate.Value));
            }
            if (query.MinWinningPercent.HasValue)
            {
                clauses.Add("rr.winning_percent >= @min_winning_percent");
                parameters.Add(new NpgsqlParameter("min_winning_percent", query.MinWinningPercent.Value));
            }
            if (query.MinEarningPercent.HasValue)
            {
                clauses.Add("rr.earning_percent >= @min_earning_percent");
                parameters.Add(new NpgsqlParameter("min_earning_percent", query.MinEarningPercent.Value));
            }
            if (query.MinSampleCount.HasValue)
            {
                // Sample threshold guards every kind — the legacy app dropped this
                // for HotRate, but a 0-sample row is unknown, not hot.
                clauses.Add("(rr.win_count + rr.lost_count) >= @min_sample_count");
                parameters.Add(new NpgsqlParameter("min_sample_count", query.MinSampleCount.Value));
            }
            if (query.FixtureDate.HasValue)
            {
                clauses.Add("f.starting_at::date = @fixture_date");
                parameters.Add(new NpgsqlParameter("fixture_date", query.FixtureDate.Value.Date));
            }

            var where = clauses.Count > 0 ? "where " + string.Join(" and ", clauses) : string.Empty;

            // Build the rich outcome_key inline so we can JOIN settled odds the
            // same way /odds-rates does, then expose summary aggregates as
            // window functions on the filtered set.
            var sql = $"""
                with filtered as (
                    select rr.*,
                           f.state_id as fixture_state_id,
                           f.starting_at as fixture_starting_at,
                           poc.winning as bet_winning
                    from {tableName} rr
                    inner join football.fixtures f on f.id = rr.fixture_id
                    left join odds.prematch_odds_current poc
                        on poc.fixture_id    = rr.fixture_id
                       and poc.bookmaker_id  = rr.bookmaker_id
                       and poc.market_id     = rr.market_id
                       and poc.outcome_key   = lower(coalesce(rr.label, ''))
                                               || ':' || coalesce(nullif(rr.total, ''), '-')
                                               || ':' || coalesce(nullif(rr.handicap, ''), '-')
                                               || ':' || to_char(rr.odd_value::numeric, 'FM99999990.0000')
                    {where}
                )
                select id, fixture_id, fixture_signal_id, bookmaker_id, market_id,
                       window_code, outcome_key, label, odd_value, total, handicap,
                       win_count, lost_count, sample_count,
                       winning_percent, earning_percent, rank_order, match_state,
                       bet_winning,
                       count(*) over() as total_count,
                       sum(sample_count) over() as total_samples,
                       avg(winning_percent) over() as avg_winning_percent,
                       avg(earning_percent) over() as avg_earning_percent,
                       avg(odd_value) over() as avg_odd_value,
                       count(*) filter (where bet_winning is true) over() as success_count,
                       count(*) filter (where bet_winning is false) over() as fail_count,
                       coalesce(sum(odd_value) filter (where bet_winning is true) over(), 0) as earning_total,
                       max(as_of_date) over() as as_of_date_max
                from filtered
                order by bookmaker_id, market_id, window_code, rank_order
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", query.PerPage));
            parameters.Add(new NpgsqlParameter("offset", (query.Page - 1) * query.PerPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<RateResultDto>();
            var total = 0;
            var summary = new RateSummaryDto();
            DateTime? asOfDate = null;

            await using var reader = await command.ExecuteReaderAsync(ct);
            var firstRow = true;
            while (await reader.ReadAsync(ct))
            {
                if (firstRow)
                {
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));
                    var success = reader.GetInt64(reader.GetOrdinal("success_count"));
                    var fail = reader.GetInt64(reader.GetOrdinal("fail_count"));
                    summary = new RateSummaryDto
                    {
                        TotalSignals = total,
                        TotalSamples = (int)(ReadNullableLong(reader, "total_samples") ?? 0),
                        AvgWinningPercent = ReadNullableDecimal(reader, "avg_winning_percent"),
                        AvgEarningPercent = ReadNullableDecimal(reader, "avg_earning_percent"),
                        AvgOddValue = ReadNullableDecimal(reader, "avg_odd_value"),
                        SuccessCount = (int)success,
                        FailCount = (int)fail,
                        BetTotal = (int)(success + fail),
                        EarningTotal = ReadNullableDecimal(reader, "earning_total"),
                    };
                    asOfDate = ReadNullableDate(reader, "as_of_date_max");
                    firstRow = false;
                }

                items.Add(new RateResultDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    FixtureId = reader.GetInt64(reader.GetOrdinal("fixture_id")),
                    FixtureSignalId = ReadNullableGuid(reader, "fixture_signal_id"),
                    BookmakerId = reader.GetInt64(reader.GetOrdinal("bookmaker_id")),
                    MarketId = reader.GetInt64(reader.GetOrdinal("market_id")),
                    WindowCode = reader.GetString(reader.GetOrdinal("window_code")),
                    OutcomeKey = reader.GetString(reader.GetOrdinal("outcome_key")),
                    Label = reader.GetString(reader.GetOrdinal("label")),
                    OddValue = ReadNullableDecimal(reader, "odd_value"),
                    Total = ReadNullableString(reader, "total"),
                    Handicap = ReadNullableString(reader, "handicap"),
                    WinCount = reader.GetInt32(reader.GetOrdinal("win_count")),
                    LostCount = reader.GetInt32(reader.GetOrdinal("lost_count")),
                    SampleCount = reader.GetInt32(reader.GetOrdinal("sample_count")),
                    WinningPercent = ReadNullableDecimal(reader, "winning_percent"),
                    EarningPercent = ReadNullableDecimal(reader, "earning_percent"),
                    RankOrder = reader.GetInt32(reader.GetOrdinal("rank_order")),
                    MatchState = ReadNullableInt(reader, "match_state"),
                    BetWinning = ReadNullableBool(reader, "bet_winning")
                });
            }

            return new RateQueryResult
            {
                Items = items,
                Summary = summary,
                AsOfDate = asOfDate,
                Total = total
            };
        }

        private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required.");

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            return connection;
        }

        private static Guid? ReadNullableGuid(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetGuid(i);
        }

        private static decimal? ReadNullableDecimal(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetDecimal(i);
        }

        private static int? ReadNullableInt(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetInt32(i);
        }

        private static long? ReadNullableLong(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetInt64(i);
        }

        private static string? ReadNullableString(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        private static bool? ReadNullableBool(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetBoolean(i);
        }

        private static DateTime? ReadNullableDate(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetDateTime(i);
        }
    }
}
