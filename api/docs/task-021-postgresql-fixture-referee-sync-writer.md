# Task 21 - PostgreSQL Fixture Referee Sync Writer

Branch: `task/021-postgresql-fixture-referee-sync-writer`

## Scope

This task adds PostgreSQL writer support for SportMonks fixture referee assignments.

It focuses on referee reference rows and fixture-referee links. It does not import referee statistics, player/coach reference data, sidelined players, TV stations, weather reports, trends, comments, news, odds, predictions, or historical backfill data.

## Target Tables

- `football.referees`
- `football.fixture_referees`

## Design Decisions

- Added `ISportMonksFixtureRefereeWriter` as the writer contract.
- Added `SportMonksFixtureRefereeWriter` as the PostgreSQL/Npgsql implementation.
- The writer performs idempotent upserts using SportMonks IDs as primary keys.
- Referee rows are written before fixture-referee assignment rows.
- Referee optional foreign keys are resolved only when referenced sport, country, nationality, and city rows already exist.
- Referee `height`, `weight`, and `date_of_birth` DTO fields are nullable because SportMonks referee payloads can return null values.
- Referee `name` is required by the database, so the writer falls back through `display_name`, `common_name`, first/last name, and finally `referee-{id}`.
- Fixture referee assignments are keyed by `(fixture_id, referee_id)` and keep the role field when SportMonks provides one.
- Fixture-by-date sync now requests the `referees` include.
- Fixture date-window sync remains disabled by default through `SportMonksFixtureSync.Enabled`.
- Existing sync tracking through `ISportMonksSyncRunner` remains in place.

## Files Added

- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/ISportMonksFixtureRefereeWriter.cs`
- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/SportMonksFixtureRefereeWriter.cs`

## Files Updated

- `PreOddsApi.Entities/SportMonks/Football/Referees/V3/Referee.cs`
- `PreOddsApi.ExternalApis/DependencyInjection/SportMonksApiServiceCollectionExtensions.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/WorkerServices/FootballWorkerService.cs`

## Acceptance Criteria

Task 21 is accepted when:

1. `ISportMonksFixtureRefereeWriter` is registered in DI.
2. SportMonks referees from fixture payloads are upserted into `football.referees`.
3. SportMonks fixture referee assignments are upserted into `football.fixture_referees`.
4. Referee optional foreign keys are null-safe and do not block referee writes when referenced catalog rows are not yet available.
5. Referee nullable value fields deserialize safely when SportMonks returns null.
6. Fixture referee role is persisted when available.
7. The Football worker requests the `referees` include when fixture sync is enabled.
8. The Football worker writes fixture core rows before writing fixture referee assignments.
9. Existing sync tracking through `ISportMonksSyncRunner` remains in place.
10. Existing solution build succeeds.

## Verification

```text
dotnet build PreOddsApi.sln --no-restore
```

Build result: succeeded with existing warnings.
