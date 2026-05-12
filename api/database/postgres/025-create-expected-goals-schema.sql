-- 025-create-expected-goals-schema.sql
--
-- xG (Expected Goals) capture for the SportMonks "Pressure Index & xG"
-- bundle (trial until 2026-05-24). The /v3/football/expected/fixtures
-- endpoint returns one row per (fixture × team × type), where `type_id`
-- distinguishes the xG variant (cumulative xG vs xG on target etc.).
--
-- We flatten `data.value` into a numeric column so the analytics layer
-- can join + aggregate without jsonb extraction, but keep the raw blob
-- in case SportMonks adds extra fields to the payload.

create table if not exists football.fixture_expected_goals (
    id              bigint primary key,
    fixture_id      bigint not null references football.fixtures(id) on delete cascade,
    participant_id  bigint null references football.teams(id) on delete set null,
    type_id         bigint null references catalog.types(id) on delete set null,
    location        text null,
    value           numeric(10,4) null,
    raw_data        jsonb null,
    last_synced_at  timestamptz null,
    raw_payload_id  uuid null references sync.raw_payloads(id) on delete set null,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now()
);

create index if not exists ix_fixture_xg_fixture
    on football.fixture_expected_goals (fixture_id);

create index if not exists ix_fixture_xg_fixture_type
    on football.fixture_expected_goals (fixture_id, type_id);

drop trigger if exists tr_fixture_xg_set_updated_at on football.fixture_expected_goals;
create trigger tr_fixture_xg_set_updated_at
    before update on football.fixture_expected_goals
    for each row
    execute function sync.set_updated_at();
