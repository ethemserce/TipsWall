-- Core football tables for SportMonks v3.
-- Detail-heavy fixture data such as events, statistics, lineups, formations, TV stations, and weather
-- is intentionally left for later schema tasks.

create table if not exists football.venues (
    id bigint primary key,
    country_id bigint null references catalog.countries(id) on delete set null,
    city_id bigint null references catalog.cities(id) on delete set null,
    name text not null,
    address text null,
    zipcode text null,
    latitude numeric(10,7) null,
    longitude numeric(10,7) null,
    capacity integer null,
    image_path text null,
    city_name text null,
    surface text null,
    national_team boolean not null default false,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_venues_country
    on football.venues (country_id);

create index if not exists ix_venues_city
    on football.venues (city_id);

create index if not exists ix_venues_name
    on football.venues (name);

drop trigger if exists tr_venues_set_updated_at on football.venues;
create trigger tr_venues_set_updated_at
    before update on football.venues
    for each row
    execute function sync.set_updated_at();

create table if not exists football.teams (
    id bigint primary key,
    sport_id bigint null references catalog.sports(id) on delete set null,
    country_id bigint null references catalog.countries(id) on delete set null,
    venue_id bigint null references football.venues(id) on delete set null,
    gender text null,
    name text not null,
    short_code text null,
    image_path text null,
    founded integer null,
    type text null,
    placeholder boolean null,
    last_played_at timestamptz null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_teams_sport
    on football.teams (sport_id);

create index if not exists ix_teams_country
    on football.teams (country_id);

create index if not exists ix_teams_venue
    on football.teams (venue_id);

create index if not exists ix_teams_name
    on football.teams (name);

create index if not exists ix_teams_short_code
    on football.teams (short_code)
    where short_code is not null;

drop trigger if exists tr_teams_set_updated_at on football.teams;
create trigger tr_teams_set_updated_at
    before update on football.teams
    for each row
    execute function sync.set_updated_at();

create table if not exists football.players (
    id bigint primary key,
    sport_id bigint null references catalog.sports(id) on delete set null,
    country_id bigint null references catalog.countries(id) on delete set null,
    nationality_id bigint null references catalog.countries(id) on delete set null,
    city_id bigint null references catalog.cities(id) on delete set null,
    position_id bigint null references catalog.types(id) on delete set null,
    detailed_position_id bigint null references catalog.types(id) on delete set null,
    type_id bigint null references catalog.types(id) on delete set null,
    common_name text null,
    first_name text null,
    last_name text null,
    name text not null,
    display_name text null,
    gender text null,
    image_path text null,
    height integer null,
    weight integer null,
    date_of_birth date null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_players_sport
    on football.players (sport_id);

create index if not exists ix_players_country
    on football.players (country_id);

create index if not exists ix_players_nationality
    on football.players (nationality_id);

create index if not exists ix_players_city
    on football.players (city_id);

create index if not exists ix_players_position
    on football.players (position_id);

create index if not exists ix_players_name
    on football.players (name);

drop trigger if exists tr_players_set_updated_at on football.players;
create trigger tr_players_set_updated_at
    before update on football.players
    for each row
    execute function sync.set_updated_at();

create table if not exists football.coaches (
    id bigint primary key,
    player_id bigint null references football.players(id) on delete set null,
    sport_id bigint null references catalog.sports(id) on delete set null,
    country_id bigint null references catalog.countries(id) on delete set null,
    nationality_id bigint null references catalog.countries(id) on delete set null,
    city_id bigint null references catalog.cities(id) on delete set null,
    common_name text null,
    first_name text null,
    last_name text null,
    name text not null,
    display_name text null,
    image_path text null,
    height integer null,
    weight integer null,
    date_of_birth date null,
    gender text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_coaches_player
    on football.coaches (player_id);

create index if not exists ix_coaches_country
    on football.coaches (country_id);

create index if not exists ix_coaches_name
    on football.coaches (name);

drop trigger if exists tr_coaches_set_updated_at on football.coaches;
create trigger tr_coaches_set_updated_at
    before update on football.coaches
    for each row
    execute function sync.set_updated_at();

create table if not exists football.referees (
    id bigint primary key,
    sport_id bigint null references catalog.sports(id) on delete set null,
    country_id bigint null references catalog.countries(id) on delete set null,
    nationality_id bigint null references catalog.countries(id) on delete set null,
    city_id bigint null references catalog.cities(id) on delete set null,
    common_name text null,
    first_name text null,
    last_name text null,
    name text not null,
    display_name text null,
    image_path text null,
    height integer null,
    weight integer null,
    date_of_birth date null,
    gender text null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_referees_country
    on football.referees (country_id);

create index if not exists ix_referees_name
    on football.referees (name);

drop trigger if exists tr_referees_set_updated_at on football.referees;
create trigger tr_referees_set_updated_at
    before update on football.referees
    for each row
    execute function sync.set_updated_at();

create table if not exists football.team_rivals (
    id bigint primary key,
    sport_id bigint null references catalog.sports(id) on delete set null,
    team_id bigint not null references football.teams(id) on delete cascade,
    rival_team_id bigint not null references football.teams(id) on delete cascade,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique (team_id, rival_team_id)
);

create index if not exists ix_team_rivals_rival
    on football.team_rivals (rival_team_id);

drop trigger if exists tr_team_rivals_set_updated_at on football.team_rivals;
create trigger tr_team_rivals_set_updated_at
    before update on football.team_rivals
    for each row
    execute function sync.set_updated_at();

create table if not exists football.team_squads (
    id bigint primary key,
    season_id bigint null references competition.seasons(id) on delete set null,
    transfer_id bigint null,
    player_id bigint not null references football.players(id) on delete cascade,
    team_id bigint not null references football.teams(id) on delete cascade,
    position_id bigint null references catalog.types(id) on delete set null,
    detailed_position_id bigint null references catalog.types(id) on delete set null,
    jersey_number integer null,
    captain boolean null,
    starts_at date null,
    ends_at date null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_team_squads_team
    on football.team_squads (team_id);

create index if not exists ix_team_squads_player
    on football.team_squads (player_id);

create index if not exists ix_team_squads_team_season
    on football.team_squads (team_id, season_id);

drop trigger if exists tr_team_squads_set_updated_at on football.team_squads;
create trigger tr_team_squads_set_updated_at
    before update on football.team_squads
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixtures (
    id bigint primary key,
    sport_id bigint not null references catalog.sports(id),
    league_id bigint not null references competition.leagues(id),
    season_id bigint null references competition.seasons(id) on delete set null,
    stage_id bigint null references competition.stages(id) on delete set null,
    group_id bigint null references competition.groups(id) on delete set null,
    aggregate_id bigint null references competition.aggregates(id) on delete set null,
    round_id bigint null references competition.rounds(id) on delete set null,
    state_id bigint null references catalog.states(id) on delete set null,
    venue_id bigint null references football.venues(id) on delete set null,
    name text null,
    result_info text null,
    leg text null,
    details text null,
    length_minutes integer null,
    placeholder boolean not null default false,
    has_odds boolean not null default false,
    has_premium_odds boolean not null default false,
    starting_at timestamptz null,
    starting_at_timestamp bigint null,
    last_processed_at timestamptz null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixtures_starting_at
    on football.fixtures (starting_at);

create index if not exists ix_fixtures_league_starting_at
    on football.fixtures (league_id, starting_at);

create index if not exists ix_fixtures_season_starting_at
    on football.fixtures (season_id, starting_at);

create index if not exists ix_fixtures_state_starting_at
    on football.fixtures (state_id, starting_at);

create index if not exists ix_fixtures_round
    on football.fixtures (round_id);

drop trigger if exists tr_fixtures_set_updated_at on football.fixtures;
create trigger tr_fixtures_set_updated_at
    before update on football.fixtures
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_participants (
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    team_id bigint not null references football.teams(id) on delete cascade,
    location text not null,
    winner boolean null,
    position integer null,
    raw_meta jsonb null,
    last_synced_at timestamptz null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    primary key (fixture_id, team_id)
);

create index if not exists ix_fixture_participants_team
    on football.fixture_participants (team_id);

create index if not exists ix_fixture_participants_fixture_location
    on football.fixture_participants (fixture_id, location);

drop trigger if exists tr_fixture_participants_set_updated_at on football.fixture_participants;
create trigger tr_fixture_participants_set_updated_at
    before update on football.fixture_participants
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_scores (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    type_id bigint null references catalog.types(id) on delete set null,
    participant_id bigint null references football.teams(id) on delete set null,
    description text null,
    goals integer null,
    participant_location text null,
    raw_score jsonb null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixture_scores_fixture_type
    on football.fixture_scores (fixture_id, type_id);

create index if not exists ix_fixture_scores_participant
    on football.fixture_scores (participant_id);

drop trigger if exists tr_fixture_scores_set_updated_at on football.fixture_scores;
create trigger tr_fixture_scores_set_updated_at
    before update on football.fixture_scores
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_periods (
    id bigint primary key,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    type_id bigint null references catalog.types(id) on delete set null,
    started_at timestamptz null,
    ended_at timestamptz null,
    started_timestamp bigint null,
    ended_timestamp bigint null,
    counts_from integer null,
    actual_period_start integer null,
    ticking boolean not null default false,
    sort_order integer null,
    description text null,
    time_added integer null,
    minutes integer null,
    seconds integer null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_fixture_periods_fixture_type
    on football.fixture_periods (fixture_id, type_id);

create index if not exists ix_fixture_periods_fixture_sort
    on football.fixture_periods (fixture_id, sort_order);

drop trigger if exists tr_fixture_periods_set_updated_at on football.fixture_periods;
create trigger tr_fixture_periods_set_updated_at
    before update on football.fixture_periods
    for each row
    execute function sync.set_updated_at();

create table if not exists football.fixture_referees (
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    referee_id bigint not null references football.referees(id) on delete cascade,
    role text null,
    last_synced_at timestamptz null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    primary key (fixture_id, referee_id)
);

create index if not exists ix_fixture_referees_referee
    on football.fixture_referees (referee_id);

drop trigger if exists tr_fixture_referees_set_updated_at on football.fixture_referees;
create trigger tr_fixture_referees_set_updated_at
    before update on football.fixture_referees
    for each row
    execute function sync.set_updated_at();

do $$
begin
    if not exists (
        select 1 from pg_constraint
        where conname = 'fk_aggregates_winner_participant'
          and conrelid = 'competition.aggregates'::regclass
    ) then
        alter table competition.aggregates
            add constraint fk_aggregates_winner_participant
            foreign key (winner_participant_id) references football.teams(id) on delete set null;
    end if;

    if not exists (
        select 1 from pg_constraint
        where conname = 'fk_standings_participant'
          and conrelid = 'competition.standings'::regclass
    ) then
        alter table competition.standings
            add constraint fk_standings_participant
            foreign key (participant_id) references football.teams(id) on delete set null;
    end if;

    if not exists (
        select 1 from pg_constraint
        where conname = 'fk_standing_forms_fixture'
          and conrelid = 'competition.standing_forms'::regclass
    ) then
        alter table competition.standing_forms
            add constraint fk_standing_forms_fixture
            foreign key (fixture_id) references football.fixtures(id) on delete set null;
    end if;

    if not exists (
        select 1 from pg_constraint
        where conname = 'fk_top_scorers_player'
          and conrelid = 'competition.top_scorers'::regclass
    ) then
        alter table competition.top_scorers
            add constraint fk_top_scorers_player
            foreign key (player_id) references football.players(id) on delete set null;
    end if;

    if not exists (
        select 1 from pg_constraint
        where conname = 'fk_top_scorers_participant'
          and conrelid = 'competition.top_scorers'::regclass
    ) then
        alter table competition.top_scorers
            add constraint fk_top_scorers_participant
            foreign key (participant_id) references football.teams(id) on delete set null;
    end if;
end;
$$;
