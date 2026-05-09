# Task 028 - PostgreSQL News Sync Writer

## Goal

Add PostgreSQL sync support for SportMonks pre-match and post-match news without running a historical import.

## SportMonks Endpoints and Entities

Official SportMonks API v3 references used for this task:

- News endpoints overview: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/news
- GET Pre-Match News: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/news/get-pre-match-news
- GET Pre-Match News for Upcoming Fixtures: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/news/get-pre-match-news-for-upcoming-fixtures
- GET Post-Match News: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/news/get-post-match-news
- News and NewsItemLine entities: https://docs.sportmonks.com/v3/endpoints-and-entities/entities/other
- Fixture entity include options: https://docs.sportmonks.com/v3/endpoints-and-entities/entities/fixture

SportMonks exposes `fixture`, `league`, and `lines` includes for news. This task stores news headers in `football.news` and article lines in `football.news_lines`.

## Scope

- Add `ISportMonksNewsWriter`.
- Add `SportMonksNewsWriter`.
- Register the writer in SportMonks DI.
- Extend the `News` DTO with `fixture`, `league`, and `lines` includes.
- Upsert standalone pre-match, upcoming pre-match, and post-match news into PostgreSQL when explicitly enabled.
- Upsert fixture `prematchNews` and `postmatchNews` includes after fixture core data is written.
- Extend the football worker with an opt-in `SportMonksNewsSync` block.

## Runtime Controls

`SportMonksNewsSync` defaults to disabled.

```json
{
  "SportMonksUrls": {
    "preMatchNews": "news/pre-match",
    "preMatchNewsUpcoming": "news/pre-match/upcoming",
    "postMatchNews": "news/post-match"
  },
  "SportMonksNewsSync": {
    "Enabled": false,
    "SyncAllPreMatchNews": false,
    "SyncUpcomingPreMatchNews": false,
    "SyncAllPostMatchNews": false,
    "SyncFixturePreMatchNews": false,
    "SyncFixturePostMatchNews": false,
    "IncludeLines": true,
    "Order": "desc"
  }
}
```

- `Enabled`: enables the whole news sync block.
- `SyncAllPreMatchNews`: calls the general pre-match news endpoint.
- `SyncUpcomingPreMatchNews`: calls the upcoming-fixtures pre-match news endpoint.
- `SyncAllPostMatchNews`: calls the general post-match news endpoint.
- `SyncFixturePreMatchNews`: adds `prematchNews` to fixture date-window requests.
- `SyncFixturePostMatchNews`: adds `postmatchNews` to fixture date-window requests.
- `IncludeLines`: adds `lines` to standalone news requests and nested fixture news requests.
- `Order`: forwards SportMonks `order` query parameter for standalone news requests.

## Safety Notes

- No live SportMonks import is run in this task.
- All new runtime switches default to disabled.
- News and line writes are idempotent through PostgreSQL `on conflict` upserts.
- FK-sensitive fixture and league references are applied only when the referenced rows already exist.
- Synthetic negative ids are generated only when SportMonks does not provide row ids.
- The repository must not contain the real SportMonks token.

## Out of Scope

- Season-specific news endpoints, because they are more likely to be used for historical backfill.
- `football.fixture_comments`, which remains reserved for legacy comment-style rows if still needed.
- Video highlights, because SportMonks currently documents video highlights as removed due to service availability.
- Web/mobile read API changes.

## Acceptance Tests

1. `dotnet build PreOddsApi.sln --no-restore` completes with zero errors.
2. `ISportMonksNewsWriter` is registered in SportMonks DI.
3. Standalone news endpoints are called only when `SportMonksNewsSync:Enabled=true` and their specific switch is enabled.
4. Fixture news includes are added only when `SportMonksNewsSync:SyncFixturePreMatchNews=true` or `SportMonksNewsSync:SyncFixturePostMatchNews=true`.
5. News rows are idempotently written to `football.news`.
6. News line rows are idempotently written to `football.news_lines`.
7. The repository does not contain a real SportMonks token.
