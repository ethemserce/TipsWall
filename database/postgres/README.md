# PostgreSQL Scripts

Run these scripts in lexical order for a new local database.

```text
001-create-baseline.sql
002-create-catalog-reference.sql
```

`docker-compose.postgres.yml` mounts this directory into `/docker-entrypoint-initdb.d`, so PostgreSQL runs the scripts automatically when the database volume is created for the first time.
