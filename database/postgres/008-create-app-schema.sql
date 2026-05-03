-- App-owned tables for PreOdds web and mobile clients.
-- These tables store PreOdds product data. They are not SportMonks provider-owned tables
-- and they do not perform legacy historical data transfer.

create table if not exists app.users (
    id uuid primary key default gen_random_uuid(),
    public_id text not null default encode(gen_random_bytes(8), 'hex'),
    email text null,
    username text null,
    display_name text null,
    first_name text null,
    last_name text null,
    avatar_url text null,
    role text not null default 'user',
    status text not null default 'active',
    password_hash text null,
    password_algorithm text null,
    locale text null,
    timezone text null,
    email_verified_at timestamptz null,
    last_login_at timestamptz null,
    terms_accepted_at timestamptz null,
    metadata jsonb null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_users_role
        check (role in ('user', 'admin', 'moderator', 'system')),
    constraint ck_users_status
        check (status in ('active', 'pending', 'blocked', 'deleted'))
);

create unique index if not exists ux_users_public_id
    on app.users (public_id);

create unique index if not exists ux_users_email_normalized
    on app.users (lower(email))
    where email is not null;

create unique index if not exists ux_users_username_normalized
    on app.users (lower(username))
    where username is not null;

create index if not exists ix_users_status
    on app.users (status);

drop trigger if exists tr_users_set_updated_at on app.users;
create trigger tr_users_set_updated_at
    before update on app.users
    for each row
    execute function sync.set_updated_at();

create table if not exists app.user_auth_identities (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null references app.users(id) on delete cascade,
    provider text not null,
    provider_subject text not null,
    provider_email text null,
    raw_profile jsonb null,
    last_login_at timestamptz null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_user_auth_identities_provider
        check (provider in ('email', 'google', 'apple', 'facebook', 'twitter', 'microsoft'))
);

create unique index if not exists ux_user_auth_identities_provider_subject
    on app.user_auth_identities (provider, provider_subject);

create index if not exists ix_user_auth_identities_user
    on app.user_auth_identities (user_id);

create index if not exists ix_user_auth_identities_raw_profile_gin
    on app.user_auth_identities using gin (raw_profile);

drop trigger if exists tr_user_auth_identities_set_updated_at on app.user_auth_identities;
create trigger tr_user_auth_identities_set_updated_at
    before update on app.user_auth_identities
    for each row
    execute function sync.set_updated_at();

create table if not exists app.user_preferences (
    user_id uuid primary key references app.users(id) on delete cascade,
    odds_format text not null default 'decimal',
    locale text null,
    timezone text null,
    notification_preferences jsonb null,
    favorite_market_ids bigint[] null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_user_preferences_odds_format
        check (odds_format in ('decimal', 'fractional', 'american'))
);

create index if not exists ix_user_preferences_favorite_markets_gin
    on app.user_preferences using gin (favorite_market_ids);

create index if not exists ix_user_preferences_notification_preferences_gin
    on app.user_preferences using gin (notification_preferences);

drop trigger if exists tr_user_preferences_set_updated_at on app.user_preferences;
create trigger tr_user_preferences_set_updated_at
    before update on app.user_preferences
    for each row
    execute function sync.set_updated_at();

create table if not exists app.user_devices (
    id uuid primary key default gen_random_uuid(),
    user_id uuid null references app.users(id) on delete cascade,
    platform text not null,
    device_name text null,
    app_version text null,
    locale text null,
    timezone text null,
    push_provider text null,
    push_token text null,
    last_seen_at timestamptz null,
    revoked_at timestamptz null,
    metadata jsonb null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_user_devices_platform
        check (platform in ('web', 'ios', 'android'))
);

create index if not exists ix_user_devices_user
    on app.user_devices (user_id);

create unique index if not exists ux_user_devices_push_token
    on app.user_devices (push_provider, push_token)
    where push_provider is not null and push_token is not null;

create index if not exists ix_user_devices_last_seen
    on app.user_devices (last_seen_at desc);

drop trigger if exists tr_user_devices_set_updated_at on app.user_devices;
create trigger tr_user_devices_set_updated_at
    before update on app.user_devices
    for each row
    execute function sync.set_updated_at();

create table if not exists app.favorites (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null references app.users(id) on delete cascade,
    favorite_type text not null,
    team_id bigint null references football.teams(id) on delete cascade,
    league_id bigint null references competition.leagues(id) on delete cascade,
    fixture_id bigint null references football.fixtures(id) on delete cascade,
    notes text null,
    sort_order integer null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_favorites_target
        check (
            (favorite_type = 'team' and team_id is not null and league_id is null and fixture_id is null) or
            (favorite_type = 'league' and team_id is null and league_id is not null and fixture_id is null) or
            (favorite_type = 'fixture' and team_id is null and league_id is null and fixture_id is not null)
        )
);

