# Auth Bridge Cutover

The unlock condition for deleting the legacy projects (BusinessLayer,
DataLayer, Core*, Utils, BusinessLayer.Entities, plus the WebApi/Models
+ MappingProfile sub-trees). This runbook walks through how to retire
the legacy bridge cleanly.

---

## Why the bridge exists

`PostgresUserIdentityService.AuthenticateAsync` has two code paths:

1. **Modern path.** Look up `app.users` by username/email; if found and
   the password hash verifies under bcrypt, return the user.
2. **Bridge path.** No `app.users` row found → call
   `IPrdUserService.GetUser(username, password)` against the legacy
   `prd_user` table; on success, mint an `app.users` row via
   `BridgeLegacyUserAsync` so the next login skips the bridge.

The bridge keeps users from the pre-V3 auth system from being orphaned
during the cutover. Until *every* active legacy user has logged in at
least once after the V3 deploy, ripping out the bridge would lock them
out.

The bridge is the only consumer of:
- `PreOddsApi.BusinessLayer` (defines `IPrdUserService`)
- `PreOddsApi.BusinessLayer.Entities` (`PrdUserBusinessModel`)
- `PreOddsApi.DataLayer` (`UnitOfWork<PreOddsApiDbContext>`)
- `PreOddsApi.Core.Data.EntityFramework` (DbContext)
- `PreOddsApi.Core.Data` / `PreOddsApi.Core` / `PreOddsApi.Utils` (transitively)
- `PreOddsApi.Entities/PreOddsEntities` (`prd_user`)
- `WebApi/Models/*` + `WebApi/MappingProfile.cs` (AutoMapper for the bridge mapping)

Removing all of these is one PR after the cutover lands.

---

## Cutover plan

### Phase 1 — Pre-cutover audit (no production impact)

Run a one-shot SQL query on production Postgres to size the migration:

```sql
-- Legacy users with no corresponding app.users row.
-- The lower(...) match mirrors PostgresUserIdentityService.FindUserIdByEmailOrUsername.
select count(*)
from public.prd_user pu
where not exists (
    select 1
    from app.users u
    where lower(u.username) = lower(pu.nick_name)
       or (u.email is not null and lower(u.email) = lower(pu.email))
);
```

If this returns 0, you can skip Phase 2 entirely and jump to Phase 3.
Otherwise, the count is your migration scope.

### Phase 2 — Bulk pre-bridge (preserves users, forces password reset)

For each unbridged legacy user, create an `app.users` row with:
- `username` = `prd_user.nick_name`
- `email` = `prd_user.email`
- `password_hash` = NULL (so login by password is impossible)
- `email_verified_at` = NULL
- `status` = 'pending_password_reset'

```sql
insert into app.users (username, email, status, password_hash, role)
select pu.nick_name,
       pu.email,
       'pending_password_reset',
       null,
       'user'
from public.prd_user pu
where not exists (
    select 1 from app.users u
    where lower(u.username) = lower(pu.nick_name)
       or (u.email is not null and lower(u.email) = lower(pu.email))
)
on conflict do nothing;
```

After this insert, every legacy user has an `app.users` row but cannot
log in until they reset their password via
`POST /api/v3/auth/forgot-password` → reset link → set new password.

Send a one-shot email blast: "We've upgraded our auth system; click here
to set your new password." Track click-throughs.

### Phase 3 — Disable the bridge

After ≥30 days post-Phase 2 (or when forced-reset rate flatlines),
ship a release that:

1. Replaces `PostgresUserIdentityService.AuthenticateAsync`'s legacy
   branches with a single `app.users` lookup. Drop the
   `IPrdUserService _legacyUserService` constructor parameter.
2. Removes `using PreOddsApi.BusinessLayer.Abstract;` from the file.

The legacy projects are still referenced at this point — the build
stays green — but no live code path touches them anymore.

### Phase 4 — Delete the chain

In a follow-up PR (split for review safety):

1. **Program.cs**:
   - Drop `using PreOddsApi.BusinessLayer.DependencyInjection;` and the
     `DependencyService.SetDependencyTypes(...)` call.
   - Drop `services.AddAutoMapper(typeof(MappingProfile));`.
2. **WebApi.csproj**: remove the three `ProjectReference` entries to
   BusinessLayer / BusinessLayer.Entities / Entities.
3. Delete the project directories:
   - `PreOddsApi.BusinessLayer/`
   - `PreOddsApi.BusinessLayer.Entities/`
   - `PreOddsApi.DataLayer/`
   - `PreOddsApi.Core.Data.EntityFramework/`
   - `PreOddsApi.Core.Data/`
   - `PreOddsApi.Core/`
   - `PreOddsApi.Utils/`
4. Strip the corresponding `Project` + `ProjectConfigurationPlatforms` +
   `NestedProjects` blocks from `PreOddsApi.sln`.
5. Delete `WebApi/Models/` and `WebApi/MappingProfile.cs` (their only
   consumer, AutoMapper for the bridge, is gone).
6. Drop the AutoMapper + Newtonsoft.Json + Microsoft.AspNetCore.Mvc.NewtonsoftJson
   package references from `WebApi.csproj` (Newtonsoft was already
   demoted to dead code by the System.Text.Json migration; this step
   removes the package itself).
7. Workers should still build — they only reference `Entities` +
   `ExternalApis`. Verify with `dotnet build PreOddsApi.sln`.
8. Drop `PreOddsApi.Entities` if Workers can move to a thinner shape.
   `PreOddsApi.ExternalApis` is needed by Workers and stays. Inspect
   what `Entities/PreOddsEntities/*` types Workers actually use; if
   only a few, inline them and delete `Entities`.

After Phase 4, the solution drops from ~14 active projects to ~5
(WebApi, ExternalApis, Migrator, Worker.{Core,Football,Odds}, Tests).

---

## Rollback safety

- Phase 2 (bulk pre-bridge) is reversible: the inserted `app.users`
  rows can be deleted by status='pending_password_reset' if a user
  complaint surfaces.
- Phase 3 (bridge disable) requires a code revert to restore the
  legacy fallback. Keep the previous release tagged for at least the
  same 30-day window.
- Phase 4 deletions are not reversible without git revert + redeploy.
  Do them only after Phase 3 has been live for ≥30 days with no auth
  incidents.

---

## Why we're not doing this in code today

This runbook lives here because the cutover requires production
coordination — a database migration that touches user records, an
email blast, and a 30-day waiting window. None of that belongs in a
single feature PR.

Open this doc when ops is ready and execute Phases 1-2 first.
