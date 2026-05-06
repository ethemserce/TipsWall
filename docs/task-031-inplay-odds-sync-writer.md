# Task 031 - PostgreSQL Inplay Odds Sync Writer

## Goal

Add PostgreSQL sync support for SportMonks standard in-play odds. Upsert current odds into
`odds.inplay_odds_current` and append a history row to `odds.inplay_odds_history` whenever a
value change is detected. Rate-limit API calls with a configurable per-request delay to avoid
exhausting the SportMonks inplay quota during live match windows.

## SportMonks Endpoints and Entities

- Inplay odds overview: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/standard-odds-feed/inplay-odds
- GET Latest updated inplay odds: `football/odds/inplay/latest`
- GET Inplay odds by fixture: `football/odds/inplay/fixtures/{fixtureId}`

In-play odds are not available as fixture includes; they must be fetched from the dedicated
endpoints listed above.

## Scope

- Add `ISportMonksInplayOddsWriter`.
- Add `SportMonksInplayOddsWriter` writing to:
  - `odds.inplay_odds_current` — upsert on `(feed_type, fixture_id, bookmaker_id, market_id, outcome_key)`.
  - `odds.inplay_odds_history` — insert when `value` changes vs. current row.
- Register the writer in SportMonks DI.
- Add `MaybeRunLatestInplayOddsAsync` to `FootballWorkerService` using `odds/inplay/latest`.

## Runtime Controls

`SportMonksInplayOddsSync` defaults to disabled.

```json
{
  "SportMonksWorkerSettings": {
    "InplayOddsIntervalSeconds": 30
  },
  "SportMonksInplayOddsSync": {
    "Enabled": false,
    "SyncLatestOdds": true,
    "RecordHistory": true,
    "RequestDelayMs": 1000
  }
}
```

- `Enabled`: master switch.
- `SyncLatestOdds`: polls `odds/inplay/latest` on each scheduled run.
- `RecordHistory`: inserts a history row when value changes.
- `RequestDelayMs`: minimum milliseconds between API page requests to respect rate limits.
  Forwarded to the sync runner via `SportMonksApiRequest`.

## Key Differences from Prematch (Task 030)

| Aspect | Prematch | Inplay |
|--------|----------|--------|
| Fixture include | Supported via `SyncFixtureOdds` | Not available |
| Primary endpoint | `odds/latest` or fixture include | `odds/inplay/latest` |
| Extra fields | — | `external_id`, `suspended` |
| History FKs | Nullable (fixture/bookmaker/market) | NOT NULL |
| Default interval | 300 s | 30 s |

## Safety Notes

- No live SportMonks import is run in this task.
- All new runtime switches default to disabled.
- `odds/inplay/latest` is only polled when `Enabled=true` and `SyncLatestOdds=true`.
- `RecordHistory` guards history inserts behind a value-change check.
- The repository must not contain the real SportMonks token.

## Out of Scope

- Premium inplay odds feed.
- Per-fixture inplay polling loop (separate task if needed).
- Odds analytics / signal calculations (Task 37).
- Web / mobile read API changes.

## Acceptance Tests

1. `dotnet build PreOddsApi.sln --no-restore` completes with zero errors.
2. `ISportMonksInplayOddsWriter` is registered in DI.
3. When `SportMonksInplayOddsSync:Enabled=false` (default), no inplay endpoint is called.
4. Inplay rows are idempotently written to `odds.inplay_odds_current`.
5. A history row is inserted to `odds.inplay_odds_history` when `RecordHistory=true` and value differs.
6. Repository does not contain a real SportMonks token.
