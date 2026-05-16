-- 030 — Season-aware analysis windows.
--
-- The existing analytics.analysis_windows table only describes time-based
-- lookback (lookback_days). For "Bu Sezon" / "Son 2 Sezon" filters we
-- also need windows tied to competition.seasons rows — each league has
-- its own calendar so a flat date filter mis-aligns half the leagues
-- (PL Aug-May vs. MLS Mar-Nov vs. BRA Apr-Dec).
--
-- This migration extends the table with a `kind` discriminator + a
-- `season_count` payload for "last N seasons" windows, and inserts the
-- two new windows. Existing rows ('1m', '3m', '6m', '1y', 'all') get
-- `kind = 'time'` automatically via the column default, so nothing
-- breaks for callers reading them. Rollback safety: dropping the new
-- column + the two new rows reverts behaviour cleanly.

alter table analytics.analysis_windows
    add column if not exists kind text not null default 'time'
        check (kind in ('time', 'season_current', 'season_n')),
    add column if not exists season_count integer null
        check (season_count is null or season_count > 0);

insert into analytics.analysis_windows
    (code, name, lookback_days, kind, season_count, sort_order)
values
    ('season_current', 'This Season', null, 'season_current', null, 5),
    ('season_2y',     'Last 2 Seasons', null, 'season_n',     2,    6)
on conflict (code) do update
set
    name = excluded.name,
    lookback_days = excluded.lookback_days,
    kind = excluded.kind,
    season_count = excluded.season_count,
    sort_order = excluded.sort_order,
    updated_at = now();
