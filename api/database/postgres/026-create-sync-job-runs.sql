-- Persist scheduler state + per-job audit log so worker restarts don't
-- re-fire every job (the in-process scheduler reset on every container
-- recreate and triggered fixture-backlog, players, transfers etc. on
-- top of the SportMonks rate-limit budget). One table covers both:
--   - ShouldRun reads MAX(completed_at) per job_key.
--   - Each completed sync inserts a row carrying status + items_count
--     so we can ask "ne zaman kaç row yazıldı, hangi adım fail etti".
-- Small enough (a few hundred rows/day) that pruning isn't urgent.

create schema if not exists sync;

create table if not exists sync.job_runs (
    id              bigserial primary key,
    job_key         text not null,
    started_at      timestamptz not null default now(),
    completed_at    timestamptz null,
    -- 'success' | 'failure' — kept as text instead of enum so adding
    -- partial-success / skipped states later is a single migration.
    status          text not null default 'success',
    items_count     integer null,
    error_message   text null
);

-- ShouldRun() reads the most-recent completion per job. Without this
-- index the lookup is a seq-scan once the table reaches a few k rows.
create index if not exists ix_job_runs_key_completed
    on sync.job_runs (job_key, completed_at desc nulls last)
    where status = 'success';

-- Convenience index for failure-rate / recent-error queries (the
-- /healthz endpoint will use this).
create index if not exists ix_job_runs_status_completed
    on sync.job_runs (status, completed_at desc)
    where status <> 'success';
