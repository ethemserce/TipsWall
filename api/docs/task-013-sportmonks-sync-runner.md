# Task 13 - SportMonks Sync Runner

Branch: `task/013-sportmonks-sync-runner`

## Scope

This task adds the first real PostgreSQL-backed SportMonks sync execution flow.

It covers sync job registration, API request tracking, raw payload capture, cursor updates, and worker integration through a shared `ISportMonksSyncRunner`. It does not redesign the existing upsert/insert mapping services, create new database tables, run a live SportMonks import during acceptance, or migrate old historical data.

## PostgreSQL Tables Used

The runner writes to the `sync` schema that already exists from the baseline script:

- `sync.sync_jobs`
- `sync.api_requests`
- `sync.raw_payloads`
- `sync.sync_cursors`

No new SQL migration is required for this task.

## Design Decisions

- Added `ISportMonksSyncRunner` as the worker-facing sync execution contract.
- Added `SportMonksSyncRunner` to wrap `ISportMonksApiClient` calls with PostgreSQL tracking.
- Added `SportMonksSyncJobDefinition` so each worker call has a stable `job_key`, `entity_name`, and description.
- The runner upserts `sync.sync_jobs` before each execution.
- Each SportMonks page request creates and completes a `sync.api_requests` row.
- Each page response is stored in `sync.raw_payloads` as JSONB.
- Cursor success/failure state is stored in `sync.sync_cursors`.
- Pagination still follows SportMonks `pagination.has_more` and `pagination.next_page`.
- Token-like query parameters are sanitized before URLs are stored.
- Workers now call `ISportMonksSyncRunner` instead of calling the API client directly for active import paths.
- Existing persistence services remain responsible for transforming SportMonks DTOs into legacy EF entities.

## Worker Jobs Added

- `sportmonks.core.continents`
- `sportmonks.core.countries`
- `sportmonks.core.regions`
- `sportmonks.core.cities`
- `sportmonks.core.types`
- `sportmonks.football.states`
- `sportmonks.football.leagues`
- `sportmonks.football.standings`
- `sportmonks.football.fixtures.by-date`
- `sportmonks.odds.markets`
- `sportmonks.odds.bookmakers`

## Files Added

- `PreOddsApi.ExternalApis/SportMonks/Sync/ISportMonksSyncRunner.cs`
- `PreOddsApi.ExternalApis/SportMonks/Sync/SportMonksSyncJobDefinition.cs`
- `PreOddsApi.ExternalApis/SportMonks/Sync/SportMonksSyncRunner.cs`

## Files Updated

- `PreOddsApi.ExternalApis/DependencyInjection/SportMonksApiServiceCollectionExtensions.cs`
- `PreOddsApi.ExternalApis/PreOddsApi.ExternalApis.csproj`
- `PreOddsApi.Worker/SportMonks/SportMonks.Core/WorkerServices/CoreWorkerService.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/WorkerServices/FootballWorkerService.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Odds/WorkerServices/OddsWorkerService.cs`

## Acceptance Criteria

Task 13 is accepted when:

1. `ISportMonksSyncRunner` is registered in DI.
2. The sync runner writes job definitions to `sync.sync_jobs`.
3. The sync runner records API request start/completion in `sync.api_requests`.
4. The sync runner stores successful page payloads in `sync.raw_payloads`.
5. The sync runner updates success/failure cursor state in `sync.sync_cursors`.
6. Active worker API reads use the sync runner.
7. Existing upsert/insert persistence behavior is not redesigned.
8. Existing solution build succeeds.

## Verification

```text
dotnet restore PreOddsApi.sln
dotnet build PreOddsApi.sln --no-restore
```

Build result: succeeded with existing warnings.
