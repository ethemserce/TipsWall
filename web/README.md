# TipsWall Admin Web

Next.js 15 (App Router) admin dashboard for the TipsWall stack.
Hosts the ops console operators see at `https://admin.tipswall.com`.

## Stack

- Next.js 15 + React 19 + TypeScript 5.7
- Tailwind 3.4 (no UI lib yet — plain Tailwind primitives in
  `src/app/**`). shadcn/ui can drop in later when we need richer
  components.
- httpOnly cookie session that wraps the existing TipsWall JWT pair
  (access + refresh). The backend's `[Authorize(Policy = "AdminOnly")]`
  is the actual gate; this UI just keeps the auth flow tidy.

## Local dev

```bash
cd web
npm install
NEXT_PUBLIC_API_BASE_URL=https://api.tipswall.com npm run dev
# open http://localhost:3000
```

For a local API instead, point at `http://<LAN_IP>:28333` — same shape
the mobile app uses.

## Auth bootstrap

1. Make an account through the mobile app (or via direct API call to
   `/api/v3/auth/signup`).
2. Flip the admin bit:
   ```sql
   UPDATE app.users SET is_admin = TRUE WHERE email = '<your-email>';
   ```
3. Re-login from the dashboard (or wait ~15 min for the next refresh).
   The `admin: true` JWT claim only mints when `is_admin = true` at
   issue time.

## Production deploy

The Dockerfile produces a standalone Next.js image (~150 MB). Plumbed
into `api/docker-compose.production.yml` as the `web` service; Caddy
forwards `admin.tipswall.com` to it on port 3000.

Build artefact pull-through CI: `.github/workflows/deploy.yml` builds
the image, pushes to GHCR, and the existing VPS pull picks it up.

## Adding a new ops widget

1. Backend: extend `IAdminOpsReader` + `PostgresAdminOpsReader` with
   the query.
2. Backend: expose it as a new `[HttpGet]` on `AdminOpsController`.
3. Web: add a `apiGet<T>(...)` call inside `src/app/ops/page.tsx`'s
   `loadOps()` (or split into its own page for heavier views).
4. Render the data through a Card / Stat / Table primitive in the
   same page.
