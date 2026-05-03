# Task 14 - PostgreSQL Odds Reference Sync Writer

Branch: `task/014-postgresql-odds-reference-sync-writer`

## Scope

This task adds the first SportMonks-to-new-PostgreSQL target table writer.

It focuses on low-risk odds reference data: bookmakers and markets. It does not import fixture odds, in-play odds, odds history, football fixtures, standings, or other high-volume feed data.

## Target Tables

- `odds.markets`
- `odds.bookmakers`

## Design Decisions

- Added `ISportMonksOddsReferenceWriter` as the writer contract.
- Added `SportMonksOddsReferenceWriter` as the PostgreSQL/Npgsql implementation.
- The writer performs idempotent upserts using SportMonks IDs as primary keys.
- Duplicate SportMonks IDs inside a fetched page set are collapsed by ID, keeping the last item.
- `last_synced_at` is updated on every successful write.
- `active` is set to `true` for records received from SportMonks.
- `raw_payload_id` remains null in this first writer because the sync runner currently stores page payloads independently and does not expose per-row raw payload IDs.
- Odds worker now writes markets/bookmakers to the new `odds` schema instead of using the legacy `IInsertService` path.
- Legacy `InsertService` files are not deleted in this task to avoid unrelated cleanup and keep rollback simple.

## Files Added

- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/ISportMonksOddsReferenceWriter.cs`
- `PreOddsApi.ExternalApis/SportMonks/Sync/Writers/SportMonksOddsReferenceWriter.cs`

## Files Updated

- `PreOddsApi.ExternalApis/DependencyInjection/SportMonksApiServiceCollectionExtensions.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Odds/Program.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Odds/WorkerServices/OddsWorkerService.cs`

## Acceptance Criteria

Task 14 is accepted when:

1. `ISportMonksOddsReferenceWriter` is registered in DI.
2. SportMonks markets are upserted into `odds.markets`.
3. SportMonks bookmakers are upserted into `odds.bookmakers`.
4. The odds worker uses the new PostgreSQL writer for market/bookmaker writes.
5. The odds worker no longer uses legacy `IInsertService` for market/bookmaker writes.
6. Existing sync tracking through `ISportMonksSyncRunner` remains in place.
7. Existing solution build succeeds.

## Verification

```text
dotnet build PreOddsApi.sln --no-restore
```

Build result: succeeded with existing warnings.
