-- Analytics/read-model tables for PreOdds.
-- These tables are calculated by the application from SportMonks fixtures, scores, statistics, and odds.
-- They are not provider-owned source tables and are not a legacy data migration path.

create table if not exists analytics.analysis_windows (
    code text primary key,
    name text not null,
    lookback_days integer null,
    sort_order integer not null,
    active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

insert into analytics.analysis_windows (code, name, lookback_days, sort_order)
values
    ('1m', 'One month', 30, 10),
    ('3m', 'Three months', 90, 20),
    ('6m', 'Six months', 180, 30),
    ('1y', 'One year', 365, 40),
    ('all', 'All time', null, 50)
on conflict (code) do update
set
    name = excluded.name,
    lookback_days = excluded.lookback_days,
    sort_order = excluded.sort_order,
    updated_at = now();

drop trigger if exists tr_analysis_windows_set_updated_at on analytics.analysis_windows;
create trigger tr_analysis_windows_set_updated_at
    before update on analytics.analysis_windows
    for each row
    execute function sync.set_updated_at();

create table if not exists analytics.odd_analysis_snapshots (
    id uuid primary key default gen_random_uuid(),
    as_of_date date not null,
    feed_type text not null default 'standard',
    bookmaker_id bigint not null references odds.bookmakers(id) on delete restrict,
    market_id bigint not null references odds.markets(id) on delete restrict,
    window_code text not null references analytics.analysis_windows(code) on delete restrict,
    outcome_key text not null,
    label text not null,
    original_label text null,
    odd_value numeric(12,4) null,
    odd_value_text text null,
    total text null,
    handicap text null,
    participants text null,
    win_count integer not null default 0,
    lost_count integer not null default 0,
    sample_count integer generated always as (win_count + lost_count) stored,
    winning_percent numeric(9,4) null,
    earning_percent numeric(9,4) null,
    average_odd_value numeric(12,4) null,
    calculated_from timestamptz null,
    calculated_to timestamptz null,
    calculation_version text null,
    metadata jsonb null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_odd_analysis_snapshots_feed_type
        check (feed_type in ('standard', 'premium')),
    constraint ck_odd_analysis_snapshots_counts
        check (win_count >= 0 and lost_count >= 0),
    unique (as_of_date, feed_type, bookmaker_id, market_id, window_code, outcome_key)
);

create index if not exists ix_odd_analysis_snapshots_lookup
    on analytics.odd_analysis_snapshots (as_of_date, bookmaker_id, market_id, window_code);

create index if not exists ix_odd_analysis_snapshots_hot_rate
    on analytics.odd_analysis_snapshots (as_of_date, market_id, window_code, winning_percent desc, earning_percent desc);

create index if not exists ix_odd_analysis_snapshots_outcome
    on analytics.odd_analysis_snapshots (bookmaker_id, market_id, outcome_key);

create index if not exists ix_odd_analysis_snapshots_metadata_gin
    on analytics.odd_analysis_snapshots using gin (metadata);

drop trigger if exists tr_odd_analysis_snapshots_set_updated_at on analytics.odd_analysis_snapshots;
create trigger tr_odd_analysis_snapshots_set_updated_at
    before update on analytics.odd_analysis_snapshots
    for each row
    execute function sync.set_updated_at();

create table if not exists analytics.fixture_signals (
    id uuid primary key default gen_random_uuid(),
    as_of_date date not null,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    odds_current_id bigint null references odds.prematch_odds_current(id) on delete set null,
    feed_type text not null default 'standard',
    signal_type text not null,
    bookmaker_id bigint not null references odds.bookmakers(id) on delete restrict,
    market_id bigint not null references odds.markets(id) on delete restrict,
    window_code text not null references analytics.analysis_windows(code) on delete restrict,
    outcome_key text not null,
    label text not null,
    odd_value numeric(12,4) null,
    odd_value_text text null,
    total text null,
    handicap text null,
    participants text null,
    win_count integer not null default 0,
    lost_count integer not null default 0,
    sample_count integer generated always as (win_count + lost_count) stored,
    winning_percent numeric(9,4) null,
    earning_percent numeric(9,4) null,
    odd_group_percent numeric(9,4) null,
    confidence_score numeric(9,4) null,
    rank_order integer null,
    filters jsonb null,
    metrics jsonb null,
    calculated_at timestamptz not null default now(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_fixture_signals_feed_type
        check (feed_type in ('standard', 'premium')),
    constraint ck_fixture_signals_signal_type
        check (signal_type in ('hot_rate', 'winning_rate', 'earning_rate', 'custom')),
    unique (as_of_date, fixture_id, feed_type, signal_type, bookmaker_id, market_id, window_code, outcome_key)
);

create index if not exists ix_fixture_signals_fixture
    on analytics.fixture_signals (fixture_id);

create index if not exists ix_fixture_signals_lookup
    on analytics.fixture_signals (as_of_date, signal_type, bookmaker_id, market_id, window_code, rank_order);

create index if not exists ix_fixture_signals_score
    on analytics.fixture_signals (signal_type, confidence_score desc, winning_percent desc, earning_percent desc);

create index if not exists ix_fixture_signals_filters_gin
    on analytics.fixture_signals using gin (filters);

create index if not exists ix_fixture_signals_metrics_gin
    on analytics.fixture_signals using gin (metrics);

drop trigger if exists tr_fixture_signals_set_updated_at on analytics.fixture_signals;
create trigger tr_fixture_signals_set_updated_at
    before update on analytics.fixture_signals
    for each row
    execute function sync.set_updated_at();

create table if not exists analytics.hot_rate_results (
    id uuid primary key default gen_random_uuid(),
    as_of_date date not null,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    fixture_signal_id uuid null references analytics.fixture_signals(id) on delete set null,
    odds_current_id bigint null references odds.prematch_odds_current(id) on delete set null,
    feed_type text not null default 'standard',
    bookmaker_id bigint not null references odds.bookmakers(id) on delete restrict,
    market_id bigint not null references odds.markets(id) on delete restrict,
    window_code text not null references analytics.analysis_windows(code) on delete restrict,
    outcome_key text not null,
    label text not null,
    odd_value numeric(12,4) null,
    odd_value_text text null,
    total text null,
    handicap text null,
    participants text null,
    win_count integer not null default 0,
    lost_count integer not null default 0,
    sample_count integer generated always as (win_count + lost_count) stored,
    winning_percent numeric(9,4) null,
    earning_percent numeric(9,4) null,
    odd_group_percent numeric(9,4) null,
    min_winning_percent numeric(9,4) null,
    min_earning_percent numeric(9,4) null,
    min_odd_value numeric(12,4) null,
    match_state integer null,
    rank_order integer not null,
    calculated_at timestamptz not null default now(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_hot_rate_results_feed_type
        check (feed_type in ('standard', 'premium')),
    unique (as_of_date, fixture_id, feed_type, bookmaker_id, market_id, window_code, outcome_key)
);

create index if not exists ix_hot_rate_results_listing
    on analytics.hot_rate_results (as_of_date, bookmaker_id, market_id, window_code, match_state, rank_order);

create index if not exists ix_hot_rate_results_fixture
    on analytics.hot_rate_results (fixture_id);

drop trigger if exists tr_hot_rate_results_set_updated_at on analytics.hot_rate_results;
create trigger tr_hot_rate_results_set_updated_at
    before update on analytics.hot_rate_results
    for each row
    execute function sync.set_updated_at();

create table if not exists analytics.winning_rate_results (
    id uuid primary key default gen_random_uuid(),
    as_of_date date not null,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    fixture_signal_id uuid null references analytics.fixture_signals(id) on delete set null,
    odds_current_id bigint null references odds.prematch_odds_current(id) on delete set null,
    feed_type text not null default 'standard',
    bookmaker_id bigint not null references odds.bookmakers(id) on delete restrict,
    market_id bigint not null references odds.markets(id) on delete restrict,
    window_code text not null references analytics.analysis_windows(code) on delete restrict,
    outcome_key text not null,
    label text not null,
    odd_value numeric(12,4) null,
    odd_value_text text null,
    total text null,
    handicap text null,
    participants text null,
    win_count integer not null default 0,
    lost_count integer not null default 0,
    sample_count integer generated always as (win_count + lost_count) stored,
    winning_percent numeric(9,4) null,
    earning_percent numeric(9,4) null,
    odd_group_percent numeric(9,4) null,
    min_winning_percent numeric(9,4) null,
    min_odd_value numeric(12,4) null,
    match_state integer null,
    rank_order integer not null,
    calculated_at timestamptz not null default now(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_winning_rate_results_feed_type
        check (feed_type in ('standard', 'premium')),
    unique (as_of_date, fixture_id, feed_type, bookmaker_id, market_id, window_code, outcome_key)
);

create index if not exists ix_winning_rate_results_listing
    on analytics.winning_rate_results (as_of_date, bookmaker_id, market_id, window_code, match_state, rank_order);

create index if not exists ix_winning_rate_results_fixture
    on analytics.winning_rate_results (fixture_id);

drop trigger if exists tr_winning_rate_results_set_updated_at on analytics.winning_rate_results;
create trigger tr_winning_rate_results_set_updated_at
    before update on analytics.winning_rate_results
    for each row
    execute function sync.set_updated_at();

create table if not exists analytics.earning_rate_results (
    id uuid primary key default gen_random_uuid(),
    as_of_date date not null,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    fixture_signal_id uuid null references analytics.fixture_signals(id) on delete set null,
    odds_current_id bigint null references odds.prematch_odds_current(id) on delete set null,
    feed_type text not null default 'standard',
    bookmaker_id bigint not null references odds.bookmakers(id) on delete restrict,
    market_id bigint not null references odds.markets(id) on delete restrict,
    window_code text not null references analytics.analysis_windows(code) on delete restrict,
    outcome_key text not null,
    label text not null,
    odd_value numeric(12,4) null,
    odd_value_text text null,
    total text null,
    handicap text null,
    participants text null,
    win_count integer not null default 0,
    lost_count integer not null default 0,
    sample_count integer generated always as (win_count + lost_count) stored,
    winning_percent numeric(9,4) null,
    earning_percent numeric(9,4) null,
    odd_group_percent numeric(9,4) null,
    min_earning_percent numeric(9,4) null,
    min_odd_value numeric(12,4) null,
    match_state integer null,
    rank_order integer not null,
    calculated_at timestamptz not null default now(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_earning_rate_results_feed_type
        check (feed_type in ('standard', 'premium')),
    unique (as_of_date, fixture_id, feed_type, bookmaker_id, market_id, window_code, outcome_key)
);

create index if not exists ix_earning_rate_results_listing
    on analytics.earning_rate_results (as_of_date, bookmaker_id, market_id, window_code, match_state, rank_order);

create index if not exists ix_earning_rate_results_fixture
    on analytics.earning_rate_results (fixture_id);

drop trigger if exists tr_earning_rate_results_set_updated_at on analytics.earning_rate_results;
create trigger tr_earning_rate_results_set_updated_at
    before update on analytics.earning_rate_results
    for each row
    execute function sync.set_updated_at();

create table if not exists analytics.season_stats (
    id uuid primary key default gen_random_uuid(),
    league_id bigint not null references competition.leagues(id) on delete cascade,
    season_id bigint not null references competition.seasons(id) on delete cascade,
    as_of_date date not null,
    number_of_clubs integer null,
    number_of_matches integer null,
    number_of_matches_played integer null,
    number_of_goals integer null,
    matches_both_teams_scored integer null,
    number_of_yellow_cards integer null,
    number_of_yellow_red_cards integer null,
    number_of_red_cards integer null,
    avg_goals_per_match numeric(9,4) null,
    avg_yellow_cards_per_match numeric(9,4) null,
    avg_yellow_red_cards_per_match numeric(9,4) null,
    avg_red_cards_per_match numeric(9,4) null,
    team_with_most_goals_id bigint null references football.teams(id) on delete set null,
    team_with_most_conceded_goals_id bigint null references football.teams(id) on delete set null,
    team_with_most_goals_per_match_id bigint null references football.teams(id) on delete set null,
    season_top_scorer_id bigint null references football.players(id) on delete set null,
    season_assist_top_scorer_id bigint null references football.players(id) on delete set null,
    team_most_clean_sheets_id bigint null references football.teams(id) on delete set null,
    goalkeeper_most_clean_sheets_id bigint null references football.players(id) on delete set null,
    goal_scored_every_minutes integer null,
    goals_scored_minute_buckets jsonb null,
    metrics jsonb null,
    calculated_at timestamptz not null default now(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique (league_id, season_id, as_of_date)
);

create index if not exists ix_season_stats_lookup
    on analytics.season_stats (league_id, season_id, as_of_date desc);

create index if not exists ix_season_stats_metrics_gin
    on analytics.season_stats using gin (metrics);

drop trigger if exists tr_season_stats_set_updated_at on analytics.season_stats;
create trigger tr_season_stats_set_updated_at
    before update on analytics.season_stats
    for each row
    execute function sync.set_updated_at();

create table if not exists analytics.season_team_stats (
    id uuid primary key default gen_random_uuid(),
    league_id bigint not null references competition.leagues(id) on delete cascade,
    season_id bigint not null references competition.seasons(id) on delete cascade,
    team_id bigint not null references football.teams(id) on delete cascade,
    as_of_date date not null,
    fixture_scope text not null default 'all',
    matches_played integer null,
    matches_won integer null,
    matches_drawn integer null,
    matches_lost integer null,
    goals_for integer null,
    goals_against integer null,
    goal_difference integer null,
    clean_sheets integer null,
    failed_to_score integer null,
    both_teams_scored integer null,
    yellow_cards integer null,
    red_cards integer null,
    average_goals_for numeric(9,4) null,
    average_goals_against numeric(9,4) null,
    points integer null,
    form text null,
    metrics jsonb null,
    calculated_at timestamptz not null default now(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_season_team_stats_fixture_scope
        check (fixture_scope in ('all', 'home', 'away')),
    unique (league_id, season_id, team_id, as_of_date, fixture_scope)
);

create index if not exists ix_season_team_stats_lookup
    on analytics.season_team_stats (league_id, season_id, as_of_date desc);

create index if not exists ix_season_team_stats_team
    on analytics.season_team_stats (team_id, season_id, fixture_scope);

create index if not exists ix_season_team_stats_metrics_gin
    on analytics.season_team_stats using gin (metrics);

drop trigger if exists tr_season_team_stats_set_updated_at on analytics.season_team_stats;
create trigger tr_season_team_stats_set_updated_at
    before update on analytics.season_team_stats
    for each row
    execute function sync.set_updated_at();