create unique index if not exists ux_favorites_user_team
    on app.favorites (user_id, team_id)
    where favorite_type = 'team';

create unique index if not exists ux_favorites_user_league
    on app.favorites (user_id, league_id)
    where favorite_type = 'league';

create unique index if not exists ux_favorites_user_fixture
    on app.favorites (user_id, fixture_id)
    where favorite_type = 'fixture';

create index if not exists ix_favorites_user_sort
    on app.favorites (user_id, favorite_type, sort_order);

drop trigger if exists tr_favorites_set_updated_at on app.favorites;
create trigger tr_favorites_set_updated_at
    before update on app.favorites
    for each row
    execute function sync.set_updated_at();

create table if not exists app.featured_fixtures (
    id uuid primary key default gen_random_uuid(),
    feature_date date not null,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    source text not null default 'manual',
    analytics_fixture_signal_id uuid null references analytics.fixture_signals(id) on delete set null,
    title text null,
    description text null,
    priority integer not null default 0,
    active boolean not null default true,
    created_by_user_id uuid null references app.users(id) on delete set null,
    metadata jsonb null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_featured_fixtures_source
        check (source in ('manual', 'analytics', 'system'))
);

create unique index if not exists ux_featured_fixtures_date_fixture
    on app.featured_fixtures (feature_date, fixture_id);

create index if not exists ix_featured_fixtures_listing
    on app.featured_fixtures (feature_date, active, priority desc);

create index if not exists ix_featured_fixtures_metadata_gin
    on app.featured_fixtures using gin (metadata);

drop trigger if exists tr_featured_fixtures_set_updated_at on app.featured_fixtures;
create trigger tr_featured_fixtures_set_updated_at
    before update on app.featured_fixtures
    for each row
    execute function sync.set_updated_at();

create table if not exists app.tips (
    id uuid primary key default gen_random_uuid(),
    user_id uuid null references app.users(id) on delete set null,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    odds_current_id bigint null references odds.prematch_odds_current(id) on delete set null,
    feed_type text not null default 'standard',
    bookmaker_id bigint not null references odds.bookmakers(id) on delete restrict,
    market_id bigint not null references odds.markets(id) on delete restrict,
    outcome_key text not null,
    label text not null,
    odd_value numeric(12,4) null,
    odd_value_text text null,
    total text null,
    handicap text null,
    participants text null,
    result_status text not null default 'pending',
    visibility text not null default 'public',
    note text null,
    published_at timestamptz null,
    settled_at timestamptz null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_tips_feed_type
        check (feed_type in ('standard', 'premium')),
    constraint ck_tips_result_status
        check (result_status in ('pending', 'won', 'lost', 'void', 'unknown')),
    constraint ck_tips_visibility
        check (visibility in ('public', 'private', 'unlisted'))
);

create index if not exists ix_tips_listing
    on app.tips (visibility, published_at desc, created_at desc);

create index if not exists ix_tips_user
    on app.tips (user_id, created_at desc);

create index if not exists ix_tips_fixture
    on app.tips (fixture_id);

create index if not exists ix_tips_market_bookmaker
    on app.tips (market_id, bookmaker_id);

drop trigger if exists tr_tips_set_updated_at on app.tips;
create trigger tr_tips_set_updated_at
    before update on app.tips
    for each row
    execute function sync.set_updated_at();

create table if not exists app.coupons (
    id uuid primary key default gen_random_uuid(),
    user_id uuid null references app.users(id) on delete set null,
    public_code text not null default encode(gen_random_bytes(8), 'hex'),
    title text null,
    total_rate numeric(12,4) null,
    total_rate_text text null,
    status text not null default 'draft',
    visibility text not null default 'public',
    starts_at timestamptz null,
    ends_at timestamptz null,
    published_at timestamptz null,
    settled_at timestamptz null,
    metadata jsonb null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_coupons_status
        check (status in ('draft', 'published', 'pending', 'won', 'lost', 'void')),
    constraint ck_coupons_visibility
        check (visibility in ('public', 'private', 'unlisted'))
);

create unique index if not exists ux_coupons_public_code
    on app.coupons (public_code);

create index if not exists ix_coupons_listing
    on app.coupons (visibility, published_at desc, created_at desc);

create index if not exists ix_coupons_user
    on app.coupons (user_id, created_at desc);

create index if not exists ix_coupons_metadata_gin
    on app.coupons using gin (metadata);

drop trigger if exists tr_coupons_set_updated_at on app.coupons;
create trigger tr_coupons_set_updated_at
    before update on app.coupons
    for each row
    execute function sync.set_updated_at();

