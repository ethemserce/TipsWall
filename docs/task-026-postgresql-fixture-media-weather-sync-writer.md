# Task 026 - PostgreSQL Fixture Media and Weather Sync Writer

## Goal

Add PostgreSQL sync support for SportMonks football TV stations and fixture weather reports without performing a historical fixture backfill.

## SportMonks Endpoints and Entities

Official SportMonks API v3 references used for this task:

- `tv-stations`: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/tv-stations/get-all-tv-stations
- Fixture includes: https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/fixtures/get-all-fixtures
- TV station and weather entities: https://docs.sportmonks.com/v3/endpoints-and-entities/entities/other

The worker uses the global `tv-stations` endpoint for reference data. Fixture TV station links and weather reports are collected only as part of the configured fixture date window through `tvStations` and `weatherReport` includes.

## Scope

- Add `ISportMonksFixtureMediaWeatherWriter`.
- Add `SportMonksFixtureMediaWeatherWriter`.
- Register the writer in SportMonks DI.
- Upsert TV station rows into `football.tv_stations`.
- Upsert TV station country links into `football.tv_station_countries` when matching catalog countries already exist.
- Upsert fixture TV station links into `football.fixture_tv_stations`.
- Upsert fixture weather reports into `football.fixture_weather_reports`.
- Extend the football worker with an opt-in `SportMonksFixtureMediaWeatherSync` block.
- Make SportMonks v3 TV station and weather report DTOs more tolerant of nullable and JSON-shaped API fields.

## Runtime Controls

`SportMonksFixtureMediaWeatherSync` defaults to disabled.

```json
{
  "SportMonksFixtureMediaWeatherSync": {
    "Enabled": false,
    "SyncTvStations": true,
    "TvStationOrder": "asc",
    "SyncTvStationCountries": true,
    "SyncFixtureTvStations": false,
    "SyncWeatherReports": false
  }
}
```

- `Enabled`: enables the whole media/weather sync block.
- `SyncTvStations`: calls `tv-stations`.
- `TvStationOrder`: passes the SportMonks `order` query parameter. Default is `asc`.
- `SyncTvStationCountries`: includes `countries` on the global TV station reference call.
- `SyncFixtureTvStations`: adds `tvStations` to fixture date-window requests and writes fixture-TV links.
- `SyncWeatherReports`: adds `weatherReport` to fixture date-window requests and writes fixture weather rows.

## Safety Notes

- No live SportMonks import is run in this task.
- TV stations and weather reports are written with PostgreSQL `on conflict` upserts.
- `tv_station_countries` links are inserted only when the country already exists in `catalog.countries`.
- Fixture TV station links are inserted only when both fixture and TV station rows exist.
- Weather reports use the fixture id as fallback when SportMonks does not provide a report id.
- Fixture media/weather includes remain separately disabled by default because they increase fixture payload size.
- The repository must not contain the real SportMonks token.

## Out of Scope

- Fixture trends.
- Commentaries and comments.
- Video highlights.
- Prematch/postmatch news and news lines.
- Historical fixture backfill.
- Web/mobile read API changes.

## Acceptance Tests

1. `dotnet build PreOddsApi.sln --no-restore` completes with zero errors.
2. `ISportMonksFixtureMediaWeatherWriter` is registered in SportMonks DI.
3. Football worker can call `tv-stations` when `SportMonksFixtureMediaWeatherSync:Enabled=true` and `SportMonksFixtureMediaWeatherSync:SyncTvStations=true`.
4. Football worker adds `tvStations` to fixture includes only when `SportMonksFixtureMediaWeatherSync:SyncFixtureTvStations=true`.
5. Football worker adds `weatherReport` to fixture includes only when `SportMonksFixtureMediaWeatherSync:SyncWeatherReports=true`.
6. TV station, fixture TV station, and weather report writes are idempotent through PostgreSQL `on conflict` upserts.
7. The repository does not contain a real SportMonks token.
