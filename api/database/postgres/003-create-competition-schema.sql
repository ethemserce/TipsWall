-- Provider-owned competition tables for SportMonks v3 football data.
-- Football-specific foreign keys such as teams, players, and fixtures are added in later schema tasks.

create table if not exists competition.standing_rules (
    id bigint primary key,
    model_type text null,
    model_id bigint null,
    type_id bigint null references catalog.types(id) on delete set null,
    position integer null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_standing_rules_type
    on competition.standing_rules (type_id);

create index if not exists ix_standing_rules_model
    on competition.standing_rules (model_type, model_id);

drop trigger if exists tr_standing_rules_set_updated_at on competition.standing_rules;
create trigger tr_standing_rules_set_updated_at
    before update on competition.standing_rules
    for each row
    execute function sync.set_updated_at();

create table if not exists competition.leagues (
    id bigint primary key,
    sport_id bigint not null references catalog.sports(id),
    country_id bigint null references catalog.countries(id) on delete set null,
    name text not null,
    active boolean not null default false,
    short_code text null,
    image_path text null,
    type text null,
    sub_type text null,
    last_played_at timestamptz null,
    category integer null,
    has_jerseys boolean not null default false,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_leagues_sport
    on competition.leagues (sport_id);

create index if not exists ix_leagues_country
    on competition.leagues (country_id);

create index if not exists ix_leagues_active
    on competition.leagues (active);

create unique index if not exists ux_leagues_short_code
    on competition.leagues (short_code)
    where short_code is not null;

drop trigger if exists tr_leagues_set_updated_at on competition.leagues;
create trigger tr_leagues_set_updated_at
    before update on competition.leagues
    for each row
    execute function sync.set_updated_at();

create table if not exists competition.seasons (
    id bigint primary key,
    sport_id bigint not null references catalog.sports(id),
    league_id bigint not null references competition.leagues(id) on delete cascade,
    tie_breaker_rule_id bigint null references competition.standing_rules(id) on delete set null,
    name text not null,
    finished boolean not null default false,
    pending boolean not null default false,
    is_current boolean not null default false,
    starting_at date null,
    ending_at date null,
    standings_recalculated_at timestamptz null,
    games_in_current_week boolean not null default false,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_seasons_league
    on competition.seasons (league_id);

create index if not exists ix_seasons_sport
    on competition.seasons (sport_id);

create index if not exists ix_seasons_dates
    on competition.seasons (starting_at, ending_at);

create unique index if not exists ux_seasons_current_per_league
    on competition.seasons (league_id)
    where is_current;

drop trigger if exists tr_seasons_set_updated_at on competition.seasons;
create trigger tr_seasons_set_updated_at
    before update on competition.seasons
    for each row
    execute function sync.set_updated_at();

create table if not exists competition.stages (
    id bigint primary key,
    sport_id bigint not null references catalog.sports(id),
    league_id bigint not null references competition.leagues(id) on delete cascade,
    season_id bigint not null references competition.seasons(id) on delete cascade,
    type_id bigint null references catalog.types(id) on delete set null,
    name text not null,
    sort_order integer null,
    finished boolean not null default false,
    is_current boolean not null default false,
    starting_at date null,
    ending_at date null,
    games_in_current_week boolean not null default false,
    tie_breaker_rule_id bigint null references competition.standing_rules(id) on delete set null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_stages_league
    on competition.stages (league_id);

create index if not exists ix_stages_season
    on competition.stages (season_id);

create index if not exists ix_stages_type
    on competition.stages (type_id);

create index if not exists ix_stages_season_sort_order
    on competition.stages (season_id, sort_order);

create unique index if not exists ux_stages_current_per_season
    on competition.stages (season_id)
    where is_current;

drop trigger if exists tr_stages_set_updated_at on competition.stages;
create trigger tr_stages_set_updated_at
    before update on competition.stages
    for each row
    execute function sync.set_updated_at();

create table if not exists competition.groups (
    id bigint primary key,
    sport_id bigint null references catalog.sports(id) on delete set null,
    league_id bigint null references competition.leagues(id) on delete cascade,
    season_id bigint null references competition.seasons(id) on delete cascade,
    stage_id bigint null references competition.stages(id) on delete cascade,
    name text not null,
    starting_at date null,
    ending_at date null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_groups_stage
    on competition.groups (stage_id);

create index if not exists ix_groups_season
    on competition.groups (season_id);

drop trigger if exists tr_groups_set_updated_at on competition.groups;
create trigger tr_groups_set_updated_at
    before update on competition.groups
    for each row
    execute function sync.set_updated_at();

create table if not exists competition.rounds (
    id bigint primary key,
    sport_id bigint not null references catalog.sports(id),
    league_id bigint not null references competition.leagues(id) on delete cascade,
    season_id bigint not null references competition.seasons(id) on delete cascade,
    stage_id bigint not null references competition.stages(id) on delete cascade,
    name text not null,
    finished boolean not null default false,
    is_current boolean not null default false,
    starting_at date null,
    ending_at date null,
    games_in_current_week boolean not null default false,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_rounds_stage
    on competition.rounds (stage_id);

create index if not exists ix_rounds_season
    on competition.rounds (season_id);

create index if not exists ix_rounds_stage_dates
    on competition.rounds (stage_id, starting_at, ending_at);

create unique index if not exists ux_rounds_current_per_stage
    on competition.rounds (stage_id)
    where is_current;

drop trigger if exists tr_rounds_set_updated_at on competition.rounds;
create trigger tr_rounds_set_updated_at
    before update on competition.rounds
    for each row
    execute function sync.set_updated_at();

create table if not exists competition.aggregates (
    id bigint primary key,
    league_id bigint not null references competition.leagues(id) on delete cascade,
    season_id bigint not null references competition.seasons(id) on delete cascade,
    stage_id bigint not null references competition.stages(id) on delete cascade,
    name text null,
    fixture_ids bigint[] null,
    result text null,
    detail text null,
    winner_participant_id bigint null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_aggregates_stage
    on competition.aggregates (stage_id);

create index if not exists ix_aggregates_fixture_ids_gin
    on competition.aggregates using gin (fixture_ids);

drop trigger if exists tr_aggregates_set_updated_at on competition.aggregates;
create trigger tr_aggregates_set_updated_at
    before update on competition.aggregates
    for each row
    execute function sync.set_updated_at();

create table if not exists competition.standings (
    id bigint primary key,
    participant_id bigint null,
    sport_id bigint null references catalog.sports(id) on delete set null,
    league_id bigint null references competition.leagues(id) on delete cascade,
    season_id bigint null references competition.seasons(id) on delete cascade,
    stage_id bigint null references competition.stages(id) on delete cascade,
    group_id bigint null references competition.groups(id) on delete cascade,
    round_id bigint null references competition.rounds(id) on delete cascade,
    standing_rule_id bigint null references competition.standing_rules(id) on delete set null,
    position integer null,
    result text null,
    points integer null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_standings_season
    on competition.standings (season_id);

create index if not exists ix_standings_stage_group_position
    on competition.standings (stage_id, group_id, position);

create index if not exists ix_standings_round_position
    on competition.standings (round_id, position);

create index if not exists ix_standings_participant
    on competition.standings (participant_id);

drop trigger if exists tr_standings_set_updated_at on competition.standings;
create trigger tr_standings_set_updated_at
    before update on competition.standings
    for each row
    execute function sync.set_updated_at();

create table if not exists competition.standing_details (
    id bigint primary key,
    standing_type text null,
    standing_id bigint null references competition.standings(id) on delete cascade,
    type_id bigint null references catalog.types(id) on delete set null,
    value integer null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_standing_details_standing
    on competition.standing_details (standing_id);

create index if not exists ix_standing_details_type
    on competition.standing_details (type_id);

drop trigger if exists tr_standing_details_set_updated_at on competition.standing_details;
create trigger tr_standing_details_set_updated_at
    before update on competition.standing_details
    for each row
    execute function sync.set_updated_at();

create table if not exists competition.standing_forms (
    id bigint primary key,
    standing_type text null,
    standing_id bigint not null references competition.standings(id) on delete cascade,
    fixture_id bigint null,
    form text null,
    sort_order integer not null default 0,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_standing_forms_standing
    on competition.standing_forms (standing_id, sort_order);

create index if not exists ix_standing_forms_fixture
    on competition.standing_forms (fixture_id);

drop trigger if exists tr_standing_forms_set_updated_at on competition.standing_forms;
create trigger tr_standing_forms_set_updated_at
    before update on competition.standing_forms
    for each row
    execute function sync.set_updated_at();

create table if not exists competition.top_scorers (
    id bigint primary key,
    season_id bigint null references competition.seasons(id) on delete cascade,
    stage_id bigint null references competition.stages(id) on delete cascade,
    player_id bigint null,
    type_id bigint null references catalog.types(id) on delete set null,
    position integer not null,
    total integer not null default 0,
    participant_type text null,
    participant_id bigint null,
    last_synced_at timestamptz null,
    raw_payload_id uuid null references sync.raw_payloads(id) on delete set null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create index if not exists ix_top_scorers_season_type_position
    on competition.top_scorers (season_id, type_id, position);

create index if not exists ix_top_scorers_stage_type_position
    on competition.top_scorers (stage_id, type_id, position);

create index if not exists ix_top_scorers_player
    on competition.top_scorers (player_id);

create index if not exists ix_top_scorers_participant
    on competition.top_scorers (participant_id);

drop trigger if exists tr_top_scorers_set_updated_at on competition.top_scorers;
create trigger tr_top_scorers_set_updated_at
    before update on competition.top_scorers
    for each row
    execute function sync.set_updated_at();
