create extension if not exists pgcrypto;

create schema if not exists catalog;
create schema if not exists competition;
create schema if not exists football;
create schema if not exists odds;
create schema if not exists analytics;
create schema if not exists app;
create schema if not exists sync;

create table if not exists sync.sync_jobs (
    id uuid primary key default gen_random_uuid(),
    job_key text not null unique,
    provider text not null default 'sportmonks',
    entity_name text not null,
    description text null,
    enabled boolean not null default true,
    schedule text null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

create table if not exists sync.sync_cursors (
    id uuid primary key default gen_random_uuid(),
    sync_job_id uuid not null references sync.sync_jobs(id),
    cursor_key text not null,
    request_url text null,
    query_hash text null,
    next_page text null,
    has_more boolean not null default false,
    current_page integer null,
    last_success_at timestamptz null,
    last_error_at timestamptz null,
    last_error text null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    unique (sync_job_id, cursor_key)
);

create table if not exists sync.raw_payloads (
    id uuid primary key default gen_random_uuid(),
    provider text not null default 'sportmonks',
    entity_name text not null,
    endpoint text not null,
    request_url text not null,
    payload jsonb not null,
    captured_at timestamptz not null default now()
);

create table if not exists sync.api_requests (
    id uuid primary key default gen_random_uuid(),
    sync_job_id uuid null references sync.sync_jobs(id),
    provider text not null default 'sportmonks',
    entity_name text not null,
    endpoint text not null,
    request_url text not null,
    status_code integer null,
    rate_limit_remaining integer null,
    rate_limit_resets_at timestamptz null,
    duration_ms integer null,
    started_at timestamptz not null default now(),
    completed_at timestamptz null,
    error text null
);

create table if not exists sync.legacy_id_map (
    legacy_table text not null,
    legacy_id bigint not null,
    target_schema text not null,
    target_table text not null,
    target_id text not null,
    created_at timestamptz not null default now(),
    primary key (legacy_table, legacy_id)
);

create index if not exists ix_raw_payloads_payload_gin
    on sync.raw_payloads using gin (payload);

create index if not exists ix_api_requests_job_started_at
    on sync.api_requests (sync_job_id, started_at desc);
