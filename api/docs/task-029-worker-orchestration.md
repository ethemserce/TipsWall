# Task 029 - Worker Orchestration

## Goal

Replace the fixed-delay loop pattern in all three SportMonks workers with per-job-group interval
scheduling. Each job group runs on its own cadence instead of a single shared timer, preventing
reference data from running as often as fixture data and fixing the 1-second loop in OddsWorkerService.

## Problems Fixed

- `OddsWorkerService` calls markets and bookmakers APIs every 1 second — exhausts API quota immediately.
- `FootballWorkerService` runs all job groups (leagues, players, fixtures, news) on the same 100-second
  timer with no per-group frequency control.
- `CoreWorkerService` runs once on startup and never repeats; continents/countries/types are never
  re-synced without a process restart.
- `sync.sync_cursors` stores `last_success_at` but workers never read it before running; every restart
  re-runs all jobs regardless of how recently they completed.

## Design

Add `ISyncJobScheduler` / `SyncJobScheduler` to `PreOddsApi.ExternalApis`. The scheduler holds an
in-memory `ConcurrentDictionary<string, DateTimeOffset>` keyed by schedule key. On startup the dict
is empty so all groups run immediately on first poll. After each successful group execution the worker
calls `RecordRun` and subsequent polls respect the configured interval.

Schedule keys used:

| Key | Worker | Default interval |
|-----|--------|-----------------|
| `worker.core.reference` | Core | 86400 s (1 day) |
| `worker.football.reference` | Football | 3600 s (1 hour) |
| `worker.football.standings` | Football | 1800 s (30 min) |
| `worker.football.transfers` | Football | 3600 s (1 hour) |
| `worker.football.tv-stations` | Football | 3600 s (1 hour) |
| `worker.football.news` | Football | 900 s (15 min) |
| `worker.football.fixture` | Football | 300 s (5 min) |
| `worker.odds.reference` | Odds | 86400 s (1 day) |

## Configuration Added

### Football appsettings.json — `SportMonksWorkerSettings`

```json
"SportMonksWorkerSettings": {
  "PollingIntervalSeconds": 60,
  "ReferenceDataIntervalSeconds": 3600,
  "StandingsIntervalSeconds": 1800,
  "TransfersIntervalSeconds": 3600,
  "TvStationsIntervalSeconds": 3600,
  "NewsIntervalSeconds": 900,
  "FixtureIntervalSeconds": 300
}
```

### Odds appsettings.json — `SportMonksOddsWorkerSettings`

```json
"SportMonksOddsWorkerSettings": {
  "PollingIntervalSeconds": 60,
  "OddsReferenceIntervalSeconds": 86400
}
```

### Core appsettings.json — `SportMonksCoreWorkerSettings`

```json
"SportMonksCoreWorkerSettings": {
  "CoreReferenceIntervalSeconds": 86400
}
```

## Scope

### Files Added

- `PreOddsApi.ExternalApis/SportMonks/Sync/ISyncJobScheduler.cs`
- `PreOddsApi.ExternalApis/SportMonks/Sync/SyncJobScheduler.cs`

### Files Updated

- `PreOddsApi.ExternalApis/DependencyInjection/SportMonksApiServiceCollectionExtensions.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/WorkerServices/FootballWorkerService.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Odds/WorkerServices/OddsWorkerService.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Core/WorkerServices/CoreWorkerService.cs`
- `PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/appsettings.json`
- `PreOddsApi.Worker/SportMonks/SportMonks.Odds/appsettings.json`
- `PreOddsApi.Worker/SportMonks/SportMonks.Core/appsettings.json`

## Out of Scope

- Distributed job scheduler (Quartz.NET, Hangfire)
- Cross-worker coordination
- Retry with exponential backoff
- Rate limiting at the API client level
- Prematch / inplay odds sync (Task 30)

## Safety Notes

- No live SportMonks import is run in this task.
- All existing feature flags remain defaulted to `false`.
- No new database tables or schema changes.
- `ISyncJobScheduler` is registered as singleton; schedule state is in-process only.

## Acceptance Tests

1. `dotnet build PreOddsApi.sln --no-restore` completes with zero errors.
2. `ISyncJobScheduler` is registered in DI.
3. `OddsWorkerService` no longer uses a 1-second `Task.Delay`.
4. `FootballWorkerService` uses `ISyncJobScheduler` for each job group.
5. `CoreWorkerService` loops rather than running once.
6. All three workers read interval settings from config with documented defaults.
7. Repository does not contain a real SportMonks token.
