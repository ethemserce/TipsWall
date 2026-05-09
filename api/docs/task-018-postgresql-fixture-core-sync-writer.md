# Task 18 - PostgreSQL Fixture Core Sync Writer

Branch: `task/018-postgresql-fixture-core-sync-writer`

## Scope

This task adds PostgreSQL writer support for SportMonks fixture core data.

It focuses on fixtures, fixture participants, fixture scores, and fixture periods. It does not import odds, events, statistics, lineups, referees, formations, TV stations, weather, predictions, news, trends, standings, or historical backfill data.

## Target Tables

- `catalog.sports`
- `football.venues`
- `football.teams`
- `football.fixtures`
- `football.fixture_participants`
- `football.fixture_scores`
- `football.fixture_periods`

## Design Decisions

- Added `ISportMonksFixtureCoreWriter` as the writer contract.
- Added `SportMonksFixtureCoreWriter` as the PostgreSQL/Npgsql implementation.
- The writer performs idempotent upserts using SportMonks IDs as primary keys.
- Fixture `sport` include data is written before fixtures because `football.fixtures.sport_id` is required.
- If the fixture sport include is missing, a placeholder sport row can be created, but placeholder values do not overwrite existing real sport names.
- Fixture `venue` include data is written before fixtures so `venue_id` can be linked when available.
- Fixture participant teams are written before `football.fixture_participants`.
- Optional foreign keys such as country, city, venue, state, season, stage, group, aggregate, round, type, and participant IDs are resolved only when the referenced row already exists.
- Fixture `starting_at` and period timestamps are normalized to UTC before writing to `timestamptz` columns.
- Participant meta and score payload details are stored as JSONB snapshots.
- The Football worker can run fixture-by-date sync through a configurable date window.
- Fixture date-window sync is disabled by default to protect SportMonks API quota during development.
- Existing sync tracking through `ISportMonksSyncRunner` remains in place.

## Files Added

- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/ISportMonksFixtureCoreWriter.cs`
- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/SportMonksFixtureCoreWriter.cs`

## Files Updated

- `PreOddsApi.ExternalApis/DependencyInjection/SportMonksApiServiceCollectionExtensions.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/WorkerServices/FootballWorkerService.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/appsettings.example.json`

## Acceptance Criteria

Task 18 is accepted when:

1. `ISportMonksFixtureCoreWriter` is registered in DI.
2. SportMonks fixtures are upserted into `football.fixtures`.
3. Included fixture sports are upserted into `catalog.sports`.
4. Included fixture venues are upserted into `football.venues`.
5. Fixture participant teams are upserted into `football.teams`.
6. Fixture participants are upserted into `football.fixture_participants`.
7. Fixture scores are upserted into `football.fixture_scores`.
8. Fixture periods are upserted into `football.fixture_periods`.
9. The Football worker can execute fixture-by-date sync by enabling `SportMonksFixtureSync`.
10. Fixture-by-date sync is disabled by default in example configuration.
11. Existing sync tracking through `ISportMonksSyncRunner` remains in place.
12. Existing solution build succeeds.

## Verification

```text
dotnet build PreOddsApi.sln --no-restore
```

Build result: succeeded with existing warnings.
