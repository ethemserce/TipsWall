-- 016-drop-bookmaker-fixture-mappings.sql
--
-- Track B4 decision (architecture audit follow-up): odds.bookmaker_fixture_mappings
-- has been a half-built scaffold since task-007/task-030 — no writer was ever
-- shipped, no endpoint was ever wired, and the product has no current need for
-- bookmaker deep-links. Drop the table; if the deep-link feature ever lands,
-- a future migration can recreate it alongside the writer.

drop table if exists odds.bookmaker_fixture_mappings;
