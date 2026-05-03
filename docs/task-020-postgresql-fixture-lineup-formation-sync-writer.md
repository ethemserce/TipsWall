# Task 20 - PostgreSQL Fixture Lineup/Formation Sync Writer

Branch: `task/020-postgresql-fixture-lineup-formation-sync-writer`

## Scope

This task adds PostgreSQL writer support for fixture lineup and formation detail payloads.

It focuses on fixture lineups, lineup details, and team formations. It does not import players as a standalone reference sync, player transfers, sidelined players, referees, TV stations, weather reports, trends, comments, news, odds, predictions, or historical backfill data.

## Target Tables

- `football.fixture_lineups`
- `football.fixture_lineup_details`
- `football.fixture_formations`

## Design Decisions

- Added `ISportMonksFixtureLineupFormationWriter` as the writer contract.
- Added `SportMonksFixtureLineupFormationWriter` as the PostgreSQL/Npgsql implementation.
- The writer performs idempotent upserts using SportMonks IDs as primary keys.
- Fixture lineup/formation writes run after fixture core and event/statistic writes so parent fixtures and participant teams can exist first.
- Lineup optional foreign keys are resolved only when referenced rows already exist: sport, player, team, position type, and lineup type.
- Lineup detail optional foreign keys are resolved only when referenced lineup, player, team, and type rows already exist.
- Lineup detail `data` is retained as JSONB because SportMonks can return different detail shapes per type.
- Formations require a participant team row; formation rows are skipped if the participant team has not been written yet.
- `Lineup` DTO now includes `details` so `lineups.details.type` payloads can be persisted.
- Fixture-by-date sync now requests `lineups`, `lineups.details.type`, and `formations` includes.
- Fixture date-window sync remains disabled by default through `SportMonksFixtureSync.Enabled`.
- Existing sync tracking through `ISportMonksSyncRunner` remains in place.

## Files Added

- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/ISportMonksFixtureLineupFormationWriter.cs`
- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/SportMonksFixtureLineupFormationWriter.cs`

## Files Updated

- `PreOddsApi.Entities/SportMonks/Football/Lineups/V3/Lineup.cs`
- `PreOddsApi.ExternalApis/DependencyInjection/SportMonksApiServiceCollectionExtensions.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/WorkerServices/FootballWorkerService.cs`

## Acceptance Criteria

Task 20 is accepted when:

1. `ISportMonksFixtureLineupFormationWriter` is registered in DI.
2. SportMonks fixture lineups are upserted into `football.fixture_lineups`.
3. SportMonks lineup details are upserted into `football.fixture_lineup_details`.
4. SportMonks fixture formations are upserted into `football.fixture_formations`.
5. Lineup optional foreign keys are null-safe and do not block lineup writes when player/type rows are not yet available.
6. Lineup detail raw `data` payload is stored as JSONB.
7. Formation writes require an existing participant team and skip safely when that team is unavailable.
8. The Football worker requests `lineups`, `lineups.details.type`, and `formations` includes when fixture sync is enabled.
9. The Football worker writes fixture core rows before writing lineup/formation detail rows.
10. Existing sync tracking through `ISportMonksSyncRunner` remains in place.
11. Existing solution build succeeds.

## Verification

```text
dotnet build PreOddsApi.sln --no-restore
```

Build result: succeeded with existing warnings.
