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
        private readonly NpgsqlDataSource _dataSource;

        public PostgresAnalyticsReader(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<RateQueryResult> GetSignalsAsync(SignalQuery query, CancellationToken ct = default)
        {
            // Filters split into two stages so İKO (the no-vig implied
            // probability) can be normalised across the FULL market context.
            //
            // Stage 1 (baseFilters) — applied before İKO normalisation:
            //   bookmaker_id, market_id, league_id, fixture_id, window_code,
            //   match_state, fixture_date, signal_type
            //
            // Stage 2 (rowFilters) — applied AFTER İKO is computed, otherwise
            // a min_rate / min_winning_percent threshold would strip outcomes
            // from a market and inflate Σ(1/odd) for the survivors.
            //   min_rate, max_rate, min_winning_percent, min_earning_percent,
            //   min_sample_count, value_only (DSO > İKO)
            var baseClauses = new List<string> { "fs.signal_type = 'custom'" };
            var rowClauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            if (query.BookmakerId.HasValue)
            {
                baseClauses.Add("fs.bookmaker_id = @bookmaker_id");
                parameters.Add(new NpgsqlParameter("bookmaker_id", query.BookmakerId.Value));
            }
            if (query.MarketId.HasValue)
            {
                baseClauses.Add("fs.market_id = @market_id");
                parameters.Add(new NpgsqlParameter("market_id", query.MarketId.Value));
            }
            if (!string.IsNullOrWhiteSpace(query.WindowCode))
            {
                baseClauses.Add("fs.window_code = @window_code");
                parameters.Add(new NpgsqlParameter("window_code", query.WindowCode.Trim()));
            }
            if (query.MatchState.HasValue)
            {
                baseClauses.Add("f.state_id = @match_state");
                parameters.Add(new NpgsqlParameter("match_state", query.MatchState.Value));
            }
            if (query.LeagueId.HasValue)
            {
                baseClauses.Add("f.league_id = @league_id");
                parameters.Add(new NpgsqlParameter("league_id", query.LeagueId.Value));
            }
            if (query.FixtureDate.HasValue)
            {
                baseClauses.Add("f.starting_at::date = @fixture_date");
                parameters.Add(new NpgsqlParameter("fixture_date", query.FixtureDate.Value.Date));
            }

            if (query.MinRate.HasValue)
            {
                rowClauses.Add("odd_value >= @min_rate");
                parameters.Add(new NpgsqlParameter("min_rate", query.MinRate.Value));
            }
            if (query.MaxRate.HasValue)
            {
                rowClauses.Add("odd_value <= @max_rate");
                parameters.Add(new NpgsqlParameter("max_rate", query.MaxRate.Value));
            }
            if (query.MinWinningPercent.HasValue)
            {
                rowClauses.Add("winning_percent >= @min_winning_percent");
                parameters.Add(new NpgsqlParameter("min_winning_percent", query.MinWinningPercent.Value));
            }
            if (query.MinEarningPercent.HasValue)
            {
                rowClauses.Add("earning_percent >= @min_earning_percent");
                parameters.Add(new NpgsqlParameter("min_earning_percent", query.MinEarningPercent.Value));
            }
            if (query.MinSampleCount.HasValue)
            {
                rowClauses.Add("sample_count >= @min_sample_count");
                parameters.Add(new NpgsqlParameter("min_sample_count", query.MinSampleCount.Value));
            }
            if (query.ValueOnly)
            {
                // DSO > İKO — historical hit rate beats the bookmaker's
                // (no-vig) implied probability. Textbook value bet.
                rowClauses.Add("winning_percent > iko");
            }

            // SQL safety invariant — KEEP THIS:
            // `orderBy` is the only place we interpolate into SQL string-wise.
            // Both `dir` (bool→literal) and `query.Sort` (closed enum→literal)
            // produce values that are NEVER taken from user input directly.
            // If you add a new sort dimension, extend the SignalSort enum and
            // the switch below — DO NOT pass query.Sort.ToString() or any
            // free-form string into this builder. Every other dynamic value
            // (window_code, dates, ids) flows through @-bound NpgsqlParameter.
            var dir = query.SortAscending ? "asc" : "desc";
            var orderBy = query.Sort switch
            {
                SignalSort.Winning => $"winning_percent {dir} nulls last, sample_count desc",
                SignalSort.Earning => $"earning_percent {dir} nulls last, sample_count desc",
                SignalSort.Odd => $"odd_value {dir} nulls last, confidence_score desc nulls last",
                // Edge / value: p̂ × odd − 1.
                SignalSort.Edge => $"(winning_percent / 100.0 * odd_value - 1.0) {dir} nulls last, sample_count desc",
                _ => $"confidence_score {dir} nulls last, sample_count desc",
            };

            var baseWhere = "where " + string.Join(" and ", baseClauses);
            var rowWhere = rowClauses.Count > 0
                ? "where " + string.Join(" and ", rowClauses)
                : string.Empty;

            // Wrap in a per-fixture rank when the caller asked for top-N.
            // Computed AFTER row filters so VBET/DSO/etc. shape the candidates
            // before each fixture's headline rows are picked.
            var fixtureCapClause = string.Empty;
            string fixtureRankSelect;
            if (query.TopPerFixture.HasValue && query.TopPerFixture.Value > 0)
            {
                fixtureRankSelect =
                    $"row_number() over (partition by fixture_id order by {orderBy}) as fixture_rank";
                fixtureCapClause = "where fixture_rank <= @top_per_fixture";
                parameters.Add(new NpgsqlParameter("top_per_fixture", query.TopPerFixture.Value));
            }
            else
            {
                fixtureRankSelect = "1 as fixture_rank";
            }

            var sql = $"""
                with base as (
                    select fs.id, fs.fixture_id,
                           fs.id as fixture_signal_id,
                           fs.bookmaker_id, fs.market_id, fs.window_code,
                           fs.outcome_key, fs.label, fs.odd_value, fs.total, fs.handicap,
                           fs.win_count, fs.lost_count, fs.sample_count,
                           fs.winning_percent, fs.earning_percent,
                           fs.confidence_score,
                           fs.rank_order, fs.as_of_date,
                           f.state_id as match_state,
                           f.starting_at as fixture_starting_at,
                           poc.winning as bet_winning
                    from analytics.fixture_signals fs
                    inner join football.fixtures f on f.id = fs.fixture_id
                    left join odds.prematch_odds_current poc
                        on poc.fixture_id    = fs.fixture_id
                       and poc.bookmaker_id  = fs.bookmaker_id
                       and poc.market_id     = fs.market_id
                       and lower(coalesce(poc.label, '')) = lower(coalesce(fs.label, ''))
                       and coalesce(nullif(poc.total, ''), '-') = coalesce(nullif(fs.total, ''), '-')
                       and coalesce(nullif(poc.handicap, ''), '-') = coalesce(nullif(fs.handicap, ''), '-')
                       and poc.value::numeric(12,4) = fs.odd_value::numeric(12,4)
                    {baseWhere}
                ),
                market_inv_sum as (
                    -- Σ(1/oran) per (fixture, bookmaker, market, total, handicap)
                    -- computed from prematch_odds_current — fixture_signals
                    -- only has outcomes that produced settled history, so
                    -- partitioning over it can leave a single-outcome group
                    -- and inflate İKO to 100%. The (total, handicap) part
                    -- keeps separate betting lines apart: Over/Under 2.5 is a
                    -- different market than Over/Under 3.5 even though they
                    -- share market_id 80.
                    select poc.fixture_id, poc.bookmaker_id, poc.market_id,
                           coalesce(nullif(poc.total, ''), '-') as total_key,
                           coalesce(nullif(poc.handicap, ''), '-') as handicap_key,
                           sum(1.0 / poc.value::numeric) as inv_sum
                    from odds.prematch_odds_current poc
                    where (poc.fixture_id, poc.bookmaker_id, poc.market_id) in (
                        select distinct b.fixture_id, b.bookmaker_id, b.market_id
                        from base b
                    )
                    and poc.value::numeric > 0
                    group by poc.fixture_id, poc.bookmaker_id, poc.market_id,
                             coalesce(nullif(poc.total, ''), '-'),
                             coalesce(nullif(poc.handicap, ''), '-')
                ),
                with_iko as (
                    -- İKO = (1/oran) / Σ(1/oran), expressed as a percentage.
                    -- Equivalent to the bookmaker's implied probability after
                    -- stripping the vig.
                    select b.*,
                           round(
                               100.0 * (1.0 / b.odd_value) /
                               nullif(mi.inv_sum, 0),
                               4
                           ) as iko
                    from base b
                    left join market_inv_sum mi
                       on mi.fixture_id   = b.fixture_id
                      and mi.bookmaker_id = b.bookmaker_id
                      and mi.market_id    = b.market_id
                      and mi.total_key    = coalesce(nullif(b.total, ''), '-')
                      and mi.handicap_key = coalesce(nullif(b.handicap, ''), '-')
                )
                ,
                row_filtered as (
                    select with_iko.*,
                           {fixtureRankSelect}
                    from with_iko
                    {rowWhere}
                )
                select id, fixture_id, fixture_signal_id, bookmaker_id, market_id,
                       window_code, outcome_key, label, odd_value, total, handicap,
                       win_count, lost_count, sample_count,
                       winning_percent, earning_percent, confidence_score, iko,
                       rank_order, match_state, bet_winning,
                       count(*) over() as total_count,
                       sum(sample_count) over() as total_samples,
                       avg(winning_percent) over() as avg_winning_percent,
                       avg(earning_percent) over() as avg_earning_percent,
                       avg(odd_value) over() as avg_odd_value,
                       count(*) filter (where bet_winning is true) over() as success_count,
                       count(*) filter (where bet_winning is false) over() as fail_count,
                       coalesce(sum(odd_value) filter (where bet_winning is true) over(), 0) as earning_total,
                       max(as_of_date) over() as as_of_date_max
                from row_filtered
                {fixtureCapClause}
                order by {orderBy}
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
                    ConfidenceScore = ReadNullableDecimal(reader, "confidence_score"),
                    Iko = ReadNullableDecimal(reader, "iko"),
                    RankOrder = ReadNullableInt(reader, "rank_order") ?? 0,
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

        private Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
            => _dataSource.OpenConnectionAsync(ct).AsTask();

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
