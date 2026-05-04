# Task 025 - PostgreSQL Transfer and Sidelined Sync Writer

## Goal

Add PostgreSQL sync support for SportMonks football transfers and fixture sidelined players without performing a historical backfill.

## SportMonks Endpoints and Entities

Official SportMonks API v3 references used for this task:

- `transfers/latest`: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/transfers/get-latest-transfers
- Fixture `sidelined` entity: https://docs.sportmonks.com/v3/endpoints-and-entities/entities/fixture
- Sidelined player tutorial include example: https://docs.sportmonks.com/v3/tutorials-and-guides/tutorials/lineups-and-formations

The worker uses `transfers/latest` instead of broad historical transfer endpoints. Fixture sidelined rows are collected only as part of the configured fixture date window.

## Scope

- Add `ISportMonksTransferSidelinedWriter`.
- Add `SportMonksTransferSidelinedWriter`.
- Register the writer in SportMonks DI.
- Upsert transfer rows into `football.transfers`.
- Upsert fixture sidelined detail rows into `football.sidelined_players`.
- Upsert fixture-to-sidelined links into `football.fixture_sidelined`.
- Upsert nested transfer sports, types, positions, players, and teams when provided by SportMonks.
- Upsert nested sidelined players, teams, and types when provided by SportMonks.
- Extend the football worker with an opt-in `SportMonksTransferSidelinedSync` block.
- Make SportMonks v3 transfer/sidelined DTOs more tolerant of nullable and nested API fields.

## Runtime Controls

`SportMonksTransferSidelinedSync` defaults to disabled.

```json
{
  "SportMonksTransferSidelinedSync": {
    "Enabled": false,
    "SyncLatestTransfers": true,
    "TransferOrder": "desc",
    "SyncFixtureSidelined": false
  }
}
```

- `Enabled`: enables the whole transfer/sidelined sync block.
- `SyncLatestTransfers`: calls `transfers/latest`.
- `TransferOrder`: passes the SportMonks `order` query parameter. Default is `desc`.
- `SyncFixtureSidelined`: adds sidelined includes to fixture date-window requests and writes fixture sidelined rows.

## Safety Notes

- No live SportMonks import is run in this task.
- Transfers and sidelined rows are written with PostgreSQL `on conflict` upserts.
- Optional FK-sensitive references use existing rows when available.
- Nested players, teams, sports, and types are upserted when included in the SportMonks response.
- Transfer `amount` is parsed defensively because the SportMonks field is documented as a string.
- Fixture sidelined sync remains separately disabled by default because it increases fixture include payload size.
- The repository must not contain the real SportMonks token.

## Out of Scope

- Historical transfer backfill.
- Transfer rumours.
- TV stations, weather, trends, comments, highlights, and news.
- Player/coach statistics.
- Worker cursor tuning and rate-limit scheduling changes.
- Web/mobile read API changes.

## Acceptance Tests

1. `dotnet build PreOddsApi.sln --no-restore` completes with zero errors.
2. `ISportMonksTransferSidelinedWriter` is registered in SportMonks DI.
3. Football worker can call `transfers/latest` when `SportMonksTransferSidelinedSync:Enabled=true` and `SportMonksTransferSidelinedSync:SyncLatestTransfers=true`.
4. Football worker adds fixture sidelined includes only when `SportMonksTransferSidelinedSync:Enabled=true` and `SportMonksTransferSidelinedSync:SyncFixtureSidelined=true`.
5. Transfer and fixture sidelined writes are idempotent through PostgreSQL `on conflict` upserts.
6. The repository does not contain a real SportMonks token.
