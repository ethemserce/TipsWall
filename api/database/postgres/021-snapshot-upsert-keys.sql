-- 021-snapshot-upsert-keys.sql
--
-- analytics.odd_analysis_snapshots accumulated multiple rows per
-- (bookmaker, market, outcome, window, feed_type) — one per as_of_date —
-- because the old refresh logic was DELETE-today + INSERT-today and the
-- UNIQUE constraint included as_of_date. Past dates piled up.
--
-- Switch to single-row-per-outcome semantics:
--   * keep only the most recent row per business key
--   * drop the as_of_date-keyed UNIQUE
--   * add a UNIQUE on the business key only (so the engine can ON CONFLICT)
--   * drop indexes that lead with as_of_date — the new access pattern is
--     business-key driven, not date driven
--
-- as_of_date stays on the row as a "last calculated on" marker.

-- 1) Strip stale rows: keep the one with the highest as_of_date per business key.
delete from analytics.odd_analysis_snapshots o
where exists (
    select 1
    from analytics.odd_analysis_snapshots newer
    where newer.feed_type    = o.feed_type
      and newer.bookmaker_id = o.bookmaker_id
      and newer.market_id    = o.market_id
      and newer.window_code  = o.window_code
      and newer.outcome_key  = o.outcome_key
      and newer.as_of_date   > o.as_of_date
);

-- 2) Replace the UNIQUE constraint.
alter table analytics.odd_analysis_snapshots
    drop constraint if exists odd_analysis_snapshots_as_of_date_feed_type_bookmaker_id_ma_key;

alter table analytics.odd_analysis_snapshots
    add constraint odd_analysis_snapshots_business_key_unique
    unique (feed_type, bookmaker_id, market_id, window_code, outcome_key);

-- 3) Drop indexes that lead with as_of_date (no longer a primary filter).
drop index if exists analytics.ix_odd_analysis_snapshots_lookup;
drop index if exists analytics.ix_odd_analysis_snapshots_hot_rate;
