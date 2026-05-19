using System;
using System.Collections.Generic;
using System.Linq;
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
            // Yol A: this query joins live `prematch_odds_current` rows to
            // `odd_analysis_snapshots` at request time on the snapshot's
            // outcome_key (which already encodes label:total:handicap:odd_value).
            // When the bookmaker moves Mainz Home from 1.45 to 1.55, the
            // current odd's outcome_key flips — the JOIN automatically picks
            // the snapshot row that matches the new odd. No nightly fixture
            // signal rebuild needed.
            //
            // Filters split into two stages so İKO (the no-vig implied
            // probability) can be normalised across the FULL market context.
            //
            // Stage 1 (baseFilters) — applied before JOIN/İKO normalisation:
            //   bookmaker_id, market_id, league_id, fixture_id, window_code,
            //   match_state, fixture_date
            //
            // Stage 2 (rowFilters) — applied AFTER İKO is computed, otherwise
            // a min_rate / min_winning_percent threshold would strip outcomes
            // from a market and inflate Σ(1/odd) for the survivors.
            //   min_rate, max_rate, min_winning_percent, min_earning_percent,
            //   min_sample_count, value_only (DSO > İKO)
            var baseClauses = new List<string> { "1 = 1" };
            var rowClauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            if (query.BookmakerId.HasValue)
            {
                baseClauses.Add("poc.bookmaker_id = @bookmaker_id");
                parameters.Add(new NpgsqlParameter("bookmaker_id", query.BookmakerId.Value));
            }
            // Prefer the multi-market filter when the caller supplies one
            // (favourite-markets list). Falls back to the single-market
            // legacy parameter for unmodified callers.
            if (query.MarketIds != null && query.MarketIds.Count > 0)
            {
                baseClauses.Add("poc.market_id = any(@market_ids)");
                parameters.Add(new NpgsqlParameter("market_ids", query.MarketIds.ToArray()));
            }
            else if (query.MarketId.HasValue)
            {
                baseClauses.Add("poc.market_id = @market_id");
                parameters.Add(new NpgsqlParameter("market_id", query.MarketId.Value));
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
                // Range comparison instead of `f.starting_at::date = @date` so
                // the planner can use ix_fixtures_starting_at and the new
                // ix_fixtures_league_state_starting_at composite. Casting the
                // column makes the predicate non-sargable.
                baseClauses.Add("f.starting_at >= @fixture_date_start and f.starting_at < @fixture_date_end");
                parameters.Add(new NpgsqlParameter("fixture_date_start", query.FixtureDate.Value.Date));
                parameters.Add(new NpgsqlParameter("fixture_date_end", query.FixtureDate.Value.Date.AddDays(1)));
            }

            // window_code is a snapshot-side filter — applied inside the JOIN
            // so the planner uses ix_odd_analysis_snapshots_join.
            var windowFilter = string.Empty;
            if (!string.IsNullOrWhiteSpace(query.WindowCode))
            {
                windowFilter = " and s.window_code = @window_code";
                parameters.Add(new NpgsqlParameter("window_code", query.WindowCode.Trim()));
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
                with relevant_fixtures as (
                    -- Anything past kickoff is fair game for the per-
                    -- fixture score + team aggregates below. Pre-match
                    -- states (NS / TBA / DELAYED / PENDING) skipped
                    -- because the scores table has no rows for them
                    -- and evaluate_outcome would just return null.
                    -- The previous "only state in (5,7,8)" filter was
                    -- too tight: a 1st half that already concluded
                    -- (HT 1-2) means HT-decidable markets like İY X
                    -- are fully settled even while the 2nd half is
                    -- still in progress — gating on finished states
                    -- alone hid that verdict and the UI stayed neutral.
                    select distinct poc.fixture_id
                    from odds.prematch_odds_current poc
                    inner join football.fixtures f on f.id = poc.fixture_id
                    {baseWhere}
                      and f.state_id not in (1, 13, 16, 26)
                ),
                score_state as (
                    -- cur_h / cur_a is the live or final score per the
                    -- CURRENT row in football.fixture_scores. Whether
                    -- it counts as a "full-time" verdict is decided in
                    -- current_odds below, not here — the column itself
                    -- is just the latest goal count we have on file.
                    select s.fixture_id,
                        max(case when s.description='CURRENT'  and s.participant_location='home' then s.goals end)::int as cur_h,
                        max(case when s.description='CURRENT'  and s.participant_location='away' then s.goals end)::int as cur_a,
                        max(case when s.description='1ST_HALF' and s.participant_location='home' then s.goals end)::int as ht_h,
                        max(case when s.description='1ST_HALF' and s.participant_location='away' then s.goals end)::int as ht_a
                    from football.fixture_scores s
                    where s.fixture_id in (select fixture_id from relevant_fixtures)
                    group by s.fixture_id
                ),
                team_state as (
                    select fp.fixture_id,
                        max(case when fp.location='home' then t.name end) as home_name,
                        max(case when fp.location='away' then t.name end) as away_name
                    from football.fixture_participants fp
                    join football.teams t on t.id = fp.team_id
                    where fp.fixture_id in (select fixture_id from relevant_fixtures)
                    group by fp.fixture_id
                ),
                current_odds as (
                    select
                        poc.id            as odds_current_id,
                        poc.fixture_id,
                        poc.bookmaker_id,
                        poc.market_id,
                        poc.label,
                        nullif(poc.total, '')    as total,
                        nullif(poc.handicap, '') as handicap,
                        poc.value::numeric(12,4) as odd_value,
                        coalesce(poc.feed_type, 'standard') as feed_type,
                        -- Read-time re-grade. evaluate_outcome is called
                        -- with the latest score in fixture_scores regardless
                        -- of state. The return value is the running verdict
                        -- mid-match (1X2 / DC / CS reflect the current
                        -- scoreboard, flip with goals) and the settled
                        -- verdict once the host hits FT. HT-decidable
                        -- markets stay correct throughout because they
                        -- use ht_h/ht_a, which are stable from HT onward.
                        -- Pre-match fixtures have no CURRENT row in
                        -- fixture_scores, so cur_h/cur_a come through as
                        -- null and evaluate_outcome returns null — no
                        -- stamps leak onto upcoming rows.
                        --
                        -- Source-of-truth order (changed 2026-05-19):
                        -- 1) odds.evaluate_outcome — our own SQL function
                        --    that mirrors mobile's outcomeLiveStatus.
                        --    Trustworthy for every market in its switch
                        --    (FT/HT result, BTTS, O-U, handicap, ...).
                        -- 2) poc.winning — SportMonks-provided value.
                        --    Used ONLY when evaluate_outcome returns null
                        --    (markets we don't know how to compute:
                        --    corners, cards, player props, ...).
                        -- Previous order trusted SportMonks first for
                        -- has_winning_calculations=true markets, which
                        -- exposed BTTS / O-U / 1X2 rows to a stale-
                        -- winning bug when the SportMonks feed lagged
                        -- the score we already had locally. coalesce()
                        -- skips the SportMonks fallback when our function
                        -- decided, so divergence drops to zero for any
                        -- score-decidable market.
                        --
                        -- Caller responsibility: the analysis-page hit-
                        -- rate badge filters on match_state ∈ finished
                        -- states (see AnalysisScreen + AnalysisQuickPicksSheet)
                        -- so running verdicts from live matches don't
                        -- count toward "X / Y settled correctly".
                        coalesce(
                            odds.evaluate_outcome(
                                m.developer_name, poc.label, poc.total, poc.handicap,
                                ss.cur_h, ss.cur_a, ss.ht_h, ss.ht_a,
                                ts.home_name, ts.away_name
                            ),
                            case when coalesce(m.has_winning_calculations, false) = true then poc.winning end
                        ) as bet_winning,
                        f.state_id               as match_state,
                        f.starting_at            as fixture_starting_at,
                        f.league_id,
                        lower(coalesce(poc.label, ''))
                            || ':' || coalesce(nullif(poc.total, ''), '-')
                            || ':' || coalesce(nullif(poc.handicap, ''), '-')
                            || ':' || to_char(poc.value::numeric, 'FM99999990.0000')
                            as outcome_key
                    from odds.prematch_odds_current poc
                    inner join football.fixtures f on f.id = poc.fixture_id
                    inner join odds.markets m on m.id = poc.market_id
                        and coalesce(m.available_in_standard, true) = true
                        and coalesce(m.active, true) = true
                    left join score_state ss on ss.fixture_id = poc.fixture_id
                    left join team_state ts on ts.fixture_id = poc.fixture_id
                    {baseWhere}
                ),
                joined as (
                    -- The snapshot row for an outcome lives at
                    -- (bookmaker, market, outcome_key, feed_type, as_of_date).
                    -- Today's snapshot is rebuilt nightly from settled history,
                    -- so the JOIN attaches today's stat row to whatever the
                    -- current odd happens to be at request time.
                    select
                        c.fixture_id,
                        c.odds_current_id,
                        c.feed_type,
                        c.bookmaker_id,
                        c.market_id,
                        s.window_code,
                        c.outcome_key,
                        c.label,
                        c.odd_value,
                        c.total,
                        c.handicap,
                        c.match_state,
                        c.bet_winning,
                        s.win_count,
                        s.lost_count,
                        (s.win_count + s.lost_count) as sample_count,
                        s.winning_percent,
                        s.earning_percent,
                        (s.win_count + s.lost_count)::numeric as n_obs,
                        case
                            when (s.win_count + s.lost_count) = 0 then null
                            else s.win_count::numeric / (s.win_count + s.lost_count)
                        end as p_hat
                    from current_odds c
                    inner join analytics.odd_analysis_snapshots s
                        on s.bookmaker_id = c.bookmaker_id
                       and s.market_id   = c.market_id
                       and s.outcome_key = c.outcome_key
                       and s.feed_type   = c.feed_type
                       -- Fallback to the latest as_of_date when today's
                       -- snapshot hasn't been generated yet. The nightly
                       -- snapshot drifts later every day (24h-interval
                       -- scheduler off the last completion); without
                       -- this fallback the signals query returns 0 rows
                       -- between 00:00 and ~21:00 every day. The cron-
                       -- triggered admin rebuild at 02:00 keeps the
                       -- difference within a couple of hours.
                       and s.as_of_date  = (
                           select max(as_of_date)
                           from analytics.odd_analysis_snapshots
                       ){windowFilter}
                ),
                scored as (
                    -- Wilson lower bound at z=1.96 (95% one-sided): penalises
                    -- small samples — 10/10 ≈ 0.72, 100/100 ≈ 0.96, 3/3 ≈ 0.44.
                    select j.*,
                        case
                            when j.n_obs = 0 or j.p_hat is null then 0
                            else round(
                                100.0 * (
                                    (j.p_hat + 1.9208 / j.n_obs
                                     - 1.96 * sqrt(j.p_hat * (1.0 - j.p_hat) / j.n_obs
                                                   + 0.9604 / (j.n_obs * j.n_obs)))
                                    / (1.0 + 3.8416 / j.n_obs)
                                ),
                                4
                            )
                        end as confidence_score
                    from joined j
                ),
                ranked as (
                    select sc.*,
                        row_number() over (
                            partition by bookmaker_id, market_id, window_code
                            order by confidence_score desc nulls last,
                                     sample_count desc
                        ) as rank_order
                    from scored sc
                ),
                market_inv_sum as (
                    -- Σ(1/oran) per (fixture, bookmaker, market, total, handicap)
                    -- — needed for İKO. The (total, handicap) part keeps
                    -- separate betting lines apart: Over/Under 2.5 is a
                    -- different market than Over/Under 3.5 even though they
                    -- share market_id 80.
                    select poc.fixture_id, poc.bookmaker_id, poc.market_id,
                           coalesce(nullif(poc.total, ''), '-') as total_key,
                           coalesce(nullif(poc.handicap, ''), '-') as handicap_key,
                           sum(1.0 / poc.value::numeric) as inv_sum
                    from odds.prematch_odds_current poc
                    where (poc.fixture_id, poc.bookmaker_id, poc.market_id) in (
                        select distinct r.fixture_id, r.bookmaker_id, r.market_id from ranked r
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
                    select r.*,
                           round(
                               100.0 * (1.0 / r.odd_value) /
                               nullif(mi.inv_sum, 0),
                               4
                           ) as iko
                    from ranked r
                    left join market_inv_sum mi
                       on mi.fixture_id   = r.fixture_id
                      and mi.bookmaker_id = r.bookmaker_id
                      and mi.market_id    = r.market_id
                      and mi.total_key    = coalesce(nullif(r.total, ''), '-')
                      and mi.handicap_key = coalesce(nullif(r.handicap, ''), '-')
                ),
                row_filtered as (
                    select with_iko.*,
                           {fixtureRankSelect}
                    from with_iko
                    {rowWhere}
                )
                select
                    md5(
                        fixture_id::text || ':' || bookmaker_id::text || ':' || market_id::text || ':' || outcome_key
                    )::uuid as id,
                    fixture_id,
                    null::uuid as fixture_signal_id,
                    bookmaker_id, market_id,
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
                    current_date as as_of_date_max
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
                        SuccessCount = (int)success,
                        FailCount = (int)fail,
                        BetTotal = (int)(success + fail),
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
