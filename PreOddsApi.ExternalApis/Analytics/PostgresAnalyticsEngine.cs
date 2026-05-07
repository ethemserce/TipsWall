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
            const string sql = """
                insert into analytics.season_team_stats (
                    league_id, season_id, team_id, as_of_date, fixture_scope,
                    matches_played, matches_won, matches_drawn, matches_lost,
                    points, calculated_at)
                select
                    f.league_id,
                    f.season_id,
                    fp.team_id,
                    current_date,
                    scope.fixture_scope,
                    count(*) filter (where f.starting_at < now()),
                    count(*) filter (where fp.winner = true),
                    count(*) filter (where f.starting_at < now() and fp.winner is null),
                    count(*) filter (where fp.winner = false),
                    coalesce(count(*) filter (where fp.winner = true), 0) * 3 +
                        coalesce(count(*) filter (where f.starting_at < now() and fp.winner is null), 0),
                    now()
                from football.fixtures f
                inner join football.fixture_participants fp on fp.fixture_id = f.id
                cross join lateral (values
                    ('all'),
                    (case when fp.location = 'home' then 'home' end),
                    (case when fp.location = 'away' then 'away' end)
                ) as scope(fixture_scope)
                where f.season_id is not null
                  and scope.fixture_scope is not null
                group by f.league_id, f.season_id, fp.team_id, scope.fixture_scope
                on conflict (league_id, season_id, team_id, as_of_date, fixture_scope) do update set
                    matches_played = excluded.matches_played,
                    matches_won = excluded.matches_won,
                    matches_drawn = excluded.matches_drawn,
                    matches_lost = excluded.matches_lost,
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

        public async Task<int> RunOddAnalysisSnapshotsAsync(CancellationToken cancellationToken = default)
        {
            const string sql = """
                delete from analytics.odd_analysis_snapshots where as_of_date = current_date;

                with base as (
                    select
                        o.bookmaker_id, o.market_id,
                        o.label as odd_label,
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
                    where o.winning is not null
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
                """;

            await using var connection = await OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            var rows = await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Analytics odd_analysis_snapshots refresh affected {Rows} rows.", rows);
            return rows;
        }

        public async Task<int> RunFixtureSignalsAsync(CancellationToken cancellationToken = default)
        {
            const string sql = """
                delete from analytics.fixture_signals where as_of_date = current_date;

                with current_odds as (
                    select
                        o.id as odds_current_id, o.fixture_id, o.bookmaker_id, o.market_id,
                        o.label,
                        nullif(o.total, '') as total,
                        nullif(o.handicap, '') as handicap,
                        nullif(o.participants, '') as participants,
                        o.value::numeric(12,4) as odd_value,
                        coalesce(o.feed_type, 'standard') as feed_type,
                        f.state_id as match_state,
                        lower(coalesce(o.label, ''))
                            || ':' || coalesce(nullif(o.total, ''), '-')
                            || ':' || coalesce(nullif(o.handicap, ''), '-')
                            || ':' || to_char(o.value::numeric, 'FM99999990.0000')
                            as outcome_key
                    from odds.prematch_odds_current o
                    inner join football.fixtures f on f.id = o.fixture_id
                ),
                joined as (
                    select c.fixture_id, c.odds_current_id, c.feed_type,
                           c.bookmaker_id, c.market_id, s.window_code, c.outcome_key,
                           c.label, c.odd_value, c.total, c.handicap, c.participants, c.match_state,
                           s.win_count, s.lost_count, s.winning_percent, s.earning_percent,
                           round(case
                               when (s.win_count + s.lost_count) < 3 then 0
                               else s.winning_percent
                                    * (1 - 1.0 / sqrt((s.win_count + s.lost_count)::numeric))
                                    + greatest(s.earning_percent, 0) * 0.5
                           end, 4) as confidence_score
                    from current_odds c
                    inner join analytics.odd_analysis_snapshots s
                        on s.bookmaker_id = c.bookmaker_id
                       and s.market_id   = c.market_id
                       and s.outcome_key = c.outcome_key
                       and s.feed_type   = c.feed_type
                       and s.as_of_date  = current_date
                ),
                signals as (
                    select 'hot_rate' as signal_type, * from joined
                    union all
                    select 'winning_rate', * from joined
                    union all
                    select 'earning_rate', * from joined
                ),
                ranked as (
                    select *,
                        row_number() over (
                            partition by signal_type, bookmaker_id, market_id, window_code
                            order by case signal_type
                                       when 'hot_rate'     then confidence_score
                                       when 'winning_rate' then winning_percent
                                       when 'earning_rate' then earning_percent
                                     end desc nulls last,
                                     (win_count + lost_count) desc) as rank_order
                    from signals
                )
                insert into analytics.fixture_signals (
                    as_of_date, fixture_id, odds_current_id, feed_type,
                    signal_type, bookmaker_id, market_id, window_code, outcome_key,
                    label, odd_value, total, handicap, participants,
                    win_count, lost_count, winning_percent, earning_percent,
                    confidence_score, rank_order, filters, metrics)
                select current_date, fixture_id, odds_current_id, feed_type,
                       signal_type, bookmaker_id, market_id, window_code, outcome_key,
                       label, odd_value, total, handicap, participants,
                       win_count, lost_count, winning_percent, earning_percent,
                       confidence_score, rank_order,
                       jsonb_build_object(
                           'min_sample',      (win_count + lost_count) >= 3,
                           'positive_edge',   earning_percent > 0,
                           'high_confidence', winning_percent >= 60),
                       jsonb_build_object(
                           'sample_count', (win_count + lost_count),
                           'odd_value',    odd_value)
                from ranked;
                """;

            await using var connection = await OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            var rows = await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Analytics fixture_signals refresh affected {Rows} rows.", rows);
            return rows;
        }

        public async Task<int> RunRateResultsAsync(CancellationToken cancellationToken = default)
        {
            const string sql = """
                delete from analytics.hot_rate_results;
                delete from analytics.winning_rate_results;
                delete from analytics.earning_rate_results;

                insert into analytics.hot_rate_results (
                    as_of_date, fixture_id, fixture_signal_id, odds_current_id, feed_type,
                    bookmaker_id, market_id, window_code, outcome_key, label,
                    odd_value, total, handicap, participants,
                    win_count, lost_count, winning_percent, earning_percent,
                    min_winning_percent, min_earning_percent, min_odd_value,
                    match_state, rank_order)
                select s.as_of_date, s.fixture_id, s.id, s.odds_current_id, s.feed_type,
                       s.bookmaker_id, s.market_id, s.window_code, s.outcome_key, s.label,
                       s.odd_value, s.total, s.handicap, s.participants,
                       s.win_count, s.lost_count, s.winning_percent, s.earning_percent,
                       50.0, 0.0, 1.50, f.state_id,
                       row_number() over (
                           partition by s.bookmaker_id, s.market_id, s.window_code
                           order by s.confidence_score desc nulls last, s.sample_count desc)
                from analytics.fixture_signals s
                join football.fixtures f on f.id = s.fixture_id
                where s.signal_type = 'hot_rate'
                  and s.sample_count >= 3
                  and s.winning_percent >= 50
                  and s.earning_percent > 0
                  and s.odd_value >= 1.50;

                insert into analytics.winning_rate_results (
                    as_of_date, fixture_id, fixture_signal_id, odds_current_id, feed_type,
                    bookmaker_id, market_id, window_code, outcome_key, label,
                    odd_value, total, handicap, participants,
                    win_count, lost_count, winning_percent, earning_percent,
                    min_winning_percent, min_odd_value, match_state, rank_order)
                select s.as_of_date, s.fixture_id, s.id, s.odds_current_id, s.feed_type,
                       s.bookmaker_id, s.market_id, s.window_code, s.outcome_key, s.label,
                       s.odd_value, s.total, s.handicap, s.participants,
                       s.win_count, s.lost_count, s.winning_percent, s.earning_percent,
                       60.0, 1.30, f.state_id,
                       row_number() over (
                           partition by s.bookmaker_id, s.market_id, s.window_code
                           order by s.winning_percent desc nulls last, s.sample_count desc)
                from analytics.fixture_signals s
                join football.fixtures f on f.id = s.fixture_id
                where s.signal_type = 'winning_rate'
                  and s.sample_count >= 3
                  and s.winning_percent >= 60
                  and s.odd_value >= 1.30;

                insert into analytics.earning_rate_results (
                    as_of_date, fixture_id, fixture_signal_id, odds_current_id, feed_type,
                    bookmaker_id, market_id, window_code, outcome_key, label,
                    odd_value, total, handicap, participants,
                    win_count, lost_count, winning_percent, earning_percent,
                    min_earning_percent, min_odd_value, match_state, rank_order)
                select s.as_of_date, s.fixture_id, s.id, s.odds_current_id, s.feed_type,
                       s.bookmaker_id, s.market_id, s.window_code, s.outcome_key, s.label,
                       s.odd_value, s.total, s.handicap, s.participants,
                       s.win_count, s.lost_count, s.winning_percent, s.earning_percent,
                       10.0, 1.50, f.state_id,
                       row_number() over (
                           partition by s.bookmaker_id, s.market_id, s.window_code
                           order by s.earning_percent desc nulls last, s.sample_count desc)
                from analytics.fixture_signals s
                join football.fixtures f on f.id = s.fixture_id
                where s.signal_type = 'earning_rate'
                  and s.sample_count >= 3
                  and s.earning_percent >= 10
                  and s.odd_value >= 1.50;
                """;

            await using var connection = await OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            var rows = await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("Analytics rate_results refresh affected {Rows} rows.", rows);
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
