-- 019-add-snapshot-join-index.sql
--
-- Yol A: the /v3/signals API switches from reading the precomputed
-- analytics.fixture_signals table to a runtime JOIN against
-- analytics.odd_analysis_snapshots. The composite index below covers the
-- JOIN keys plus the as_of_date filter so the lookup stays in O(log n)
-- territory across the live odds × today's snapshot window.

create index if not exists ix_odd_analysis_snapshots_join
    on analytics.odd_analysis_snapshots (
        bookmaker_id, market_id, outcome_key, feed_type, as_of_date
    );
