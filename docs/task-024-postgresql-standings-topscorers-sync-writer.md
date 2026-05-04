# Task 024 - PostgreSQL Standings and Top Scorers Sync Writer

## Goal

Add PostgreSQL sync support for SportMonks football standings and top scorers without performing a historical backfill.

## SportMonks Endpoints

Official SportMonks API v3 endpoint references used for this task:

- `standings/seasons/{ID}`: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/standings/get-standings-by-season-id
- `topscorers/seasons/{ID}`: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/topscorers/get-topscorers-by-season-id

The worker uses season-scoped endpoints instead of broad historical endpoints. By default it selects only current seasons from the league hierarchy already fetched by the football worker.

## Scope

- Add `ISportMonksStandingTopScorerWriter`.
- Add `SportMonksStandingTopScorerWriter`.
- Register the writer in SportMonks DI.
- Upsert standing rows into `competition.standings`.
- Upsert standing rules into `competition.standing_rules`.
- Upsert standing details into `competition.standing_details`.
- Upsert standing forms into `competition.standing_forms`.
- Upsert top scorer rows into `competition.top_scorers`.
- Upsert nested participant teams into `football.teams` when provided by SportMonks.
- Upsert nested top scorer players into `football.players` when provided by SportMonks.
- Upsert nested standing/top scorer types into `catalog.types` when provided by SportMonks.
- Extend the football worker with an opt-in `SportMonksStandingSync` block.
- Add `season_id` to the SportMonks v3 `TopScorer` DTO.

## Runtime Controls

`SportMonksStandingSync` defaults to disabled.

```json
{
  "SportMonksStandingSync": {
    "Enabled": false,
    "SyncStandings": true,
    "SyncTopScorers": true,
    "CurrentSeasonsOnly": true,
    "MaxSeasonsPerRun": 0
  }
}
```

- `Enabled`: enables the whole standings/top scorers block.
- `SyncStandings`: calls `standings/seasons/{ID}`.
- `SyncTopScorers`: calls `topscorers/seasons/{ID}`.
- `CurrentSeasonsOnly`: prevents historical backfill by using only current seasons from the league sync payload.
- `MaxSeasonsPerRun`: limits season-scoped calls per worker loop. `0` means no explicit limit after the current-season filter.

## Safety Notes

- No live SportMonks import is run in this task.
- Standings and top scorers are written with PostgreSQL `on conflict` upserts.
- Optional FK-sensitive references use existing rows when available.
- Participant teams, top scorer players, and nested types are upserted when included in the SportMonks response.
- Standing detail/form children are refreshed only when their collections are included in the response.
- `CurrentSeasonsOnly` remains enabled by default to avoid historical data transfer.

## Out of Scope

- Transfers.
- Sidelined players.
- TV stations, weather, trends, comments, highlights, and news.
- Historical standings/top scorers backfill.
- Web/mobile read API changes.

## Acceptance Tests

1. `dotnet build PreOddsApi.sln --no-restore` completes with zero errors.
2. `ISportMonksStandingTopScorerWriter` is registered in SportMonks DI.
3. Football worker can call `standings/seasons/{ID}` when `SportMonksStandingSync:Enabled=true` and `SportMonksStandingSync:SyncStandings=true`.
4. Football worker can call `topscorers/seasons/{ID}` when `SportMonksStandingSync:Enabled=true` and `SportMonksStandingSync:SyncTopScorers=true`.
5. Current-season filtering is enabled by default through `SportMonksStandingSync:CurrentSeasonsOnly=true`.
6. The repository does not contain a real SportMonks token.
