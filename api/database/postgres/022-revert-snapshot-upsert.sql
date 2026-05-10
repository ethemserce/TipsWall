-- 022-revert-snapshot-upsert.sql
--
-- Reverts migration 021. Multi-as_of_date rows turned out to be the right
-- design after all: a row per (business_key, as_of_date) preserves the
-- historical trend of winning_percent / earning_percent for an outcome,
-- which is something the product wants to surface later. The duplicates
-- the user spotted were a query-writing issue (GROUP BY without as_of_date),
-- not a schema bug.
--
-- 021 already deleted older duplicates and we can't bring them back.
-- That's fine: the missing rows would be stale snapshots from before the
-- premium upgrade and have no analytical value.

-- 1) Drop the business-key UNIQUE that 021 added.
alter table analytics.odd_analysis_snapshots
    drop constraint if exists odd_analysis_snapshots_business_key_unique;

-- 2) Restore the as_of_date-keyed UNIQUE from migration 007.
alter table analytics.odd_analysis_snapshots
    add constraint odd_analysis_snapshots_as_of_date_feed_type_bookmaker_id_ma_key
    unique (as_of_date, feed_type, bookmaker_id, market_id, window_code, outcome_key);

-- 3) Recreate the indexes 021 dropped.
create index if not exists ix_odd_analysis_snapshots_lookup
    on analytics.odd_analysis_snapshots (as_of_date, bookmaker_id, market_id, window_code);

create index if not exists ix_odd_analysis_snapshots_hot_rate
    on analytics.odd_analysis_snapshots (as_of_date, market_id, window_code, winning_percent desc, earning_percent desc);
