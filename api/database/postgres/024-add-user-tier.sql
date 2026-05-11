-- Membership tier on the user record + auxiliary tables for the freemium
-- system (Faz 1 of the auth/membership rollout).
--
-- The mobile app and the public read endpoints (fixtures, signals) all
-- still work without a JWT — guest is the default. Once a user logs in,
-- the JWT carries `tier` so server-side filters can grant the extra detail.
-- Premium upgrades go through Apple/Google IAP; the receipt verification
-- webhook bumps `tier` to 'premium' and stamps `tier_expires_at`.

-- 1) Tier columns on app.users
alter table app.users
    add column if not exists tier text not null default 'free'
        check (tier in ('free', 'premium'));

alter table app.users
    add column if not exists tier_expires_at timestamptz null;

create index if not exists ix_users_tier
    on app.users (tier)
    where tier <> 'free';

-- 2) Subscription receipts — one row per active subscription. The
-- transaction_id is unique per Apple/Google purchase event so re-running
-- a webhook is idempotent. raw_receipt holds the full verified payload
-- in case we need to re-validate later.
create table if not exists app.user_subscriptions (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null references app.users(id) on delete cascade,
    provider text not null check (provider in ('apple', 'google')),
    product_id text not null,
    transaction_id text not null,
    original_transaction_id text not null,
    purchased_at timestamptz not null,
    expires_at timestamptz not null,
    auto_renewing boolean not null default true,
    is_trial boolean not null default false,
    last_verified_at timestamptz not null default now(),
    raw_receipt jsonb not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint ux_user_subscriptions_transaction
        unique (provider, transaction_id)
);

create index if not exists ix_user_subscriptions_user
    on app.user_subscriptions (user_id);

-- "Active subscriptions" lookups use this; we filter by expires_at at
-- query time. Partial index with `where expires_at > now()` would be
-- nicer but now() isn't IMMUTABLE so PG won't accept it in a predicate.
create index if not exists ix_user_subscriptions_active
    on app.user_subscriptions (user_id, expires_at desc);

drop trigger if exists tr_user_subscriptions_set_updated_at
    on app.user_subscriptions;
create trigger tr_user_subscriptions_set_updated_at
    before update on app.user_subscriptions
    for each row
    execute function sync.set_updated_at();

-- 3) Guest daily quota — keyed by device_id (UUID minted on first launch
-- by the mobile client). Guests get 2 picks/day; the mobile client sends
-- device_id on the pick-add endpoint and the server increments here. A
-- composite unique constraint on (device_id, quota_date) keeps the row
-- count bounded.
create table if not exists app.guest_pick_quotas (
    device_id text not null,
    quota_date date not null,
    picks_today int not null default 0,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    primary key (device_id, quota_date)
);

drop trigger if exists tr_guest_pick_quotas_set_updated_at
    on app.guest_pick_quotas;
create trigger tr_guest_pick_quotas_set_updated_at
    before update on app.guest_pick_quotas
    for each row
    execute function sync.set_updated_at();

-- 4) Account deletion audit — Apple/Google require an in-app delete flow.
-- Soft-delete first (flip users.status='deleted'), then a separate
-- nightly job hard-purges anything older than 30 days. Keep the audit row
-- forever so we can answer "did this email ever exist?" without leaking
-- the original PII.
create table if not exists app.account_deletions (
    user_id uuid primary key,
    deleted_at timestamptz not null default now(),
    reason text null,
    purged_at timestamptz null
);
