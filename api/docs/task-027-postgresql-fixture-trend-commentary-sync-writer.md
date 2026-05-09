# Task 027 - PostgreSQL Fixture Trend and Commentary Sync Writer

## Goal

Add PostgreSQL sync support for SportMonks fixture trend data and fixture commentaries without performing a historical fixture backfill.

## SportMonks Endpoints and Entities

Official SportMonks API v3 references used for this task:

- Fixture include options: https://docs.sportmonks.com/v3/endpoints-and-entities/entities/fixture
- Commentary entity: https://docs.sportmonks.com/v3/endpoints-and-entities/entities/other
- Trends tutorial: https://docs.sportmonks.com/v3/tutorials-and-guides/tutorials/trends

Fixture commentaries are collected through the fixture `comments` include. Trend data is collected through the fixture `trends` include and, when enabled, the feature-specific `pressure` include described by SportMonks.

## Scope

- Add `ISportMonksFixtureTrendCommentaryWriter`.
- Add `SportMonksFixtureTrendCommentaryWriter`.
- Register the writer in SportMonks DI.
- Upsert fixture trends into `football.fixture_trends`.
- Upsert fixture commentaries into `football.fixture_commentaries`.
- Extend the football worker with an opt-in `SportMonksFixtureTimelineSync` block.
- Add `pressure` trend collection support to the fixture DTO.
- Correct the fixture `comments` DTO mapping to use SportMonks `Commentary` rows instead of `News`.
- Make trend and commentary DTOs more tolerant of nullable SportMonks fields.

## Runtime Controls

`SportMonksFixtureTimelineSync` defaults to disabled.

```json
{
  "SportMonksFixtureTimelineSync": {
    "Enabled": false,
    "SyncTrends": false,
    "SyncPressureTrends": false,
    "SyncCommentaries": false
  }
}
```

- `Enabled`: enables the whole fixture timeline sync block.
- `SyncTrends`: adds `trends` to fixture date-window requests.
- `SyncPressureTrends`: adds `pressure` to fixture date-window requests.
- `SyncCommentaries`: adds `comments` to fixture date-window requests.

## Safety Notes

- No live SportMonks import is run in this task.
- Trends and commentaries are written with PostgreSQL `on conflict` upserts.
- FK-sensitive writes use existing fixture, team, type, and period rows when available.
- Synthetic negative ids are generated only when SportMonks does not provide row ids.
- Fixture timeline includes remain separately disabled by default because they increase fixture payload size.
- The repository must not contain the real SportMonks token.

## Out of Scope

- `football.fixture_comments`, which is reserved for legacy/comment-like rows if still needed.
- Video highlights.
- Prematch/postmatch news and news lines.
- Pressure-specific analytics calculations.
- Historical fixture backfill.
- Web/mobile read API changes.

## Acceptance Tests

1. `dotnet build PreOddsApi.sln --no-restore` completes with zero errors.
2. `ISportMonksFixtureTrendCommentaryWriter` is registered in SportMonks DI.
3. Football worker adds `trends` to fixture includes only when `SportMonksFixtureTimelineSync:SyncTrends=true`.
4. Football worker adds `pressure` to fixture includes only when `SportMonksFixtureTimelineSync:SyncPressureTrends=true`.
5. Football worker adds `comments` to fixture includes only when `SportMonksFixtureTimelineSync:SyncCommentaries=true`.
6. Trend and commentary writes are idempotent through PostgreSQL `on conflict` upserts.
7. The repository does not contain a real SportMonks token.
