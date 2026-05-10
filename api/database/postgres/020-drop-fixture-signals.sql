-- 020-drop-fixture-signals.sql
--
-- Yol A cleanup: analytics.fixture_signals is no longer populated or read.
-- The /v3/signals API joins prematch_odds_current × odd_analysis_snapshots
-- at request time, and PostgresAnalyticsEngine.RunFixtureSignalsAsync is
-- already a no-op. Drop the table along with its only inbound reference
-- (app.featured_fixtures.analytics_fixture_signal_id) — the column was
-- defined in migration 008 but never read or written by code.

alter table app.featured_fixtures
    drop column if exists analytics_fixture_signal_id;

drop table if exists analytics.fixture_signals;
