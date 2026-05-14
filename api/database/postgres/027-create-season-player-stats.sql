-- analytics.season_player_stats — per-player season totals derived
-- from football.fixture_lineups × football.fixture_events × football
-- .fixtures. Powers /api/v3/players/{id}/season-stats and is refreshed
-- nightly by the analytics tier (RunSeasonPlayerStatsAsync).
--
-- Same shape philosophy as analytics.season_team_stats:
--   - one row per (league, season, team, player, as_of_date)
--   - fixture_scope keeps the door open for future home/away splits
--   - integer counters + minutes_played → keeps the UI cheap
--   - upsert key on (league, season, team, player, as_of_date, scope)
--
-- Numbers are *cumulative for the season* (not delta), so a daily
-- rebuild always overwrites the previous row idempotently. The worker
-- DELETE's today's slice then re-INSERTs from the source tables.

create schema if not exists analytics;

create table if not exists analytics.season_player_stats (
    id                  uuid not null default gen_random_uuid(),
    league_id           bigint not null,
    season_id           bigint not null,
    team_id             bigint not null,
    player_id           bigint not null,
    as_of_date          date   not null,
    fixture_scope       text   not null default 'all',

    matches_played      integer null,
    matches_started     integer null,
    -- Bench appearance with a sub-on event.
    matches_subbed_in   integer null,
    -- Sub-off (came on then taken off, or starter who got subbed).
    matches_subbed_out  integer null,
    minutes_played      integer null,
    goals               integer null,
    -- SportMonks' event type for assist is unstable across leagues;
    -- we count both ASSIST and SECONDARY_ASSIST when both ship.
    assists             integer null,
    yellow_cards        integer null,
    red_cards           integer null,
    -- Own goals / penalty info that the UI doesn't yet surface but is
    -- cheap to carry alongside the basic counts.
    own_goals           integer null,
    penalties_scored    integer null,
    penalties_missed    integer null,

    created_at          timestamptz not null default now(),
    updated_at          timestamptz not null default now(),

    primary key (id),
    unique (league_id, season_id, team_id, player_id, as_of_date, fixture_scope)
);

-- Per-player lookups by the API are always (player_id, as_of_date desc)
-- — the reader needs the newest row across whatever leagues that
-- player appears in.
create index if not exists ix_season_player_stats_player_date
    on analytics.season_player_stats (player_id, as_of_date desc);

-- updated_at touch trigger so manual UPDATEs keep the bookkeeping
-- consistent. The nightly job uses INSERT ... ON CONFLICT so the
-- trigger only matters for ad-hoc tweaks.
drop trigger if exists tr_season_player_stats_set_updated_at on analytics.season_player_stats;
create trigger tr_season_player_stats_set_updated_at
    before update on analytics.season_player_stats
    for each row
    execute function sync.set_updated_at();
