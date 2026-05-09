-- Provider-owned catalog/reference tables for SportMonks v3.
-- These tables use SportMonks v3 IDs directly as primary keys.

create or replace function sync.set_updated_at()
returns trigger
language plpgsql
as $$
begin
    new.updated_at = now();
    return new;
end;
$$;

create table if not exists catalog.sports (
    id bigint primary key,
    name text not null,
    code text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create unique index if not exists ux_sports_code
    on catalog.sports (code)
    where code is not null;

drop trigger if exists tr_sports_set_updated_at on catalog.sports;
create trigger tr_sports_set_updated_at
    before update on catalog.sports
    for each row
    execute function sync.set_updated_at();

create table if not exists catalog.types (
    id bigint primary key,
    name text not null,
    code text null,
    developer_name text null,
    model_type text null,
    stat_group text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_types_code
    on catalog.types (code);

create index if not exists ix_types_model_type
    on catalog.types (model_type);

create index if not exists ix_types_stat_group
    on catalog.types (stat_group);

drop trigger if exists tr_types_set_updated_at on catalog.types;
create trigger tr_types_set_updated_at
    before update on catalog.types
    for each row
    execute function sync.set_updated_at();

create table if not exists catalog.states (
    id bigint primary key,
    type_id bigint null references catalog.types(id) on delete set null,
    state_code text null,
    name text not null,
    short_name text null,
    developer_name text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create unique index if not exists ux_states_state_code
    on catalog.states (state_code)
    where state_code is not null;

create index if not exists ix_states_type
    on catalog.states (type_id);

drop trigger if exists tr_states_set_updated_at on catalog.states;
create trigger tr_states_set_updated_at
    before update on catalog.states
    for each row
    execute function sync.set_updated_at();

create table if not exists catalog.continents (
    id bigint primary key,
    name text not null,
    code text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create unique index if not exists ux_continents_code
    on catalog.continents (code)
    where code is not null;

drop trigger if exists tr_continents_set_updated_at on catalog.continents;
create trigger tr_continents_set_updated_at
    before update on catalog.continents
    for each row
    execute function sync.set_updated_at();

create table if not exists catalog.continent_translations (
    continent_id bigint not null references catalog.continents(id) on delete cascade,
    locale text not null,
    name text not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    primary key (continent_id, locale)
);

drop trigger if exists tr_continent_translations_set_updated_at on catalog.continent_translations;
create trigger tr_continent_translations_set_updated_at
    before update on catalog.continent_translations
    for each row
    execute function sync.set_updated_at();

create table if not exists catalog.countries (
    id bigint primary key,
    continent_id bigint not null references catalog.continents(id),
    name text not null,
    official_name text null,
    fifa_name text null,
    iso2 text null,
    iso3 text null,
    latitude numeric(10,7) null,
    longitude numeric(10,7) null,
    image_path text null,
    borders text[] null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_countries_continent
    on catalog.countries (continent_id);

create unique index if not exists ux_countries_iso2
    on catalog.countries (iso2)
    where iso2 is not null;

create unique index if not exists ux_countries_iso3
    on catalog.countries (iso3)
    where iso3 is not null;

create index if not exists ix_countries_borders_gin
    on catalog.countries using gin (borders);

drop trigger if exists tr_countries_set_updated_at on catalog.countries;
create trigger tr_countries_set_updated_at
    before update on catalog.countries
    for each row
    execute function sync.set_updated_at();

create table if not exists catalog.country_translations (
    country_id bigint not null references catalog.countries(id) on delete cascade,
    locale text not null,
    name text not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    primary key (country_id, locale)
);

drop trigger if exists tr_country_translations_set_updated_at on catalog.country_translations;
create trigger tr_country_translations_set_updated_at
    before update on catalog.country_translations
    for each row
    execute function sync.set_updated_at();

create table if not exists catalog.regions (
    id bigint primary key,
    country_id bigint not null references catalog.countries(id) on delete cascade,
    name text not null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_regions_country
    on catalog.regions (country_id);

drop trigger if exists tr_regions_set_updated_at on catalog.regions;
create trigger tr_regions_set_updated_at
    before update on catalog.regions
    for each row
    execute function sync.set_updated_at();

create table if not exists catalog.cities (
    id bigint primary key,
    country_id bigint null references catalog.countries(id) on delete set null,
    region_id bigint null references catalog.regions(id) on delete set null,
    name text not null,
    latitude numeric(10,7) null,
    longitude numeric(10,7) null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_cities_country
    on catalog.cities (country_id);

create index if not exists ix_cities_region
    on catalog.cities (region_id);

create index if not exists ix_cities_name
    on catalog.cities (name);

drop trigger if exists tr_cities_set_updated_at on catalog.cities;
create trigger tr_cities_set_updated_at
    before update on catalog.cities
    for each row
    execute function sync.set_updated_at();
