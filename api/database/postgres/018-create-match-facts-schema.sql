-- 018-create-match-facts-schema.sql
--
-- Track D3: SportMonks Match Facts (BETA) — narrative-style stats per
-- fixture. Examples: "team has won 5 in a row", "0-0 in last 3 head-to-
-- head". The `data` payload shape varies per type_id and category, so it
-- lands in jsonb. Each fixture returns multiple rows; SportMonks `id` is
-- the natural primary key.
--
-- Requires the Match Facts add-on on the SportMonks side. Until that's
-- active, /v3/football/match-facts/{id} returns 404 — the worker logs
-- the warning and moves on.

create table if not exists football.fixture_match_facts (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    sport_id bigint null references catalog.sports(id) on delete set null,
    type_id bigint null references catalog.types(id) on delete set null,
    participant text null,
    basis text null,
    data jsonb null,
    natural_language text null,
    category text null,
    scope text null,
    captured_at timestamptz not null default now(),
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixture_match_facts_fixture
    on football.fixture_match_facts (fixture_id);

create index if not exists ix_fixture_match_facts_category
    on football.fixture_match_facts (category);

drop trigger if exists tr_fixture_match_facts_set_updated_at on football.fixture_match_facts;
create trigger tr_fixture_match_facts_set_updated_at
    before update on football.fixture_match_facts
    for each row
    execute function sync.set_updated_at();
