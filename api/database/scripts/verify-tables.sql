-- Smoke test: verify all expected schemas and key tables exist.
-- Run with: psql -h localhost -U preodds -d preodds -f database/postgres/verify-tables.sql

\set ON_ERROR_STOP on

with expected(schema_name, table_name) as (values
    ('sync',        'sync_jobs'),
    ('sync',        'sync_cursors'),
    ('sync',        'raw_payloads'),
    ('sync',        'api_requests'),
    ('sync',        'schema_migrations'),
    ('catalog',     'sports'),
    ('catalog',     'states'),
    ('catalog',     'types'),
    ('catalog',     'continents'),
    ('catalog',     'countries'),
    ('catalog',     'regions'),
    ('catalog',     'cities'),
    ('competition', 'leagues'),
    ('competition', 'seasons'),
    ('competition', 'stages'),
    ('competition', 'rounds'),
    ('competition', 'standings'),
    ('competition', 'top_scorers'),
    ('football',    'venues'),
    ('football',    'teams'),
    ('football',    'players'),
    ('football',    'coaches'),
    ('football',    'fixtures'),
    ('football',    'fixture_participants'),
    ('football',    'fixture_scores'),
    ('football',    'fixture_events'),
    ('football',    'fixture_statistics'),
    ('football',    'fixture_lineups'),
    ('football',    'news'),
    ('football',    'news_lines'),
    ('football',    'transfers'),
    ('football',    'tv_stations'),
    ('odds',        'bookmakers'),
    ('odds',        'markets'),
    ('odds',        'prematch_odds_current'),
    ('odds',        'prematch_odds_history'),
    ('odds',        'inplay_odds_current'),
    ('odds',        'inplay_odds_history'),
    ('analytics',   'analysis_windows'),
    ('analytics',   'odd_analysis_snapshots'),
    ('analytics',   'fixture_signals'),
    ('analytics',   'season_stats'),
    ('analytics',   'season_team_stats'),
    ('app',         'users'),
    ('app',         'user_auth_identities'),
    ('app',         'user_preferences'),
    ('app',         'user_devices'),
    ('app',         'favorites'),
    ('app',         'featured_fixtures'),
    ('app',         'tips'),
    ('app',         'coupons'),
    ('app',         'coupon_items'),
    ('app',         'notifications'),
    ('app',         'contact_messages'),
    ('app',         'refresh_tokens')
)
select
    e.schema_name,
    e.table_name,
    case when t.tablename is null then 'MISSING' else 'OK' end as status
from expected e
left join pg_tables t
    on t.schemaname = e.schema_name
   and t.tablename = e.table_name
order by status desc, e.schema_name, e.table_name;

-- Final summary
select
    count(*) filter (where t.tablename is null) as missing_count,
    count(*) filter (where t.tablename is not null) as present_count
from (values
    ('sync',        'sync_jobs'),
    ('sync',        'sync_cursors'),
    ('sync',        'raw_payloads'),
    ('sync',        'api_requests'),
    ('sync',        'schema_migrations'),
    ('catalog',     'sports'),
    ('catalog',     'countries'),
    ('catalog',     'continents'),
    ('competition', 'leagues'),
    ('competition', 'seasons'),
    ('competition', 'standings'),
    ('football',    'fixtures'),
    ('football',    'teams'),
    ('football',    'players'),
    ('football',    'fixture_events'),
    ('football',    'news'),
    ('odds',        'bookmakers'),
    ('odds',        'markets'),
    ('odds',        'prematch_odds_current'),
    ('odds',        'inplay_odds_current'),
    ('analytics',   'analysis_windows'),
    ('analytics',   'season_stats'),
    ('app',         'users'),
    ('app',         'tips'),
    ('app',         'coupons'),
    ('app',         'refresh_tokens')
) as expected(schema_name, table_name)
left join pg_tables t
    on t.schemaname = expected.schema_name
   and t.tablename = expected.table_name;
