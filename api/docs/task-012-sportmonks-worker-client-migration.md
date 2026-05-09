# Task 12 - SportMonks Worker Client Migration

Branch: `task/012-sportmonks-worker-client-migration`

## Scope

This task migrates the existing SportMonks worker projects from the legacy static `SportMonksApi` facade to the injectable `ISportMonksApiClient` introduced in Task 11.

It covers worker dependency injection registration, worker constructor injection, typed request construction, pagination-aware all-page reads, and cancellation token flow. It does not change the PostgreSQL schema, create new sync tables, redesign worker schedules, or replace the current upsert/insert persistence logic.

## Workers Updated

- `SportMonks.Core.Worker`
- `SportMonks.Football.FootballWorker`
- `SportMonks.Odds`

## Design Decisions

- Each worker program now registers `AddSportMonksApiClient(...)`.
- Worker services receive `ISportMonksApiClient` through constructor injection.
- Live worker calls no longer depend on the static `SportMonksApi.GetAll(...)` compatibility facade.
- Request includes and filters are expressed through `SportMonksApiRequest`.
- Results from `GetAllAsync<T>()` are converted to `List<T>` before calling the existing `UpsertAsync` and `InsertAsync` methods, because those services currently require `List<T>`.
- Core worker no longer starts its main SportMonks request through `async void`; the main import task is awaited from `ExecuteAsync`.
- Cancellation tokens are passed to SportMonks API calls so worker shutdown can stop pending HTTP requests.
- The existing static facade remains in the codebase for temporary backward compatibility, but the worker services no longer use it.

## Files Updated

- `PreOddsApi.Worker/SportMonks/SportMonks.Core/Program.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Core/WorkerServices/CoreWorkerService.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/Program.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/WorkerServices/FootballWorkerService.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Odds/Program.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Odds/WorkerServices/OddsWorkerService.cs`

## Acceptance Criteria

Task 12 is accepted when:

1. Core, football fixture, and odds workers register `ISportMonksApiClient` in DI.
2. Core, football fixture, and odds worker services use constructor-injected `ISportMonksApiClient`.
3. Active worker code no longer calls `SportMonksApi.GetAll(...)`.
4. Existing include/filter/query behavior is preserved through `SportMonksApiRequest`.
5. Worker API calls pass the worker cancellation token.
6. Existing persistence services are not redesigned in this task.
7. Existing solution build succeeds.

## Verification

```text
dotnet build PreOddsApi.sln --no-restore
```

Build result: succeeded with existing warnings.
