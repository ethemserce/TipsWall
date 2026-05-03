-- Odds tables for SportMonks v3.
-- Supports standard and premium pre-match feeds, in-play odds, bookmaker/market references,
-- future odds movement history, and bookmaker fixture event mappings.

create table if not exists odds.bookmakers (
    id bigint primary key,
    legacy_id bigint null,
    name text not null,
    logo_path text null,
    available_in_standard boolean not null default true,
    available_in_premium boolean not null default false,
    active boolean not null default true,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create unique index if not exists ux_bookmakers_legacy_id
    on odds.bookmakers (legacy_id)
    where legacy_id is not null;

create index if not exists ix_bookmakers_name
    on odds.bookmakers (name);

drop trigger if exists tr_bookmakers_set_updated_at on odds.bookmakers;
create trigger tr_bookmakers_set_updated_at
    before update on odds.bookmakers
    for each row
    execute function sync.set_updated_at();

create table if not exists odds.markets (
    id bigint primary key,
    legacy_id bigint null,
    name text not null,
    developer_name text null,
    has_winning_calculations boolean null,
    available_in_standard boolean not null default true,
    available_in_premium boolean not null default false,
    active boolean not null default true,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create unique index if not exists ux_markets_legacy_id
    on odds.markets (legacy_id)
    where legacy_id is not null;

create unique index if not exists ux_markets_developer_name
    on odds.markets (developer_name)
    where developer_name is not null;

create index if not exists ix_markets_name
    on odds.markets (name);

drop trigger if exists tr_markets_set_updated_at on odds.markets;
create trigger tr_markets_set_updated_at
    before update on odds.markets
    for each row
    execute function sync.set_updated_at();

create table if not exists odds.bookmaker_fixture_mappings (
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    bookmaker_id bigint not null references odds.bookmakers(id) on delete cascade,
    bookmaker_name text null,
    bookmaker_event_id text not null,
    bookmaker_event_url text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    primary key (fixture_id, bookmaker_id, bookmaker_event_id)
);

create index if not exists ix_bookmaker_fixture_mappings_bookmaker
    on odds.bookmaker_fixture_mappings (bookmaker_id);

drop trigger if exists tr_bookmaker_fixture_mappings_set_updated_at on odds.bookmaker_fixture_mappings;
create trigger tr_bookmaker_fixture_mappings_set_updated_at
    before update on odds.bookmaker_fixture_mappings
    for each row
    execute function sync.set_updated_at();

create table if not exists odds.prematch_odds_current (
    id bigint primary key,
    feed_type text not null default 'standard',
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    market_id bigint not null references odds.markets(id) on delete restrict,
    bookmaker_id bigint not null references odds.bookmakers(id) on delete restrict,
    outcome_key text not null,
    label text not null,
    original_label text null,
    name text null,
    sort_order integer null,
    market_description text null,
    value numeric(12,4) null,
    probability numeric(9,4) null,
    probability_text text null,
    dp3 numeric(12,4) null,
    fractional text null,
    american integer null,
    american_text text null,
    winning boolean null,
    stopped boolean null,
    total text null,
    handicap text null,
    participants text null,
    source_created_at timestamptz null,
    source_updated_at timestamptz null,
    latest_bookmaker_update timestamptz null,
    captured_at timestamptz not null default now(),
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_prematch_odds_current_feed_type
        check (feed_type in ('standard', 'premium')),
    unique (feed_type, fixture_id, bookmaker_id, market_id, outcome_key)
);

create index if not exists ix_prematch_odds_current_fixture_bookmaker_market
    on odds.prematch_odds_current (fixture_id, bookmaker_id, market_id);

create index if not exists ix_prematch_odds_current_fixture_market
    on odds.prematch_odds_current (fixture_id, market_id);

create index if not exists ix_prematch_odds_current_market_bookmaker
    on odds.prematch_odds_current (market_id, bookmaker_id);

create index if not exists ix_prematch_odds_current_latest_bookmaker_update
    on odds.prematch_odds_current (latest_bookmaker_update desc);

create index if not exists ix_prematch_odds_current_outcome
    on odds.prematch_odds_current (outcome_key);

drop trigger if exists tr_prematch_odds_current_set_updated_at on odds.prematch_odds_current;
create trigger tr_prematch_odds_current_set_updated_at
    before update on odds.prematch_odds_current
    for each row
    execute function sync.set_updated_at();

create table if not exists odds.prematch_odds_history (
    id uuid primary key default gen_random_uuid(),
    sportmonks_history_id bigint null,
    sportmonks_odd_id bigint not null,
    feed_type text not null default 'standard',
    fixture_id bigint null references football.fixtures(id) on delete cascade,
    market_id bigint null references odds.markets(id) on delete restrict,
    bookmaker_id bigint null references odds.bookmakers(id) on delete restrict,
    outcome_key text null,
    label text null,
    original_label text null,
    name text null,
    value numeric(12,4) null,
    probability numeric(9,4) null,
    probability_text text null,
    dp3 numeric(12,4) null,
    fractional text null,
    american integer null,
    american_text text null,
    winning boolean null,
    stopped boolean null,
    total text null,
    handicap text null,
    participants text null,
    source_created_at timestamptz null,
    source_updated_at timestamptz null,
    bookmaker_update timestamptz null,
    captured_at timestamptz not null default now(),
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    constraint ck_prematch_odds_history_feed_type
        check (feed_type in ('standard', 'premium'))
);

create unique index if not exists ux_prematch_odds_history_sportmonks_history_id
    on odds.prematch_odds_history (sportmonks_history_id)
    where sportmonks_history_id is not null;

create index if not exists ix_prematch_odds_history_sportmonks_odd
    on odds.prematch_odds_history (sportmonks_odd_id, bookmaker_update desc);

create index if not exists ix_prematch_odds_history_lookup
    on odds.prematch_odds_history (fixture_id, bookmaker_id, market_id, bookmaker_update desc);

create index if not exists ix_prematch_odds_history_captured
    on odds.prematch_odds_history (captured_at desc);

create table if not exists odds.inplay_odds_current (
    id bigint primary key,
    external_id bigint null,
    feed_type text not null default 'standard',
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    market_id bigint not null references odds.markets(id) on delete restrict,
    bookmaker_id bigint not null references odds.bookmakers(id) on delete restrict,
    outcome_key text not null,
    label text not null,
    name text null,
    sort_order integer null,
    market_description text null,
    value numeric(12,4) null,
    probability numeric(9,4) null,
    probability_text text null,
    dp3 numeric(12,4) null,
    fractional text null,
    american integer null,
    american_text text null,
    winning boolean null,
    suspended boolean null,
    stopped boolean null,
    total text null,
    handicap text null,
    participants text null,
    latest_bookmaker_update timestamptz null,
    captured_at timestamptz not null default now(),
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_inplay_odds_current_feed_type
        check (feed_type in ('standard', 'premium')),
    unique (feed_type, fixture_id, bookmaker_id, market_id, outcome_key)
);

create index if not exists ix_inplay_odds_current_fixture_bookmaker_market
    on odds.inplay_odds_current (fixture_id, bookmaker_id, market_id);

create index if not exists ix_inplay_odds_current_fixture_market
    on odds.inplay_odds_current (fixture_id, market_id);

create index if not exists ix_inplay_odds_current_market_bookmaker
    on odds.inplay_odds_current (market_id, bookmaker_id);

create index if not exists ix_inplay_odds_current_latest_bookmaker_update
    on odds.inplay_odds_current (latest_bookmaker_update desc);

create index if not exists ix_inplay_odds_current_outcome
    on odds.inplay_odds_current (outcome_key);

drop trigger if exists tr_inplay_odds_current_set_updated_at on odds.inplay_odds_current;
create trigger tr_inplay_odds_current_set_updated_at
    before update on odds.inplay_odds_current
    for each row
    execute function sync.set_updated_at();

create table if not exists odds.inplay_odds_history (
    id uuid primary key default gen_random_uuid(),
    sportmonks_odd_id bigint not null,
    external_id bigint null,
    feed_type text not null default 'standard',
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    market_id bigint not null references odds.markets(id) on delete restrict,
    bookmaker_id bigint not null references odds.bookmakers(id) on delete restrict,
    outcome_key text not null,
    label text not null,
    name text null,
    value numeric(12,4) null,
    probability numeric(9,4) null,
    probability_text text null,
    dp3 numeric(12,4) null,
    fractional text null,
    american integer null,
    american_text text null,
    winning boolean null,
    suspended boolean null,
    stopped boolean null,
    total text null,
    handicap text null,
    participants text null,
    bookmaker_update timestamptz null,
    captured_at timestamptz not null default now(),
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    constraint ck_inplay_odds_history_feed_type
        check (feed_type in ('standard', 'premium'))
);

create index if not exists ix_inplay_odds_history_sportmonks_odd
    on odds.inplay_odds_history (sportmonks_odd_id, bookmaker_update desc);

create index if not exists ix_inplay_odds_history_lookup
    on odds.inplay_odds_history (fixture_id, bookmaker_id, market_id, captured_at desc);

create index if not exists ix_inplay_odds_history_captured
    on odds.inplay_odds_history (captured_at desc);
