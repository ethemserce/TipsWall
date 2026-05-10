-- 015-drop-dead-tables.sql
--
-- Architecture audit 2026-05-10 identified nine tables with no writers — schema
-- artefacts that survived earlier task plans without follow-through. Drop them
-- and the only live FK that points at the dropped set
-- (football.fixtures.aggregate_id → competition.aggregates).
--
-- The companion code changes in the same commit:
--   * SportMonksFixtureCoreWriter no longer inserts aggregate_id.
--   * PostgresAnalyticsEngine.RunRateResultsAsync (and its caller in
--     FootballWorkerService) is removed — it only DELETEd from the three
--     analytics rate tables dropped here.
--   * Stage.Aggregates parsing is removed.
--
-- odds.bookmaker_fixture_mappings is intentionally kept: Track B4 still needs
-- a decision (complete the writer/endpoint vs. drop).

alter table football.fixtures drop column if exists aggregate_id;

drop table if exists analytics.hot_rate_results;
drop table if exists analytics.winning_rate_results;
drop table if exists analytics.earning_rate_results;
drop table if exists football.fixture_comments;
drop table if exists football.fixture_highlights;
drop table if exists competition.aggregates;
drop table if exists catalog.continent_translations;
drop table if exists catalog.country_translations;
