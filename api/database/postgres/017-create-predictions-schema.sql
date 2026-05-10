-- 017-create-predictions-schema.sql
--
-- Track D1: SportMonks Predictions feed (probabilities-by-fixture).
-- Lands once Football Growth + Odds & Predictions add-on are active. The
-- payload shape varies by type_id (1X2 scores, BTTS, total goals, etc.),
-- so the per-prediction values land in jsonb rather than a fixed schema.
-- A single fixture can carry multiple prediction rows — one per type_id
-- — so the SportMonks `id` column is the natural primary key.

create schema if not exists analytics;

create table if not exists analytics.sportmonks_predictions (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    type_id bigint null references catalog.types(id) on delete set null,
    predictions jsonb not null,
    captured_at timestamptz not null default now(),
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_sm_predictions_fixture
    on analytics.sportmonks_predictions (fixture_id);

create index if not exists ix_sm_predictions_fixture_type
    on analytics.sportmonks_predictions (fixture_id, type_id);

drop trigger if exists tr_sm_predictions_set_updated_at on analytics.sportmonks_predictions;
create trigger tr_sm_predictions_set_updated_at
    before update on analytics.sportmonks_predictions
    for each row
    execute function sync.set_updated_at();
