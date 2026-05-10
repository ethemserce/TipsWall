-- 023-create-value-bets-schema.sql
--
-- Track D1 follow-up: SportMonks Predictions value-bets feed
-- (predictions/value-bets/fixtures/{id} per fixture). The probabilities
-- table (analytics.sportmonks_predictions, migration 017) already covers
-- the win/draw/lose probability spread. Value-bets is the second half of
-- the bundle: each row marks one bookmaker × outcome pair that
-- SportMonks' model rates as +EV against the fair odd, with a Kelly-
-- style stake suggestion.
--
-- Known fields from /predictions/value-bets are promoted to columns so
-- the analytics layer can filter on is_value / fair_odd / stake without
-- jsonb extraction. The raw payload sticks around as well — the bundle
-- evolves and future fields will land in `raw_predictions`.

create table if not exists analytics.sportmonks_value_bets (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    type_id bigint null references catalog.types(id) on delete set null,
    bet text null,
    bookmaker text null,
    fair_odd numeric(10,4) null,
    odd numeric(10,4) null,
    stake numeric(8,4) null,
    is_value boolean null,
    raw_predictions jsonb null,
    captured_at timestamptz not null default now(),
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_sm_value_bets_fixture
    on analytics.sportmonks_value_bets (fixture_id);

create index if not exists ix_sm_value_bets_fixture_value
    on analytics.sportmonks_value_bets (fixture_id, is_value)
    where is_value = true;

create index if not exists ix_sm_value_bets_fixture_type
    on analytics.sportmonks_value_bets (fixture_id, type_id);

drop trigger if exists tr_sm_value_bets_set_updated_at on analytics.sportmonks_value_bets;
create trigger tr_sm_value_bets_set_updated_at
    before update on analytics.sportmonks_value_bets
    for each row
    execute function sync.set_updated_at();
