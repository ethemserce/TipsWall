# PostgreSQL Scripts

Run these scripts in lexical order for a new local database.

```text
001-create-baseline.sql
002-create-catalog-reference.sql
003-create-competition-schema.sql
004-create-football-core-schema.sql
005-create-football-detail-schema.sql
006-create-odds-schema.sql
007-create-analytics-schema.sql
008-create-app-schema.sql
```

`docker-compose.postgres.yml` mounts this directory into `/docker-entrypoint-initdb.d`, so PostgreSQL runs the scripts automatically when the database volume is created for the first time.
