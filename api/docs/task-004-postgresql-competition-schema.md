# Task 4 - PostgreSQL Competition Schema

Branch: `task/004-postgresql-competition-schema`

## Scope

This task creates the PostgreSQL DDL for SportMonks v3 competition data under the `competition` schema.

It does not add EF entities, EF migrations, sync workers, ETL logic, or football-domain tables.

## Inputs Reviewed

- Existing legacy entities: `league`, `season`, `stage`, `round`, `group`, `aggregate`, `standing`, `standing_detail`, `standing_rule`, `standing_form`, `topScorer`
- Existing SportMonks v3 models for leagues, seasons, stages, rounds, groups, aggregates, standings, and top scorers
- SportMonks v3 docs:
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/leagues
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/seasons/get-all-seasons
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/stages/get-all-stages
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/rounds
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/standings
  - https://docs.sportmonks.com/football/endpoints-and-entities/endpoints/topscorers

## Tables Added

- `competition.standing_rules`
- `competition.leagues`
- `competition.seasons`
- `competition.stages`
- `competition.groups`
- `competition.rounds`
- `competition.aggregates`
- `competition.standings`
- `competition.standing_details`
- `competition.standing_forms`
- `competition.top_scorers`

## Design Decisions

- Provider-owned rows use SportMonks v3 `id` as the PostgreSQL primary key.
- Competition hierarchy follows SportMonks v3: league -> season -> stage -> round/group.
- League favorite/status fields from the legacy model are not stored here because they are app-owned concerns.
- Stage type and scorer/detail types reference `catalog.types`.
- `standing_rules` is created before seasons/stages because seasons and stages can point to tie-breaker rules.
- Standing participant IDs, top scorer player IDs, participant IDs, aggregate winner participant IDs, and standing form fixture IDs are stored as scalar IDs for now.
- Foreign keys to `football.teams`, `football.players`, and `football.fixtures` will be added when the football schema exists.
- Dates exposed as `starting_at`/`ending_at` are stored as `date`; recalculation and sync audit timestamps use `timestamptz`.

## Script

SQL file:

```text
database/postgres/003-create-competition-schema.sql
```

## Acceptance Criteria

Task 4 is accepted when:

1. Competition PostgreSQL tables are defined.
2. Tables follow the SportMonks v3 ID strategy from Task 1.
3. Script can be applied after `001-create-baseline.sql` and `002-create-catalog-reference.sql`.
4. The script does not reference future football tables before they exist.
5. Existing application build still succeeds.
