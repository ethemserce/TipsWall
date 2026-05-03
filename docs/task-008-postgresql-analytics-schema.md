# Task 8 - PostgreSQL Analytics Schema

Branch: `task/008-postgresql-analytics-schema`

## Scope

This task adds the PostgreSQL analytics/read-model schema for PreOdds.

It covers normalized odd analysis snapshots, fixture-level analysis signals, hot/winning/earning read models, and season aggregate statistics. It does not add app-owned user/coupon/tip tables, sync workers, API clients, API controllers, or historical legacy data migration.

## Inputs Reviewed

- Existing legacy entities: `odd_analysis`, `seasonstats`
- Existing business read paths:
  - `OddService.GetHotRateOdds`
  - `OddService.GetWinningPercenteOdds`
  - `OddService.GetEarningPercenteOdds`
  - `FixtureService.GetHotRateFixtures`
  - `FixtureService.GetWinningPercenteFixtures`
  - `FixtureService.GetEarningPercenteFixtures`
  - `StatisticService.GetSeasonStats`
- Existing PostgreSQL schemas from Tasks 2-7

## Tables Added

- `analytics.analysis_windows`
- `analytics.odd_analysis_snapshots`
- `analytics.fixture_signals`
- `analytics.hot_rate_results`
- `analytics.winning_rate_results`
- `analytics.earning_rate_results`
- `analytics.season_stats`
- `analytics.season_team_stats`

## Design Decisions

- `odd_analysis` is normalized from many time-window columns into one row per `window_code`.
- Supported windows are seeded as `1m`, `3m`, `6m`, `1y`, and `all`.
- `outcome_key` is the stable lookup key for a bookmaker/market/outcome. Label, total, handicap, participants, and odd value are also stored for compatibility and display.
- `sample_count` is generated from `win_count + lost_count` so analytics jobs do not duplicate that calculation.
- Hot rate, winning rate, and earning rate outputs are separate read-model tables because the current product exposes them as separate workflows and filters.
- `fixture_signals` is the canonical calculated signal table; the read-model tables can point back to it through `fixture_signal_id`.
- `seasonstats` is split into:
  - `analytics.season_stats` for league/season summary metrics.
  - `analytics.season_team_stats` for team-level aggregates by season and home/away/all scope.
- Old minute-bucket columns such as `goals_scored_minutes_0` are represented as `goals_scored_minute_buckets jsonb` instead of hard-coded columns.
- This schema is calculated from SportMonks/PostgreSQL source tables. It is not a legacy historical data transfer path.

## Script

SQL file:

```text
database/postgres/007-create-analytics-schema.sql
```

## Acceptance Criteria

Task 8 is accepted when:

1. Odd analysis snapshots are normalized by date, bookmaker, market, outcome, and window.
2. Analysis windows are explicitly modeled and seeded.
3. Fixture-level signals and hot/winning/earning result tables are defined for web/mobile read paths.
4. Season aggregate statistics are represented without copying the legacy column-heavy shape unchanged.
5. Script can be applied after baseline, catalog, competition, football core, football detail, and odds scripts.
6. Script does not create app-owned tables, sync workers, API clients, API controllers, or legacy migration logic.
7. Existing application build still succeeds.
