# Task 6 - PostgreSQL Football Detail Schema

Branch: `task/006-postgresql-football-detail-schema`

## Scope

This task adds the detailed football tables that hang off the football core schema.

It covers fixture detail, media/reference links, news, and transfers. It does not add odds, analytics, app tables, EF entities, sync workers, or ETL logic.

## Inputs Reviewed

- Existing legacy entities: `events`, `statistic`, `lineup`, `bench`, `formation`, `sidelined`, `tvstation`, `weather_report`, `trend`, `comment`, `commentary`, `highlight`, `news`, `newsItemLine`, `transfer`
- Existing SportMonks v3 models for events, statistics, lineups, formations, sidelined players, TV stations, weather, trends, commentaries, news, and transfers
- SportMonks v3 docs:
  - https://docs.sportmonks.com/v3/endpoints-and-entities/entities/fixture
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/tv-stations
  - https://docs.sportmonks.com/football/endpoints-and-entities/endpoints

## Tables Added

- `football.fixture_events`
- `football.fixture_statistics`
- `football.fixture_lineups`
- `football.fixture_lineup_details`
- `football.fixture_formations`
- `football.sidelined_players`
- `football.fixture_sidelined`
- `football.tv_stations`
- `football.tv_station_countries`
- `football.fixture_tv_stations`
- `football.fixture_weather_reports`
- `football.fixture_trends`
- `football.fixture_commentaries`
- `football.fixture_comments`
- `football.fixture_highlights`
- `football.news`
- `football.news_lines`
- `football.transfers`

## Design Decisions

- Provider-owned rows use SportMonks v3 `id` as the PostgreSQL primary key.
- Fixture events are normalized by fixture, period, participant, type, player, related player, and coach.
- Fixture statistics are typed rows; missing statistics remain absent or `NULL`, never forced to zero.
- `raw_data jsonb` is kept for statistic and lineup-detail payloads because SportMonks can return different shapes per statistic/detail type.
- Lineups and bench are modeled as one `football.fixture_lineups` table, with `type_id` carrying the source classification.
- Weather fields that can be object-shaped in SportMonks responses are stored as `jsonb`.
- TV stations are separated from fixture broadcasts and country availability.
- `football.team_squads.transfer_id` is linked to `football.transfers` now that transfers exist.

## Script

SQL file:

```text
database/postgres/005-create-football-detail-schema.sql
```

## Acceptance Criteria

Task 6 is accepted when:

1. Football detail PostgreSQL tables are defined.
2. Script can be applied after baseline, catalog, competition, and football core scripts.
3. Missing statistics are not converted to zero in the schema.
4. Script does not create odds, analytics, or app-owned tables.
5. Existing application build still succeeds.
