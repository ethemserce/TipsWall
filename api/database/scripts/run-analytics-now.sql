-- One-shot manual analytics refresh — mirrors the SQL in
-- PostgresAnalyticsEngine.cs's RunSeasonStatsAsync /
-- RunSeasonTeamStatsAsync / RunOddAnalysisSnapshotsAsync. Used to
-- short-circuit the worker's nightly schedule when we need today's
-- numbers right now (e.g. after a SportMonks plan upgrade).
--
-- Idempotent: every statement does an upsert / delete-then-insert
-- so re-running on the same calendar day just refreshes.

\timing on

-- 1) Season stats
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

-- 2) Season team stats
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

-- 3) Odd analysis snapshots — delete today + recompute
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
    inner join odds.markets m on m.id = o.market_id
    where o.winning is not null
      and coalesce(m.has_winning_calculations, false) = true
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

-- Summary
select 'season_stats' as t, count(*) from analytics.season_stats where as_of_date = current_date
union all
select 'season_team_stats', count(*) from analytics.season_team_stats where as_of_date = current_date
union all
select 'odd_analysis_snapshots', count(*) from analytics.odd_analysis_snapshots where as_of_date = current_date;
