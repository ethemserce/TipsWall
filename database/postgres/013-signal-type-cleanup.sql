-- 013 — Collapse signal_type to a single value.
--
-- The unified signals migration (faz öncesi) replaced the legacy
-- hot_rate / winning_rate / earning_rate trio with a single
-- signal_type='custom'. The CHECK constraint kept the old values
-- accepted for transitional safety; nothing has written them since.
-- This migration:
--
--   1. Purges any legacy rows still hanging around (no-op on a fresh
--      cluster; cleans up on long-lived ones).
--   2. Tightens the CHECK to the single value we actually use.
--
-- A future migration can drop signal_type entirely if we never
-- introduce a second category. Until then this constraint costs
-- nothing and documents intent.

delete from analytics.fixture_signals
where signal_type in ('hot_rate', 'winning_rate', 'earning_rate');

alter table analytics.fixture_signals
    drop constraint if exists fixture_signals_signal_type_check;

alter table analytics.fixture_signals
    add constraint fixture_signals_signal_type_check
    check (signal_type = 'custom');
