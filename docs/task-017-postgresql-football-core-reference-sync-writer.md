# Task 17 - PostgreSQL Football Core Reference Sync Writer

Branch: `task/017-postgresql-football-core-reference-sync-writer`

## Scope

This task adds PostgreSQL writer support for the first football core reference entities required before reliable fixture import.

It focuses on states, venues, and teams. It does not import fixtures, fixture participants, scores, periods, referees, coaches, players, squads, rivals, standings, odds, or historical data.

## Target Tables

- `catalog.types`
- `catalog.states`
- `catalog.sports`
- `football.venues`
- `football.teams`

## Design Decisions

- Added `ISportMonksFootballCoreReferenceWriter` as the writer contract.
- Added `SportMonksFootballCoreReferenceWriter` as the PostgreSQL/Npgsql implementation.
- The writer performs idempotent upserts using SportMonks IDs as primary keys.
- State `type` includes are written to `catalog.types` before `catalog.states`.
- Team `sport` includes are written to `catalog.sports` before `football.teams`.
- If a sport include is missing, a placeholder sport row can be created, but placeholder values do not overwrite existing real sport names.
- Team `venue` includes are written before teams so `venue_id` can be linked when available.
- Optional foreign keys such as country, city, sport, and venue IDs are resolved only when the referenced row already exists.
- Numeric latitude/longitude values are parsed with invariant culture and written as PostgreSQL numeric values.
- Team `last_played_at` is parsed as UTC when possible; invalid or missing values are stored as null.
- Football worker now syncs states, venues, competition hierarchy, and teams in sequence.
- Players, coaches, referees, standings, and fixtures are intentionally left for later PostgreSQL writer tasks.

## Files Added

- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/ISportMonksFootballCoreReferenceWriter.cs`
- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/SportMonksFootballCoreReferenceWriter.cs`

## Files Updated

- `PreOddsApi.ExternalApis/DependencyInjection/SportMonksApiServiceCollectionExtensions.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/WorkerServices/FootballWorkerService.cs`

## Acceptance Criteria

Task 17 is accepted when:

1. `ISportMonksFootballCoreReferenceWriter` is registered in DI.
2. SportMonks states are upserted into `catalog.states`.
3. Included state types are upserted into `catalog.types`.
4. SportMonks venues are upserted into `football.venues`.
5. SportMonks teams are upserted into `football.teams`.
6. Included team sports are upserted into `catalog.sports`.
7. Included team venues are upserted before teams.
8. The Football worker calls state, venue, league hierarchy, and team sync in sequence.
9. Existing sync tracking through `ISportMonksSyncRunner` remains in place.
10. Existing solution build succeeds.

## Verification

```text
dotnet build PreOddsApi.sln --no-restore
```

Build result: succeeded with existing warnings.