create table if not exists app.coupon_items (
    id uuid primary key default gen_random_uuid(),
    coupon_id uuid not null references app.coupons(id) on delete cascade,
    fixture_id bigint not null references football.fixtures(id) on delete cascade,
    odds_current_id bigint null references odds.prematch_odds_current(id) on delete set null,
    feed_type text not null default 'standard',
    bookmaker_id bigint not null references odds.bookmakers(id) on delete restrict,
    market_id bigint not null references odds.markets(id) on delete restrict,
    outcome_key text not null,
    label text not null,
    odd_value numeric(12,4) null,
    odd_value_text text null,
    total text null,
    handicap text null,
    participants text null,
    result_status text not null default 'pending',
    sort_order integer not null default 0,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_coupon_items_feed_type
        check (feed_type in ('standard', 'premium')),
    constraint ck_coupon_items_result_status
        check (result_status in ('pending', 'won', 'lost', 'void', 'unknown'))
);

create unique index if not exists ux_coupon_items_selection
    on app.coupon_items (coupon_id, fixture_id, feed_type, bookmaker_id, market_id, outcome_key);

create index if not exists ix_coupon_items_coupon_sort
    on app.coupon_items (coupon_id, sort_order);

create index if not exists ix_coupon_items_fixture
    on app.coupon_items (fixture_id);

drop trigger if exists tr_coupon_items_set_updated_at on app.coupon_items;
create trigger tr_coupon_items_set_updated_at
    before update on app.coupon_items
    for each row
    execute function sync.set_updated_at();

create table if not exists app.notifications (
    id uuid primary key default gen_random_uuid(),
    user_id uuid null references app.users(id) on delete cascade,
    notification_type text not null,
    title text not null,
    body text not null,
    priority integer not null default 0,
    status text not null default 'pending',
    data jsonb null,
    scheduled_at timestamptz null,
    sent_at timestamptz null,
    read_at timestamptz null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_notifications_status
        check (status in ('pending', 'sent', 'read', 'failed', 'cancelled'))
);

create index if not exists ix_notifications_user_status
    on app.notifications (user_id, status, created_at desc);

create index if not exists ix_notifications_schedule
    on app.notifications (status, scheduled_at);

create index if not exists ix_notifications_data_gin
    on app.notifications using gin (data);

drop trigger if exists tr_notifications_set_updated_at on app.notifications;
create trigger tr_notifications_set_updated_at
    before update on app.notifications
    for each row
    execute function sync.set_updated_at();

create table if not exists app.notification_deliveries (
    id uuid primary key default gen_random_uuid(),
    notification_id uuid not null references app.notifications(id) on delete cascade,
    user_device_id uuid null references app.user_devices(id) on delete set null,
    channel text not null,
    provider_message_id text null,
    status text not null default 'pending',
    error text null,
    attempted_at timestamptz null,
    delivered_at timestamptz null,
    opened_at timestamptz null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_notification_deliveries_channel
        check (channel in ('in_app', 'push', 'email')),
    constraint ck_notification_deliveries_status
        check (status in ('pending', 'sent', 'delivered', 'opened', 'failed', 'cancelled'))
);

create index if not exists ix_notification_deliveries_notification
    on app.notification_deliveries (notification_id);

create index if not exists ix_notification_deliveries_device
    on app.notification_deliveries (user_device_id);

create index if not exists ix_notification_deliveries_status
    on app.notification_deliveries (channel, status, attempted_at desc);

drop trigger if exists tr_notification_deliveries_set_updated_at on app.notification_deliveries;
create trigger tr_notification_deliveries_set_updated_at
    before update on app.notification_deliveries
    for each row
    execute function sync.set_updated_at();

create table if not exists app.contact_messages (
    id uuid primary key default gen_random_uuid(),
    user_id uuid null references app.users(id) on delete set null,
    name text not null,
    email text not null,
    subject text null,
    message text not null,
    locale text null,
    status text not null default 'new',
    ip_address inet null,
    user_agent text null,
    handled_by_user_id uuid null references app.users(id) on delete set null,
    handled_at timestamptz null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ck_contact_messages_status
        check (status in ('new', 'read', 'replied', 'spam', 'archived'))
);

create index if not exists ix_contact_messages_status_created
    on app.contact_messages (status, created_at desc);

create index if not exists ix_contact_messages_email
    on app.contact_messages (lower(email));

drop trigger if exists tr_contact_messages_set_updated_at on app.contact_messages;
create trigger tr_contact_messages_set_updated_at
    before update on app.contact_messages
    for each row
    execute function sync.set_updated_at();
