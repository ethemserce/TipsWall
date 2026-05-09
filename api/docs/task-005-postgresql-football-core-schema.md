# Task 5 - PostgreSQL Football Core Schema

Branch: `task/005-postgresql-football-core-schema`

## Scope

This task creates the core PostgreSQL DDL for SportMonks v3 football data.

It covers the base entities needed before odds, analytics, and richer fixture detail sync can be designed.

## Inputs Reviewed

- Existing legacy entities: `team`, `player`, `coach`, `referee`, `venue`, `fixture`, `score`, `period`, `rival`, `squad`
- Existing SportMonks v3 models for fixtures, participants, scores, periods, teams, players, coaches, referees, venues, rivals, and squads
- SportMonks v3 docs:
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/fixtures
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/teams
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/players
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/venues
  - https://docs.sportmonks.com/v3/endpoints-and-entities/endpoints/referees

## Tables Added

- `football.venues`
- `football.teams`
- `football.players`
- `football.coaches`
- `football.referees`
- `football.team_rivals`
- `football.team_squads`
- `football.fixtures`
- `football.fixture_participants`
- `football.fixture_scores`
- `football.fixture_periods`
- `football.fixture_referees`

## Design Decisions

- Provider-owned rows use SportMonks v3 `id` as the PostgreSQL primary key.
- Fixtures do not store `local_team_id` or `visitor_team_id`; v3 participants are stored in `football.fixture_participants`.
- Fixture scores are stored as typed rows in `football.fixture_scores`; denormalized half-time/full-time score columns are not used.
- Participant metadata is preserved in `raw_meta jsonb` so home/away/winner/position can evolve without losing source detail.
- Period source timestamps are represented with both timestamp columns and raw Unix timestamp columns.
- Standing/top scorer/aggregate scalar references from Task 4 are linked to football tables once the relevant football tables exist.
- Events, statistics, lineups, formations, TV stations, news, transfers, weather, and fixture trends are intentionally deferred to later tasks.

## Script

SQL file:

```text
database/postgres/004-create-football-core-schema.sql
```

## Acceptance Criteria

Task 5 is accepted when:

1. Football core PostgreSQL tables are defined.
2. Fixture participants and scores follow SportMonks v3 modeling decisions from Task 1.
3. Script can be applied after baseline, catalog, and competition scripts.
4. Script does not create odds, analytics, app, or detail-heavy fixture tables.
5. Existing application build still succeeds.
