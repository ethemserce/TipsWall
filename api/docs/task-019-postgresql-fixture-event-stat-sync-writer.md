# Task 19 - PostgreSQL Fixture Event/Statistic Sync Writer

Branch: `task/019-postgresql-fixture-event-stat-sync-writer`

## Scope

This task adds PostgreSQL writer support for the first fixture detail payloads after fixture core import.

It focuses on fixture events and fixture statistics. It does not import lineups, lineup details, formations, referees, TV stations, weather reports, trends, comments, news, transfers, odds, predictions, or historical backfill data.

## Target Tables

- `football.fixture_events`
- `football.fixture_statistics`

## Design Decisions

- Added `ISportMonksFixtureEventStatisticWriter` as the writer contract.
- Added `SportMonksFixtureEventStatisticWriter` as the PostgreSQL/Npgsql implementation.
- The writer performs idempotent upserts using SportMonks IDs as primary keys.
- Fixture event/statistic writes run after `ISportMonksFixtureCoreWriter` so the parent `football.fixtures` row exists first.
- Event optional foreign keys are resolved only when referenced rows already exist: period, participant team, type, sub-type, player, related player, and coach.
- Statistic optional foreign keys are resolved only when referenced participant team and type rows already exist.
- Statistic `data.value` is parsed into the numeric `value` column when possible and the full `data` payload is retained in `raw_data` JSONB.
- `StatisticData` now keeps extension fields so row-level `raw_data` does not drop unknown SportMonks statistic data fields.
- Fixture-by-date sync now requests `events` and `statistics` includes in addition to the fixture core includes.
- Fixture date-window sync remains disabled by default through `SportMonksFixtureSync.Enabled`.
- Existing sync tracking through `ISportMonksSyncRunner` remains in place.

## Files Added

- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/ISportMonksFixtureEventStatisticWriter.cs`
- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/SportMonksFixtureEventStatisticWriter.cs`

## Files Updated

- `PreOddsApi.ExternalApis/DependencyInjection/SportMonksApiServiceCollectionExtensions.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/WorkerServices/FootballWorkerService.cs`
- `PreOddsApi.Entities/SportMonks/Football/Statistics/V3/StatisticData.cs`

## Acceptance Criteria

Task 19 is accepted when:

1. `ISportMonksFixtureEventStatisticWriter` is registered in DI.
2. SportMonks fixture events are upserted into `football.fixture_events`.
3. SportMonks fixture statistics are upserted into `football.fixture_statistics`.
4. Event optional foreign keys are null-safe and do not block event writes when related player/coach/type rows are not yet available.
5. Statistic optional foreign keys are null-safe and do not block statistic writes when related type rows are not yet available.
6. Statistic raw `data` payload is stored as JSONB.
7. The Football worker requests `events` and `statistics` includes when fixture sync is enabled.
8. The Football worker writes fixture core rows before writing fixture event/statistic rows.
9. Existing sync tracking through `ISportMonksSyncRunner` remains in place.
10. Existing solution build succeeds.

## Verification

```text
dotnet build PreOddsApi.sln --no-restore
```

Build result: succeeded with existing warnings.
