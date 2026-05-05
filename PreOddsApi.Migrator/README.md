# PreOddsApi.Migrator

Standalone console tool that applies SQL files from `database/postgres/` against a
PostgreSQL database, tracking applied scripts in `sync.schema_migrations` by SHA256
checksum.

## Usage

```
PREODDS_POSTGRES_CONNECTION="Host=...;Port=5432;Database=preodds;Username=...;Password=..." \
  dotnet run --project PreOddsApi.Migrator -- [migrations-dir] [--dry-run]
```

- `migrations-dir` defaults to `./database/postgres` relative to the current working
  directory. Pass an absolute path when invoking from elsewhere.
- `--dry-run` lists pending files without applying them.

## Behavior

1. Ensures `sync.schema_migrations` exists.
2. Loads previously-applied file names and checksums.
3. Iterates `*.sql` files in the migrations directory in alphabetical order.
4. For each file:
   - Skips if applied with the same checksum.
   - **Fails** if applied with a different checksum (script content changed).
   - Applies otherwise: runs the file inside a transaction, then records the row.

## Exit codes

- `0`: success.
- `1`: failure (connection error, applied script changed, SQL error).
- `2`: usage error (env var missing, migrations dir not found).

## Notes

- Each script runs in its own transaction. Rolling back a failed script leaves the
  tracking table unchanged.
- Idempotent SQL (`create table if not exists ...`) is encouraged but not required.
- This tool replaces the previous "rely on Docker entrypoint init" approach for
  production deploys. Docker init still works for fresh local databases.
