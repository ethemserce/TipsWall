-- 012 — Hot-path indices for the unified signals reader.
--
-- The /api/v3/signals endpoint dominates analytics traffic. Two profile
-- gaps showed up in the audit:
--
--  1. The `ix_fixture_signals_lookup` index includes signal_type as the
--     second column. Now that we've collapsed the legacy hot/winning/
--     earning trio onto signal_type='custom' the column is effectively
--     constant — the index pages still split on it, wasting heap.
--     A partial index narrows to the rows we actually serve.
--
--  2. The reader's WHERE clause used `f.starting_at::date = @date` in
--     an earlier revision; this migration also pre-builds a partial
--     covering index on (fixture_id, signal_type='custom') so the JOIN
--     from fixture_signals to football.fixtures stays tight when the
--     query is sorted by confidence_score.
--
-- These indices coexist with the originals — Postgres can keep the old
-- ones if some other consumer depends on signal_type≠'custom'. We can
-- DROP the originals in a future migration once the legacy values are
-- physically purged.

-- NB: the migrator wraps each migration in a transaction; CONCURRENTLY
-- can't run inside one, so we use a regular CREATE INDEX. On busy prod
-- tables consider applying these CONCURRENTLY out-of-band before
-- deploying this migration (then this becomes a no-op via IF NOT EXISTS).

create index if not exists ix_fixture_signals_custom_score
    on analytics.fixture_signals (bookmaker_id, market_id, confidence_score desc nulls last, sample_count desc)
    where signal_type = 'custom';

create index if not exists ix_fixture_signals_custom_fixture
    on analytics.fixture_signals (fixture_id, bookmaker_id, market_id)
    where signal_type = 'custom';

-- Football fixtures: range filters on starting_at already use
-- ix_fixtures_starting_at, but a (league_id, state_id, starting_at)
-- composite helps the home page / state-filter combo where users tap
-- "live in league X" and the planner currently has to choose one of
-- the two existing partial indices. Multi-column avoids the choice.
create index if not exists ix_fixtures_league_state_starting_at
    on football.fixtures (league_id, state_id, starting_at);
