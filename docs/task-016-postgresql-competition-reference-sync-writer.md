# Task 16 - PostgreSQL Competition Reference Sync Writer

Branch: `task/016-postgresql-competition-reference-sync-writer`

## Scope

This task migrates SportMonks football competition reference sync from the legacy EF upsert path to the new PostgreSQL writer path.

It focuses on the league hierarchy required before fixture, standings, odds, and analytics imports can be made reliable. It does not import fixtures, standings, standing details, top scorers, teams, players, or historical data.

## Target Tables

- `catalog.sports`
- `competition.leagues`
- `competition.seasons`
- `competition.stages`
- `competition.groups`
- `competition.rounds`

## Design Decisions

- Added `ISportMonksCompetitionReferenceWriter` as the writer contract.
- Added `SportMonksCompetitionReferenceWriter` as the PostgreSQL/Npgsql implementation.
- The writer performs idempotent upserts using SportMonks IDs as primary keys.
- League payloads are treated as the root of the competition hierarchy.
- Included `sport` data is written to `catalog.sports` first because `competition.leagues` requires `sport_id`.
- If a sport include is missing, a placeholder sport row can be created, but placeholder values do not overwrite existing real sport names.
- Missing parent IDs on nested season/stage/group/round payloads are repaired from the parent league/season/stage payload where possible.
- Seasons are written before stages; stages are written before groups and rounds.
- `is_current` unique partial indexes are protected by clearing previous current season/stage/round rows in the same transaction before writing the new current row.
- Optional foreign keys such as league `country_id`, stage `type_id`, and tie-breaker rule IDs are resolved only when the referenced row already exists.
- Football worker now writes the league hierarchy through the PostgreSQL writer instead of the legacy EF `IUpsertService` path.
- Legacy Football worker AutoMapper/MySQL/DataLayer references were removed from the active worker project because this task no longer uses them.
- Fixture and standings import logic is intentionally left for later PostgreSQL writer tasks.

## Files Added

- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/ISportMonksCompetitionReferenceWriter.cs`
- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/SportMonksCompetitionReferenceWriter.cs`

## Files Updated

- `PreOddsApi.ExternalApis/DependencyInjection/SportMonksApiServiceCollectionExtensions.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/Program.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/SportMonks.Football.FootballWorker.csproj`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/WorkerServices/FootballWorkerService.cs`

## Files Removed

- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/Mapping/FootballMapping.cs`

## Acceptance Criteria

Task 16 is accepted when:

1. `ISportMonksCompetitionReferenceWriter` is registered in DI.
2. SportMonks sports from league payloads are upserted into `catalog.sports`.
3. SportMonks leagues are upserted into `competition.leagues`.
4. SportMonks seasons are upserted into `competition.seasons`.
5. SportMonks stages are upserted into `competition.stages`.
6. SportMonks groups are upserted into `competition.groups`.
7. SportMonks rounds are upserted into `competition.rounds`.
8. The Football worker league sync uses the new PostgreSQL writer.
9. The Football worker project no longer depends on legacy EF upsert infrastructure for its active sync path.
10. Existing sync tracking through `ISportMonksSyncRunner` remains in place.
11. Existing solution build succeeds.

## Verification

```text
dotnet build PreOddsApi.sln --no-restore
```

Build result: succeeded with existing warnings.
