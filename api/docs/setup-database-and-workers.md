# Setup: Database and Workers

End-to-end guide to bring up PostgreSQL with all tables and run the SportMonks
sync workers locally.

## Prerequisites

- Docker Desktop running (for PostgreSQL + migrator)
- .NET 8 SDK installed (for running workers locally)
- A SportMonks API token (free tier works for testing)
- PowerShell or bash

## Step 1: Start PostgreSQL with all migrations applied

```powershell
docker compose -f docker-compose.postgres.yml up -d --build
```

The host port defaults to `15432` (override with `POSTGRES_HOST_PORT=5432` env
var if `5432` is free). The container internal port is always `5432`.

What this does:
- Starts `preodds-postgres` (PostgreSQL 16 Alpine).
- On the **first** volume creation, Postgres entrypoint applies every
  `database/postgres/*.sql` file alphabetically.
- After Postgres is healthy, **`preodds-migrator`** runs once. It applies any
  SQL files added since the last run, tracking them in `sync.schema_migrations`.
  The migrator exits 0 when done.

Wait until both services have reported success:

```powershell
docker compose -f docker-compose.postgres.yml ps
docker compose -f docker-compose.postgres.yml logs migrator
```

You should see `Done: N new file(s) applied.` in the migrator output.

## Step 2: Verify all tables exist

```powershell
docker exec -i preodds-postgres psql -U preodds -d preodds `
    -f - < database/scripts/verify-tables.sql
```

Last result row should show `missing_count = 0`.

## Step 3: Configure workers

Each worker reads from `appsettings.json` (gitignored). Copy the example and add
your SportMonks token:

```powershell
Copy-Item PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/appsettings.example.json `
          PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/appsettings.json
Copy-Item PreOddsApi.Worker/SportMonks/SportMonks.Core/appsettings.example.json `
          PreOddsApi.Worker/SportMonks/SportMonks.Core/appsettings.json
Copy-Item PreOddsApi.Worker/SportMonks/SportMonks.Odds/appsettings.example.json `
          PreOddsApi.Worker/SportMonks/SportMonks.Odds/appsettings.json
```

Edit each `appsettings.json` and set:

- `SportMonks.ApiToken` — your SportMonks token
- `ConnectionStrings.PreOddsApiPostgresDb` — change `Port=5432` to `Port=15432`
  if you used the default host port from Step 1; otherwise keep as-is.

For a **minimal pilot run**, in `SportMonks.Football.Fixture/appsettings.json`
flip these flags to `true`:

- `SportMonksFixtureSync.Enabled`

Leave odds, news, players, transfers, analytics flags as `false` for the first run.

Optionally use the pilot preset directly:

```powershell
Copy-Item PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/appsettings.pilot.example.json `
          PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/appsettings.json
```

## Step 4: Run the workers

In separate terminals:

```powershell
# Football worker (fixtures, optional odds/news/players)
dotnet run --project PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture

# Core worker (catalog reference data)
dotnet run --project PreOddsApi.Worker/SportMonks/SportMonks.Core

# Odds worker (bookmakers, markets reference)
dotnet run --project PreOddsApi.Worker/SportMonks/SportMonks.Odds
```

First poll typically runs within 60 seconds. Watch the console for log lines like:

```
SportMonks sync job sportmonks.football.fixtures.by-date completed with 12 items.
```

## Step 5: Verify the sync is working

Two ways:

### Via PostgreSQL directly

```powershell
docker exec -i preodds-postgres psql -U preodds -d preodds -c `
    "select job_key, last_success_at, last_error from sync.sync_jobs j left join sync.sync_cursors c on c.sync_job_id = j.id order by last_success_at desc nulls last;"
```

Recent successful runs should have `last_success_at` populated and `last_error` null.

### Via WebApi admin endpoint

After signing in to get a JWT (POST `/api/v3/auth/token`):

```
GET /api/v3/admin/sync-status
Authorization: Bearer <token>
```

Returns the same job/cursor state in JSON.

## Troubleshooting

### Migrator fails with "FAIL: file content changed"

A previously applied SQL file's content changed. To reset (DROP everything!):

```powershell
docker compose -f docker-compose.postgres.yml down -v
docker compose -f docker-compose.postgres.yml up -d --build
```

Or fix the SQL change to be additive (use `alter table ... if not exists`) and
manually update `sync.schema_migrations` checksum.

### Worker logs `SportMonksApiException 401 Unauthorized`

`SportMonks.ApiToken` is wrong or missing. Check `appsettings.json`.

### Worker logs `SportMonksApiException 429 Too Many Requests`

Quota exceeded. Lower polling intervals via `SportMonksWorkerSettings.*IntervalSeconds`
or reduce `SportMonks.DefaultPerPage`.

### No data appearing in `football.fixtures`

Check `SportMonksFixtureSync.Enabled` is `true` and the date window
(`DaysBack`/`DaysForward`) includes a date with active fixtures.

## What the database contains

After Step 2 succeeds, you'll have these schemas populated with empty tables:

- `sync` — job tracking, raw payloads, api request audit, schema migrations
- `catalog` — sports, countries, continents, regions, cities, types, states
- `competition` — leagues, seasons, stages, rounds, standings, top scorers
- `football` — fixtures, teams, players, coaches, events, statistics, lineups, news, transfers, TV stations
- `odds` — bookmakers, markets, prematch & inplay odds (current + history)
- `analytics` — analysis snapshots, fixture signals, hot/winning/earning rate results, season aggregates
- `app` — users, auth identities, devices, favorites, tips, coupons, notifications, contact, refresh tokens

Workers populate the SportMonks-owned tables (catalog, competition, football,
odds). The app schema is populated by user-driven V3 endpoints.
