# Task 3 - PostgreSQL Catalog Reference Schema

Branch: `task/003-postgresql-catalog-reference-schema`

## Scope

This task creates the PostgreSQL DDL for SportMonks v3 reference data under the `catalog` schema.

It does not add EF entities, EF migrations, sync workers, or ETL logic. Those should be separate tasks.

## Tables Added

- `catalog.sports`
- `catalog.types`
- `catalog.states`
- `catalog.continents`
- `catalog.continent_translations`
- `catalog.countries`
- `catalog.country_translations`
- `catalog.regions`
- `catalog.cities`

## Design Decisions

- Provider-owned rows use SportMonks v3 `id` as the PostgreSQL primary key.
- Legacy numeric IDs are not duplicated on these tables. Old-to-new migration mapping remains in `sync.legacy_id_map`.
- `raw_payload_id` is available on provider-owned tables for audit/debug links back to `sync.raw_payloads`.
- `last_synced_at` is stored on provider-owned tables for sync freshness checks.
- Translations use natural composite keys: `(continent_id, locale)` and `(country_id, locale)`.
- Latitude and longitude are stored as numeric values instead of strings.
- `catalog.states.type_id` references `catalog.types` because SportMonks v3 state responses can include the typed metadata relationship.
- `sync.set_updated_at()` centralizes automatic `updated_at` maintenance for PostgreSQL tables.

## Indexes

The script adds lookup indexes for:

- sport/continent/country codes
- type code, model type, and stat group
- state code and type
- country continent lookup
- region country lookup
- city country, region, and name lookup
- country border array lookup with GIN

## Script

SQL file:

```text
database/postgres/002-create-catalog-reference.sql
```

## Acceptance Criteria

Task 3 is accepted when:

1. Catalog/reference PostgreSQL tables are defined.
2. Tables follow the SportMonks v3 ID strategy from Task 1.
3. Script is idempotent enough to rerun in development.
4. SQL can be applied after `001-create-baseline.sql`.
5. Existing application build still succeeds.
