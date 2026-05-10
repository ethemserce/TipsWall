# Task 030 - PostgreSQL Prematch Odds Sync Writer

## Goal

Add PostgreSQL sync support for SportMonks standard pre-match odds. Upsert current odds into
`odds.prematch_odds_current` and append a history row to `odds.prematch_odds_history` whenever
a value change is detected. Optionally upsert bookmaker fixture mappings.

## SportMonks Endpoints and Entities

- Pre-match odds overview: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/standard-odds-feed/pre-match-odds
- GET Pre-match odds by fixture: `football/odds/fixtures/{fixtureId}`
- GET Latest updated pre-match odds: `football/odds/latest`
- Odds entity: https://docs.sportmonks.com/v3/endpoints-and-entities/entities/odds

Odds are also available as a fixture include (`odds`, `odds.market`, `odds.bookmaker`) on the
fixture date-window endpoint already used by the football worker.

## Scope

- Add `ISportMonksPrematchOddsWriter`.
- Add `SportMonksPrematchOddsWriter` writing to:
  - `odds.prematch_odds_current` — upsert on `(feed_type, fixture_id, bookmaker_id, market_id, outcome_key)`.
  - `odds.prematch_odds_history` — insert when `value` changes vs. the current row.
- Register the writer in SportMonks DI.
- Extend `Fixture` includes in `FootballWorkerService` with `odds` when `SportMonksPrematchOddsSync:Enabled=true`.
- Add a `MaybeRunPrematchOddsAsync` group to `FootballWorkerService` for the `odds/latest` polling path.

## Runtime Controls

`SportMonksPrematchOddsSync` defaults to disabled.

```json
{
  "SportMonksWorkerSettings": {
    "PrematchOddsIntervalSeconds": 300
  },
  "SportMonksPrematchOddsSync": {
    "Enabled": false,
    "SyncFixtureOdds": true,
    "SyncLatestOdds": false,
    "IncludeMarket": true,
    "IncludeBookmaker": true,
    "RecordHistory": true
  }
}
```

- `Enabled`: master switch for all prematch odds sync.
- `SyncFixtureOdds`: includes `odds` in the fixture date-window include list so odds are fetched together with fixtures.
- `SyncLatestOdds`: polls `odds/latest` endpoint independently for recently updated odds.
- `IncludeMarket`: adds `odds.market` to the fixture include.
- `IncludeBookmaker`: adds `odds.bookmaker` to the fixture include.
- `RecordHistory`: inserts a history row when `value` changes.

## Safety Notes

- No live SportMonks import is run in this task.
- All new runtime switches default to disabled.
- Writes are idempotent: `prematch_odds_current` uses `on conflict` upsert; history inserts are
  guarded by a value-change check.
- FK-sensitive fixture, market, and bookmaker references are applied only when the referenced rows
  already exist (using `on conflict do nothing` or FK violation handling).
- The repository must not contain the real SportMonks token.

## Out of Scope

- Premium odds feed (separate feed_type, separate task).
- Inplay odds (Task 31).
- Odds analytics / hot rate calculations (Task 37).
- Web / mobile read API changes.

## Acceptance Tests

1. `dotnet build PreOddsApi.sln --no-restore` completes with zero errors.
2. `ISportMonksPrematchOddsWriter` is registered in DI.
3. When `SportMonksPrematchOddsSync:Enabled=false` (default), no odds includes are added to fixture requests and no odds endpoints are called.
4. When `SyncFixtureOdds=true`, fixture requests include `odds`.
5. Odds rows are idempotently written to `odds.prematch_odds_current`.
6. A history row is inserted to `odds.prematch_odds_history` when `RecordHistory=true` and the value differs from the current row.
7. Repository does not contain a real SportMonks token.
