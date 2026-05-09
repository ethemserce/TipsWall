-- 014 — Single-use tokens for account lifecycle flows.
--
-- Backs password-reset + email-verification flows. Pattern mirrors
-- app.refresh_tokens (hash-only storage, expiry, per-token use limit)
-- so we never persist a token in a form that could be replayed if the
-- DB is compromised.
--
--   purpose:       which lifecycle flow this token belongs to
--   token_hash:    sha256(rawToken); the raw token never touches disk
--   expires_at:    short-lived (1h reset, 24h verify by default)
--   consumed_at:   stamped when redeemed; a token is single-use
--   user_id:       fk to the target user, cascade-deletes on user purge
--
-- The raw token only exists in:
--   1. The email body sent to the user
--   2. Memory of the controller that issued it
-- Once consumed, the row stays for audit + replay protection (anyone
-- presenting an already-consumed token gets a 400, not a 401, so the
-- caller can distinguish "wrong token" from "expired link").

create table if not exists app.account_tokens (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null references app.users(id) on delete cascade,
    purpose text not null,
    token_hash text not null,
    expires_at timestamptz not null,
    consumed_at timestamptz null,
    created_at timestamptz not null default now(),
    constraint ck_account_tokens_purpose
        check (purpose in ('password_reset', 'email_verify'))
);

create unique index if not exists ux_account_tokens_hash
    on app.account_tokens (token_hash);

create index if not exists ix_account_tokens_user_purpose
    on app.account_tokens (user_id, purpose, consumed_at);

create index if not exists ix_account_tokens_expires_at
    on app.account_tokens (expires_at);
