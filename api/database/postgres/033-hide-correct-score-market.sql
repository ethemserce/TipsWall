-- Migration 033: hide FT Correct Score (market_id 57) from the app.
--
-- Ethem flagged it on 2026-05-18 — the variance is too high and the
-- "correct score" framing reads betting-adjacent in a way that
-- doesn't fit TipsWall's analysis posture. Toggling
-- available_in_standard=false on the row keeps the historical
-- data intact (odds.prematch_odds_current rows survive, analytics
-- still references the row by id) but every display query that
-- joins odds.markets ... on m.available_in_standard = true now
-- skips it:
--
--   * fixture detail OddsRatesCard (PostgresFixtureReader.Extras)
--   * analytics snapshot rebuild (PostgresAnalyticsEngine)
--   * /signals analysis listing (PostgresAnalyticsReader — filter
--     added in the same commit)
--
-- Half-time / 1st-half / 2nd-half correct score variants (30 / 33 /
-- 38) stay visible; those are tighter sub-markets and still useful.
-- Re-enable later with `update odds.markets set available_in_standard
-- = true where id = 57;` if the product position changes.

update odds.markets
set available_in_standard = false
where id = 57;
