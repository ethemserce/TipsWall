using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace PreOddsApi.ExternalApis.Analytics
{
    public sealed class PostgresAnalyticsEngine : IAnalyticsEngine
    {
        private readonly string? _connectionString;
        private readonly ILogger<PostgresAnalyticsEngine> _logger;

        public PostgresAnalyticsEngine(
            IConfiguration configuration,
            ILogger<PostgresAnalyticsEngine> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task<int> RunSeasonStatsAsync(CancellationToken cancellationToken = default)
        {
            const string sql = """
                insert into analytics.season_stats (
                    league_id, season_id, as_of_date,
                    number_of_clubs, number_of_matches, number_of_matches_played,
                    calculated_at)
                select
                    f.league_id,
                    f.season_id,
                    current_date,
                    (
                        select count(distinct fp.team_id)
                        from football.fixture_participants fp
                        inner join football.fixtures f2 on f2.id = fp.fixture_id
                        where f2.league_id = f.league_id and f2.season_id = f.season_id
                    ),
                    count(distinct f.id),
                    count(distinct f.id) filter (where f.starting_at < now()),
                    now()
                from football.fixtures f
                where f.season_id is not null
                group by f.league_id, f.season_id
                on conflict (league_id, season_id, as_of_date) do update set
                    number_of_clubs = excluded.number_of_clubs,
                    number_of_matches = excluded.number_of_matches,
                    number_of_matches_played = excluded.number_of_matches_played,
                    calculated_at = now(),
                    updated_at = now();
                """;

            await using var connection = await OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            var rows = await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation(
                "Analytics season_stats refresh affected {Rows} rows.", rows);

            return rows;
        }

        public async Task<int> RunSeasonTeamStatsAsync(CancellationToken cancellationToken = default)
        {
            // v2: only count fixtures whose state is final (5/7/8) — the old
            // `starting_at < now()` filter inflated matches_played with
            // scheduled-but-not-played rows and left goals_for/against/
            // clean_sheets etc. as NULL because the INSERT never referenced
            // them. Pull scores via football.fixture_scores (CURRENT row
            // per participant_location) and aggregate goals + derived
            // metrics in one pass.
            const string sql = """
                with team_fixtures as (
                    select
                        f.id as fixture_id, f.league_id, f.season_id,
                        fp.team_id, fp.location, fp.winner
                    from football.fixtures f
                    inner join football.fixture_participants fp on fp.fixture_id = f.id
                    where f.season_id is not null
                      and f.state_id in (5, 7, 8)
                ),
                fixture_scores as (
                    select
                        s.fixture_id,
                        max(case when s.description='CURRENT' and s.participant_location='home' then s.goals end)::int as h_goals,
                        max(case when s.description='CURRENT' and s.participant_location='away' then s.goals end)::int as a_goals
                    from football.fixture_scores s
                    group by s.fixture_id
                ),
                enriched as (
                    select tf.*,
                        case when tf.location='home' then sc.h_goals else sc.a_goals end as goals_scored,
                        case when tf.location='home' then sc.a_goals else sc.h_goals end as goals_conceded,
                        sc.h_goals, sc.a_goals
                    from team_fixtures tf
                    left join fixture_scores sc on sc.fixture_id = tf.fixture_id
                ),
                expanded as (
                    select e.*, scope.fixture_scope
                    from enriched e
                    cross join lateral (values
                        ('all'),
                        (case when e.location = 'home' then 'home' end),
                        (case when e.location = 'away' then 'away' end)
                    ) as scope(fixture_scope)
                    where scope.fixture_scope is not null
                )
                insert into analytics.season_team_stats (
                    league_id, season_id, team_id, as_of_date, fixture_scope,
                    matches_played, matches_won, matches_drawn, matches_lost,
                    goals_for, goals_against, goal_difference,
                    clean_sheets, failed_to_score, both_teams_scored,
                    average_goals_for, average_goals_against,
                    points, calculated_at)
                select
                    league_id, season_id, team_id, current_date, fixture_scope,
                    count(*) as matches_played,
                    count(*) filter (where winner = true)  as matches_won,
                    count(*) filter (where winner is null) as matches_drawn,
                    count(*) filter (where winner = false) as matches_lost,
                    coalesce(sum(goals_scored), 0)::int   as goals_for,
                    coalesce(sum(goals_conceded), 0)::int as goals_against,
                    (coalesce(sum(goals_scored), 0) - coalesce(sum(goals_conceded), 0))::int as goal_difference,
                    count(*) filter (where goals_conceded = 0) as clean_sheets,
                    count(*) filter (where goals_scored = 0)   as failed_to_score,
                    count(*) filter (where goals_scored >= 1 and goals_conceded >= 1) as both_teams_scored,
                    round(coalesce(sum(goals_scored), 0)::numeric   / nullif(count(*), 0), 2) as average_goals_for,
                    round(coalesce(sum(goals_conceded), 0)::numeric / nullif(count(*), 0), 2) as average_goals_against,
                    coalesce(count(*) filter (where winner = true), 0) * 3 +
                        coalesce(count(*) filter (where winner is null), 0) as points,
                    now()
                from expanded
                group by league_id, season_id, team_id, fixture_scope
                on conflict (league_id, season_id, team_id, as_of_date, fixture_scope) do update set
                    matches_played = excluded.matches_played,
                    matches_won = excluded.matches_won,
                    matches_drawn = excluded.matches_drawn,
                    matches_lost = excluded.matches_lost,
                    goals_for = excluded.goals_for,
                    goals_against = excluded.goals_against,
                    goal_difference = excluded.goal_difference,
                    clean_sheets = excluded.clean_sheets,
                    failed_to_score = excluded.failed_to_score,
                    both_teams_scored = excluded.both_teams_scored,
                    average_goals_for = excluded.average_goals_for,
                    average_goals_against = excluded.average_goals_against,
                    points = excluded.points,
                    calculated_at = now(),
                    updated_at = now();
                """;

            await using var connection = await OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            var rows = await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation(
                "Analytics season_team_stats refresh affected {Rows} rows.", rows);

            return rows;
        }

        public async Task<int> RunSeasonPlayerStatsAsync(CancellationToken cancellationToken = default)
        {
            // Build per-player season totals from the source tables. Joins:
            //   fixture_lineups  → who played + which team
            //   fixture_events   → goals/assists/cards/subs
            //   fixtures         → league + season + finished-only filter
            // Idempotent: DELETE today's slice then bulk-INSERT.
            const string sql = """
                delete from analytics.season_player_stats where as_of_date = current_date;

                with played as (
                    -- A fixture_lineups row alone only proves the player
                    -- was in the matchday SQUAD (starting XI OR bench).
                    -- Counting bench rows here was inflating
                    -- matches_played + matches_started + minutes_played
                    -- for unused bench players — the "no sub-in event
                    -- detected" branch below treated them as 90-minute
                    -- starters. Filter to players who actually took the
                    -- field: starters (type=LINEUP) OR bench players
                    -- with a SUBSTITUTION event putting them on.
                    select distinct
                        f.league_id, f.season_id,
                        l.team_id, l.player_id, l.fixture_id,
                        l.type_id  as lineup_type_id,
                        upper(coalesce(t.developer_name, '')) as lineup_type_code
                    from football.fixture_lineups l
                    join football.fixtures f on f.id = l.fixture_id
                    left join catalog.types t on t.id = l.type_id
                    where l.player_id is not null
                      and l.team_id   is not null
                      and f.state_id in (5, 7, 8)
                      and f.league_id is not null
                      and f.season_id is not null
                      and (
                          upper(coalesce(t.developer_name, '')) = 'LINEUP'
                          or exists (
                              select 1
                              from football.fixture_events ev
                              join catalog.types et on et.id = ev.type_id
                              where ev.fixture_id = l.fixture_id
                                and ev.player_id  = l.player_id
                                and et.developer_name = 'SUBSTITUTION'
                          )
                      )
                ),
                events_agg as (
                    -- type_id mapping from catalog.types; the worker syncs
                    -- it from SportMonks. Codes are stable across leagues:
                    --   GOAL, OWNGOAL, PENALTY, MISSED_PENALTY,
                    --   YELLOWCARD, REDCARD, YELLOWREDCARD,
                    --   SUBSTITUTION, ASSIST, SECONDARY_ASSIST.
                    select
                        e.fixture_id, e.player_id,
                        count(*) filter (where t.developer_name = 'GOAL')                          as goals,
                        count(*) filter (where t.developer_name in ('ASSIST','SECONDARY_ASSIST'))  as assists,
                        count(*) filter (where t.developer_name = 'OWNGOAL')                       as own_goals,
                        count(*) filter (where t.developer_name = 'PENALTY')                       as penalties_scored,
                        count(*) filter (where t.developer_name = 'MISSED_PENALTY')                as penalties_missed,
                        count(*) filter (where t.developer_name = 'YELLOWCARD')                    as yellow_cards,
                        count(*) filter (where t.developer_name in ('REDCARD','YELLOWREDCARD'))    as red_cards
                    from football.fixture_events e
                    left join catalog.types t on t.id = e.type_id
                    where e.player_id is not null
                    group by e.fixture_id, e.player_id
                ),
                subs as (
                    -- A SUBSTITUTION row carries the player coming on in
                    -- player_id and the one going off in related_player_id.
                    select
                        e.fixture_id,
                        e.player_id          as sub_in_player_id,
                        e.related_player_id  as sub_out_player_id,
                        e.minute, e.extra_minute
                    from football.fixture_events e
                    join catalog.types t on t.id = e.type_id
                    where t.developer_name = 'SUBSTITUTION'
                )
                insert into analytics.season_player_stats (
                    league_id, season_id, team_id, player_id, as_of_date, fixture_scope,
                    matches_played, matches_started, matches_subbed_in, matches_subbed_out,
                    minutes_played, goals, assists, own_goals,
                    penalties_scored, penalties_missed, yellow_cards, red_cards)
                select
                    p.league_id, p.season_id, p.team_id, p.player_id, current_date, 'all',
                    count(*) as matches_played,
                    -- Lineup row + no sub-in on this fixture → started.
                    count(*) filter (
                        where not exists (
                            select 1 from subs s
                            where s.fixture_id = p.fixture_id
                              and s.sub_in_player_id = p.player_id
                        )
                    ) as matches_started,
                    count(*) filter (
                        where exists (
                            select 1 from subs s
                            where s.fixture_id = p.fixture_id
                              and s.sub_in_player_id = p.player_id
                        )
                    ) as matches_subbed_in,
                    count(*) filter (
                        where exists (
                            select 1 from subs s
                            where s.fixture_id = p.fixture_id
                              and s.sub_out_player_id = p.player_id
                        )
                    ) as matches_subbed_out,
                    -- Minutes — rough cut, deliberately simple:
                    --   started + not subbed off → 90
                    --   started + subbed off    → minute of the SUB event
                    --   subbed on               → 90 - minute_in
                    -- Extra-time / red-card edge cases are ignored for now;
                    -- the API field is "approximate minutes played" and a
                    -- second-tier metric on the UI.
                    sum(
                        case
                            when not exists (
                                select 1 from subs s
                                where s.fixture_id = p.fixture_id
                                  and s.sub_in_player_id = p.player_id
                            ) and not exists (
                                select 1 from subs s
                                where s.fixture_id = p.fixture_id
                                  and s.sub_out_player_id = p.player_id
                            ) then 90
                            when not exists (
                                select 1 from subs s
                                where s.fixture_id = p.fixture_id
                                  and s.sub_in_player_id = p.player_id
                            ) then coalesce((
                                select s.minute from subs s
                                where s.fixture_id = p.fixture_id
                                  and s.sub_out_player_id = p.player_id
                                limit 1
                            ), 90)
                            else greatest(0, 90 - coalesce((
                                select s.minute from subs s
                                where s.fixture_id = p.fixture_id
                                  and s.sub_in_player_id = p.player_id
                                limit 1
                            ), 0))
                        end
                    ) as minutes_played,
                    coalesce(sum(ea.goals), 0)             as goals,
                    coalesce(sum(ea.assists), 0)           as assists,
                    coalesce(sum(ea.own_goals), 0)         as own_goals,
                    coalesce(sum(ea.penalties_scored), 0)  as penalties_scored,
                    coalesce(sum(ea.penalties_missed), 0)  as penalties_missed,
                    coalesce(sum(ea.yellow_cards), 0)      as yellow_cards,
                    coalesce(sum(ea.red_cards), 0)         as red_cards
                from played p
                left join events_agg ea
                    on ea.fixture_id = p.fixture_id
                   and ea.player_id  = p.player_id
                group by p.league_id, p.season_id, p.team_id, p.player_id;
                """;

            await using var connection = await OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            var rows = await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation(
                "Analytics season_player_stats refresh affected {Rows} rows.", rows);

            return rows;
        }

        public async Task<int> RunOddAnalysisSnapshotsAsync(CancellationToken cancellationToken = default)
        {
            // Two adjustments from the original SQL:
            //
            // 1. The outcome_key normalises the label with `lower(...)` but
            //    the agg's GROUP BY used the *original-case* label. When
            //    SportMonks ships the same outcome from two bookmakers with
            //    different casing ('Home' vs 'home'), the GROUP BY treats
            //    them as two distinct rows yet they produce the same
            //    outcome_key — and the unique constraint
            //    odd_analysis_snapshots_as_of_date_..._outcome_key_key
            //    then rejects the second row. Lower the label everywhere
            //    we touch it so the bucket key is consistent end-to-end.
            //
            // 2. DELETE + INSERT used to be sent as two separate auto-commit
            //    statements (no `begin`/`commit`). If the INSERT then failed
            //    midway — like it just did with the case-collision above —
            //    the DELETE was already committed, leaving the snapshot
            //    table in a half-empty state. Wrapping both inside a single
            //    transaction means a failure rolls back to the pre-run
            //    snapshot rather than partially destroying it.
            const string sql = """
                begin;

                delete from analytics.odd_analysis_snapshots where as_of_date = current_date;

                with base as (
                    select
                        o.bookmaker_id, o.market_id,
                        lower(o.label) as odd_label,
                        nullif(o.total, '') as odd_total,
                        nullif(o.handicap, '') as odd_handicap,
                        o.value::numeric(12,4) as odd_value,
                        o.winning,
                        coalesce(o.feed_type, 'standard') as feed_type,
                        f.starting_at,
                        lower(coalesce(o.label, ''))
                            || ':' || coalesce(nullif(o.total, ''), '-')
                            || ':' || coalesce(nullif(o.handicap, ''), '-')
                            || ':' || to_char(o.value::numeric, 'FM99999990.0000')
                            as outcome_key
                    from odds.prematch_odds_current o
                    inner join football.fixtures f on f.id = o.fixture_id
                    inner join odds.markets m on m.id = o.market_id
                    where o.winning is not null
                      -- Snapshot pipeline scope matches the broader display
                      -- filter the rest of the app uses now (every active
                      -- standard market) so /signals and /odds-rates show
                      -- consistent rows. SportMonks doesn't grade most of
                      -- these markets itself, so the OddOutcomeFinalizer
                      -- (RunOddOutcomeFinalizerAsync) is responsible for
                      -- populating o.winning via odds.evaluate_outcome —
                      -- without it, this aggregation would inherit the
                      -- stale `false` defaults SportMonks ships.
                      and m.available_in_standard = true
                      and m.active = true
                      -- SportMonks ships `winning=false` on rows that haven't
                      -- been settled yet (state_id=1, NotStarted, also on
                      -- in-play states 2/3/22 mid-match). Those rows were
                      -- inflating "lost" counts — a Yes vs No bug-check on
                      -- 2026-05-13 showed 1658 fixture-pairs where Yes and
                      -- No were both winning=false, 1580 of them on yet-
                      -- unplayed matches. Only count finished states:
                      --   5 = Full Time, 7 = AET, 8 = FT pen.
                      and f.state_id in (5, 7, 8)
                ),
                windowed as (
                    select b.*, w.code as window_code
                    from base b
                    cross join analytics.analysis_windows w
                    where w.lookback_days is null
                       or b.starting_at >= now() - (w.lookback_days || ' days')::interval
                ),
                agg as (
                    select bookmaker_id, market_id, window_code, outcome_key, feed_type,
                           odd_label as label, odd_total as total, odd_handicap as handicap, odd_value,
                           count(*) filter (where winning is true)  as win_count,
                           count(*) filter (where winning is false) as lost_count
                    from windowed
                    group by bookmaker_id, market_id, window_code, outcome_key, feed_type,
                             odd_label, odd_total, odd_handicap, odd_value
                )
                insert into analytics.odd_analysis_snapshots (
                    as_of_date, feed_type, bookmaker_id, market_id, window_code,
                    outcome_key, label, odd_value, total, handicap,
                    win_count, lost_count, winning_percent, earning_percent, average_odd_value)
                select
                    current_date, feed_type, bookmaker_id, market_id, window_code,
                    outcome_key, label, odd_value, total, handicap,
                    win_count, lost_count,
                    round(100.0 * win_count / nullif(win_count + lost_count, 0), 4),
                    round(100.0 * (win_count * odd_value - (win_count + lost_count))
                                / nullif(win_count + lost_count, 0), 4),
                    odd_value
                from agg;

                commit;
                """;

            await using var connection = await OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            var rows = await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Analytics odd_analysis_snapshots refresh affected {Rows} rows.", rows);
            return rows;
        }

        public Task<int> RunFixtureSignalsAsync(CancellationToken cancellationToken = default)
        {
            // No-op: the fixture_signals materialised table is being retired.
            // /v3/signals now joins prematch_odds_current × odd_analysis_snapshots
            // at request time (see PostgresAnalyticsReader.GetSignalsAsync), so
            // the precomputed snapshot served by this method is dead weight.
            // Migration 020 will drop the table; until then it sits empty.
            _logger.LogInformation(
                "Analytics fixture_signals refresh is a no-op (Yol A: runtime JOIN).");
            return Task.FromResult(0);
        }

        public async Task<int> RunOddOutcomeFinalizerAsync(
            int lookbackHours = 36,
            CancellationToken cancellationToken = default)
        {
            // Grade outcomes for fixtures that transitioned to a final state
            // recently. The UPDATE only touches rows where SportMonks doesn't
            // compute the winning flag itself — for those markets the field
            // ships with a default `false` even when the outcome actually won,
            // so we overwrite with the score-derived value. Markets with
            // has_winning_calculations=true are trusted as-is.
            //
            // Restricted by `f.updated_at` so older fixtures (already graded
            // on a previous run) don't get rescanned. odds.evaluate_outcome
            // handles the per-market logic; the WHERE-clause null check on
            // its return makes the UPDATE idempotent + safe — unsupported
            // markets keep whatever SportMonks shipped.
            const string sql = """
                with target_fixtures as (
                    select f.id
                    from football.fixtures f
                    where f.state_id in (5, 7, 8)
                      and f.updated_at > now() - make_interval(hours => @lookback_hours)
                ),
                score_state as (
                    select
                        s.fixture_id,
                        max(case when s.description='CURRENT'  and s.participant_location='home' then s.goals end)::int as ft_h,
                        max(case when s.description='CURRENT'  and s.participant_location='away' then s.goals end)::int as ft_a,
                        max(case when s.description='1ST_HALF' and s.participant_location='home' then s.goals end)::int as ht_h,
                        max(case when s.description='1ST_HALF' and s.participant_location='away' then s.goals end)::int as ht_a
                    from football.fixture_scores s
                    where s.fixture_id in (select id from target_fixtures)
                    group by s.fixture_id
                ),
                team_state as (
                    select
                        fp.fixture_id,
                        max(case when fp.location='home' then t.name end) as home_name,
                        max(case when fp.location='away' then t.name end) as away_name
                    from football.fixture_participants fp
                    join football.teams t on t.id = fp.team_id
                    where fp.fixture_id in (select id from target_fixtures)
                    group by fp.fixture_id
                )
                update odds.prematch_odds_current poc
                set winning = odds.evaluate_outcome(
                    m.developer_name, poc.label, poc.total, poc.handicap,
                    ss.ft_h, ss.ft_a, ss.ht_h, ss.ht_a,
                    ts.home_name, ts.away_name)
                from odds.markets m, score_state ss, team_state ts
                where m.id = poc.market_id
                  and ss.fixture_id = poc.fixture_id
                  and ts.fixture_id = poc.fixture_id
                  and coalesce(m.has_winning_calculations, false) = false
                  and odds.evaluate_outcome(
                        m.developer_name, poc.label, poc.total, poc.handicap,
                        ss.ft_h, ss.ft_a, ss.ht_h, ss.ht_a,
                        ts.home_name, ts.away_name) is not null
                  and poc.winning is distinct from odds.evaluate_outcome(
                        m.developer_name, poc.label, poc.total, poc.handicap,
                        ss.ft_h, ss.ft_a, ss.ht_h, ss.ht_a,
                        ts.home_name, ts.away_name);
                """;

            await using var connection = await OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("lookback_hours", lookbackHours));
            var rows = await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation(
                "Odd outcome finalizer stamped {Rows} prematch_odds_current rows (lookback {Hours}h).",
                rows, lookbackHours);
            return rows;
        }

        private async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for analytics engine.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
