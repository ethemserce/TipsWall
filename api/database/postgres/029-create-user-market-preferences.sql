-- Migration 029: app.user_market_preferences
--
-- Per-user list of markets they want to see across /odds-rates and
-- /signals. Empty preference list means "show everything" (the
-- broader available_in_standard + active default) so a user who never
-- visits the picker gets the same experience as before.
--
-- Free tier is capped at 5 selected markets; premium relaxes the cap
-- to a higher value. The cap is enforced in the API layer rather than
-- a DB CHECK so the limit can move without a migration.

create table if not exists app.user_market_preferences (
    user_id uuid not null references app.users(id) on delete cascade,
    market_id bigint not null references odds.markets(id) on delete cascade,
    created_at timestamptz not null default now(),
    primary key (user_id, market_id)
);

create index if not exists ix_user_market_preferences_user
    on app.user_market_preferences (user_id);
