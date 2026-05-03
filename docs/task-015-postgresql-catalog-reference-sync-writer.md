# Task 15 - PostgreSQL Catalog Reference Sync Writer

Branch: `task/015-postgresql-catalog-reference-sync-writer`

## Scope

This task adds PostgreSQL writer support for SportMonks core catalog reference data.

It focuses on relatively low-volume reference data used across web, mobile, analytics, and future football sync flows. It does not import fixtures, odds, standings, players, teams, or historical records.

## Target Tables

- `catalog.continents`
- `catalog.countries`
- `catalog.regions`
- `catalog.cities`
- `catalog.types`

## Design Decisions

- Added `ISportMonksCatalogReferenceWriter` as the writer contract.
- Added `SportMonksCatalogReferenceWriter` as the PostgreSQL/Npgsql implementation.
- The writer performs idempotent upserts using SportMonks IDs as primary keys.
- Duplicate SportMonks IDs inside a fetched set are collapsed by ID, keeping the last item.
- `last_synced_at` is updated on every successful write.
- `raw_payload_id` remains null in this first catalog writer because the sync runner stores page payloads independently and does not expose per-row raw payload IDs.
- Countries are fetched with `regions.cities` include so nested geography can be written in one transactional path.
- Continents are fetched with `countries` include and used to repair missing `continent_id` values before country writes.
- Country writes require a valid `continent_id`; missing values fail fast instead of producing partial or invalid geography data.
- The Core worker now writes catalog reference data to the new PostgreSQL schema instead of using the legacy EF `IUpsertService` path.
- Legacy EF data-layer files are not deleted in this task to avoid unrelated cleanup.

## Files Added

- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/ISportMonksCatalogReferenceWriter.cs`
- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/SportMonksCatalogReferenceWriter.cs`

## Files Updated

- `PreOddsApi.ExternalApis/DependencyInjection/SportMonksApiServiceCollectionExtensions.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Core/Program.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Core/WorkerServices/CoreWorkerService.cs`

## Acceptance Criteria

Task 15 is accepted when:

1. `ISportMonksCatalogReferenceWriter` is registered in DI.
2. SportMonks continents are upserted into `catalog.continents`.
3. SportMonks countries are upserted into `catalog.countries`.
4. Nested SportMonks regions are upserted into `catalog.regions`.
5. Nested SportMonks cities are upserted into `catalog.cities`.
6. SportMonks types are upserted into `catalog.types`.
7. The Core worker uses the new PostgreSQL writer for catalog reference writes.
8. The Core worker no longer uses legacy `IUpsertService` for catalog reference writes.
9. Existing sync tracking through `ISportMonksSyncRunner` remains in place.
10. Existing solution build succeeds.

## Verification

```text
dotnet build PreOddsApi.sln --no-restore
```

Build result: succeeded with existing warnings.
