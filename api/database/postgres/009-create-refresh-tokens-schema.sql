-- Refresh tokens for V3 auth flow.
-- Stores hashed refresh tokens (SHA256 hex) supporting rotation and revocation.

create table if not exists app.refresh_tokens (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null references app.users(id) on delete cascade,
    token_hash text not null,
    expires_at timestamptz not null,
    revoked_at timestamptz null,
    revoked_reason text null,
    rotated_to_id uuid null references app.refresh_tokens(id) on delete set null,
    user_agent text null,
    ip_address inet null,
    created_at timestamptz not null default now(),
    constraint ck_refresh_tokens_revoked_reason
        check (revoked_reason in ('rotated', 'logout', 'expired', 'admin_revoke') or revoked_reason is null)
);

create unique index if not exists ux_refresh_tokens_token_hash
    on app.refresh_tokens (token_hash);

create index if not exists ix_refresh_tokens_user_active
    on app.refresh_tokens (user_id, revoked_at)
    where revoked_at is null;

create index if not exists ix_refresh_tokens_expires_at
    on app.refresh_tokens (expires_at);
