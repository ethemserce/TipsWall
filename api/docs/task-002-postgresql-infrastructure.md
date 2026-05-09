# Task 2 - PostgreSQL Infrastructure

Branch: `task/002-postgresql-infrastructure`

## Scope

This task adds the first PostgreSQL-ready infrastructure layer while keeping the existing MySQL path available.

## Changes

- Added provider selection through `PreOddsDatabaseOptions`.
- Added PostgreSQL provider support with `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Kept MySQL as a supported provider for transition.
- Added local PostgreSQL Docker Compose file.
- Added SQL baseline for schemas and sync infrastructure tables.
- Added tracked `appsettings.example.json` files with placeholders only.

## Provider Selection

The application reads the database provider from:

1. `PREODDS_DB_PROVIDER`
2. `DatabaseProvider` in configuration
3. fallback: `mysql`

Supported values:

- `postgresql`
- `postgres`
- `mysql`

## Connection String Selection

For PostgreSQL:

1. `PREODDS_POSTGRES_CONNECTION`
2. `ConnectionStrings:PreOddsApiPostgresDb`

For MySQL:

1. `PREODDS_MYSQL_CONNECTION`
2. `ConnectionStrings:PreOddsApiMySqlDb`

## Local PostgreSQL

Start local PostgreSQL:

```powershell
docker compose -f docker-compose.postgres.yml up -d
```

The init SQL creates:

- PostgreSQL extension: `pgcrypto`
- Schemas: `catalog`, `competition`, `football`, `odds`, `analytics`, `app`, `sync`
- Sync foundation tables:
  - `sync.sync_jobs`
  - `sync.sync_cursors`
  - `sync.raw_payloads`
  - `sync.api_requests`
  - `sync.legacy_id_map`

## Acceptance Criteria

Task 2 is accepted when:

1. `dotnet restore` succeeds.
2. `dotnet build PreOddsApi.sln --no-restore` succeeds.
3. The only tracked config files are examples with placeholder secrets.
4. PostgreSQL can be selected without removing MySQL support.
5. Local PostgreSQL can be started with `docker-compose.postgres.yml`.
