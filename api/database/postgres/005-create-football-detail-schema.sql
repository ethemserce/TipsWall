-- Detail football tables for SportMonks v3.
-- These tables extend football core with fixture events, statistics, lineups, media, news, transfers,
-- weather, and related many-to-many links.

create table if not exists football.fixture_events (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    period_id bigint null references football.fixture_periods(id) on delete set null,
    participant_id bigint null references football.teams(id) on delete set null,
    type_id bigint null references catalog.types(id) on delete set null,
    sub_type_id bigint null references catalog.types(id) on delete set null,
    player_id bigint null references football.players(id) on delete set null,
    related_player_id bigint null references football.players(id) on delete set null,
    coach_id bigint null references football.coaches(id) on delete set null,
    section text null,
    player_name text null,
    related_player_name text null,
    result text null,
    info text null,
    addition text null,
    minute integer null,
    extra_minute integer null,
    injured boolean null,
    on_bench boolean null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixture_events_fixture_minute
    on football.fixture_events (fixture_id, minute, extra_minute);

create index if not exists ix_fixture_events_type
    on football.fixture_events (type_id);

create index if not exists ix_fixture_events_participant
    on football.fixture_events (participant_id);

create index if not exists ix_fixture_events_player
    on football.fixture_events (player_id);

drop trigger if exists tr_fixture_events_set_updated_at on football.fixture_events;
create trigger tr_fixture_events_set_updated_at
    before update on football.fixture_events
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_statistics (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    participant_id bigint null references football.teams(id) on delete set null,
    type_id bigint null references catalog.types(id) on delete set null,
    value numeric(18,4) null,
    location text null,
    raw_data jsonb null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixture_statistics_fixture_type
    on football.fixture_statistics (fixture_id, type_id);

create index if not exists ix_fixture_statistics_participant
    on football.fixture_statistics (participant_id);

create index if not exists ix_fixture_statistics_raw_data_gin
    on football.fixture_statistics using gin (raw_data);

drop trigger if exists tr_fixture_statistics_set_updated_at on football.fixture_statistics;
create trigger tr_fixture_statistics_set_updated_at
    before update on football.fixture_statistics
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_lineups (
    id bigint primary key,
    sport_id bigint null references catalog.sports(id) on delete set null,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    player_id bigint null references football.players(id) on delete set null,
    team_id bigint null references football.teams(id) on delete set null,
    position_id bigint null references catalog.types(id) on delete set null,
    type_id bigint null references catalog.types(id) on delete set null,
    formation_field text null,
    jersey_number integer null,
    formation_position integer null,
    player_name text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixture_lineups_fixture_team
    on football.fixture_lineups (fixture_id, team_id);

create index if not exists ix_fixture_lineups_player
    on football.fixture_lineups (player_id);

create index if not exists ix_fixture_lineups_type
    on football.fixture_lineups (type_id);

drop trigger if exists tr_fixture_lineups_set_updated_at on football.fixture_lineups;
create trigger tr_fixture_lineups_set_updated_at
    before update on football.fixture_lineups
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_lineup_details (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    lineup_id bigint null references football.fixture_lineups(id) on delete cascade,
    player_id bigint null references football.players(id) on delete set null,
    team_id bigint null references football.teams(id) on delete set null,
    type_id bigint null references catalog.types(id) on delete set null,
    data jsonb null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixture_lineup_details_lineup
    on football.fixture_lineup_details (lineup_id);

create index if not exists ix_fixture_lineup_details_fixture_type
    on football.fixture_lineup_details (fixture_id, type_id);

create index if not exists ix_fixture_lineup_details_data_gin
    on football.fixture_lineup_details using gin (data);

drop trigger if exists tr_fixture_lineup_details_set_updated_at on football.fixture_lineup_details;
create trigger tr_fixture_lineup_details_set_updated_at
    before update on football.fixture_lineup_details
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_formations (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    participant_id bigint not null references football.teams(id) on delete cascade,
    formation text null,
    location text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique (fixture_id, participant_id)
);

create index if not exists ix_fixture_formations_participant
    on football.fixture_formations (participant_id);

drop trigger if exists tr_fixture_formations_set_updated_at on football.fixture_formations;
create trigger tr_fixture_formations_set_updated_at
    before update on football.fixture_formations
    for each row
    execute function sync.set_updated_at();

create table if not exists football.sidelined_players (
    id bigint primary key,
    player_id bigint null references football.players(id) on delete set null,
    team_id bigint null references football.teams(id) on delete set null,
    season_id bigint null references competition.seasons(id) on delete set null,
    type_id bigint null references catalog.types(id) on delete set null,
    category text null,
    start_date date null,
    end_date date null,
    games_missed integer null,
    completed boolean not null default false,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_sidelined_players_player
    on football.sidelined_players (player_id);

create index if not exists ix_sidelined_players_team
    on football.sidelined_players (team_id);

create index if not exists ix_sidelined_players_season
    on football.sidelined_players (season_id);

drop trigger if exists tr_sidelined_players_set_updated_at on football.sidelined_players;
create trigger tr_sidelined_players_set_updated_at
    before update on football.sidelined_players
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_sidelined (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    sidelined_id bigint not null references football.sidelined_players(id) on delete cascade,
    participant_id bigint null references football.teams(id) on delete set null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique (fixture_id, sidelined_id)
);

create index if not exists ix_fixture_sidelined_participant
    on football.fixture_sidelined (participant_id);

drop trigger if exists tr_fixture_sidelined_set_updated_at on football.fixture_sidelined;
create trigger tr_fixture_sidelined_set_updated_at
    before update on football.fixture_sidelined
    for each row
    execute function sync.set_updated_at();

create table if not exists football.tv_stations (
    id bigint primary key,
    name text not null,
    url text null,
    image_path text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_tv_stations_name
    on football.tv_stations (name);

drop trigger if exists tr_tv_stations_set_updated_at on football.tv_stations;
create trigger tr_tv_stations_set_updated_at
    before update on football.tv_stations
    for each row
    execute function sync.set_updated_at();

create table if not exists football.tv_station_countries (
    tv_station_id bigint not null references football.tv_stations(id) on delete cascade,
    country_id bigint not null references catalog.countries(id) on delete cascade,
    created_at timestamptz not null default now(),
    primary key (tv_station_id, country_id)
);

create index if not exists ix_tv_station_countries_country
    on football.tv_station_countries (country_id);

create table if not exists football.fixture_tv_stations (
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    tv_station_id bigint not null references football.tv_stations(id) on delete cascade,
    created_at timestamptz not null default now(),
    primary key (fixture_id, tv_station_id)
);

create index if not exists ix_fixture_tv_stations_tv_station
    on football.fixture_tv_stations (tv_station_id);

create table if not exists football.fixture_weather_reports (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    venue_id bigint null references football.venues(id) on delete set null,
    temperature jsonb null,
    feels_like jsonb null,
    wind jsonb null,
    humidity text null,
    pressure integer null,
    clouds text null,
    description text null,
    icon text null,
    type text null,
    metric text null,
    current text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixture_weather_reports_fixture
    on football.fixture_weather_reports (fixture_id);

create index if not exists ix_fixture_weather_reports_venue
    on football.fixture_weather_reports (venue_id);

drop trigger if exists tr_fixture_weather_reports_set_updated_at on football.fixture_weather_reports;
create trigger tr_fixture_weather_reports_set_updated_at
    before update on football.fixture_weather_reports
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_trends (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    participant_id bigint null references football.teams(id) on delete set null,
    type_id bigint null references catalog.types(id) on delete set null,
    period_id bigint null references football.fixture_periods(id) on delete set null,
    value numeric(18,4) null,
    minute integer null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixture_trends_fixture_minute
    on football.fixture_trends (fixture_id, minute);

create index if not exists ix_fixture_trends_type
    on football.fixture_trends (type_id);

drop trigger if exists tr_fixture_trends_set_updated_at on football.fixture_trends;
create trigger tr_fixture_trends_set_updated_at
    before update on football.fixture_trends
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_commentaries (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    comment text not null,
    minute integer null,
    extra_minute integer null,
    is_goal boolean not null default false,
    is_important boolean not null default false,
    sort_order integer null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixture_commentaries_fixture_order
    on football.fixture_commentaries (fixture_id, sort_order);

drop trigger if exists tr_fixture_commentaries_set_updated_at on football.fixture_commentaries;
create trigger tr_fixture_commentaries_set_updated_at
    before update on football.fixture_commentaries
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_comments (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    text text not null,
    minute integer null,
    extra_minute integer null,
    is_goal boolean null,
    is_important boolean null,
    sort_order integer null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixture_comments_fixture_order
    on football.fixture_comments (fixture_id, sort_order);

drop trigger if exists tr_fixture_comments_set_updated_at on football.fixture_comments;
create trigger tr_fixture_comments_set_updated_at
    before update on football.fixture_comments
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_highlights (
    id bigint primary key,
    fixture_id bigint null references football.fixtures(id) on delete cascade,
    location text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixture_highlights_fixture
    on football.fixture_highlights (fixture_id);

drop trigger if exists tr_fixture_highlights_set_updated_at on football.fixture_highlights;
create trigger tr_fixture_highlights_set_updated_at
    before update on football.fixture_highlights
    for each row
    execute function sync.set_updated_at();

create table if not exists football.news (
    id bigint primary key,
    fixture_id bigint null references football.fixtures(id) on delete cascade,
    league_id bigint null references competition.leagues(id) on delete set null,
    title text not null,
    type text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_news_fixture
    on football.news (fixture_id);

create index if not exists ix_news_league
    on football.news (league_id);

drop trigger if exists tr_news_set_updated_at on football.news;
create trigger tr_news_set_updated_at
    before update on football.news
    for each row
    execute function sync.set_updated_at();

create table if not exists football.news_lines (
    id bigint primary key,
    news_id bigint not null references football.news(id) on delete cascade,
    text text not null,
    type text null,
    sort_order integer null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_news_lines_news_order
    on football.news_lines (news_id, sort_order);

drop trigger if exists tr_news_lines_set_updated_at on football.news_lines;
create trigger tr_news_lines_set_updated_at
    before update on football.news_lines
    for each row
    execute function sync.set_updated_at();

create table if not exists football.transfers (
    id bigint primary key,
    sport_id bigint null references catalog.sports(id) on delete set null,
    player_id bigint null references football.players(id) on delete set null,
    type_id bigint null references catalog.types(id) on delete set null,
    from_team_id bigint null references football.teams(id) on delete set null,
    to_team_id bigint null references football.teams(id) on delete set null,
    position_id bigint null references catalog.types(id) on delete set null,
    detailed_position_id bigint null references catalog.types(id) on delete set null,
    transfer_date date null,
    career_ended boolean not null default false,
    completed boolean not null default false,
    amount numeric(18,2) null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_transfers_player
    on football.transfers (player_id);

create index if not exists ix_transfers_from_team
    on football.transfers (from_team_id);

create index if not exists ix_transfers_to_team
    on football.transfers (to_team_id);

create index if not exists ix_transfers_date
    on football.transfers (transfer_date);

drop trigger if exists tr_transfers_set_updated_at on football.transfers;
create trigger tr_transfers_set_updated_at
    before update on football.transfers
    for each row
    execute function sync.set_updated_at();

do $$
begin
    if not exists (
        select 1 from pg_constraint
        where conname = 'fk_team_squads_transfer'
          and conrelid = 'football.team_squads'::regclass
    ) then
        alter table football.team_squads
            add constraint fk_team_squads_transfer
            foreign key (transfer_id) references football.transfers(id) on delete set null;
    end if;
end;
$$;
