# Production Deployment Runbook

End-to-end procedure for bringing PreOdds up in production. Pairs with
[setup-database-and-workers.md](./setup-database-and-workers.md), which is
the local-dev counterpart.

> **Audience:** the operator running the deploy. Assumes you can reach the
> target host via SSH or your platform's equivalent and have Docker
> installed. The repo's `docker-compose.production.yml` is the source of
> truth for service topology.

---

## 1. Required secrets

These values **must not** live in the repo. Use whichever vault your
platform provides (AWS Secrets Manager, GCP Secret Manager, Doppler,
HashiCorp Vault) and surface them to the host as environment variables.

| Variable | Used by | Why |
|---|---|---|
| `PREODDS_POSTGRES_CONNECTION` | webapi, workers, migrator | Npgsql connection string with prod credentials. |
| `PREODDS_JWT_SECRET` | webapi | At least 32 chars. The webapi refuses to boot outside Development if this looks like the placeholder. |
| `Authentication__Issuer` | webapi | Public URL the JWT is issued by, e.g. `https://api.preodds.example`. |
| `Authentication__Audience` | webapi | Same — usually identical to issuer. |
| `Cors__AllowedOrigins__0` (etc.) | webapi | Origins permitted to call the API. CORS errors loudly if unset outside Development. |
| `SportMonks__ApiToken` | workers | SportMonks tenant token. Free-plan token is fine for the 2-league pilot. |

For mobile builds, additional EAS-side secrets:

| Variable | Used by | Why |
|---|---|---|
| `EXPO_PUBLIC_API_BASE_URL` | mobile | Production API URL. EAS injects per profile (`eas.json`). |
| `EXPO_PUBLIC_SENTRY_DSN` | mobile | Sentry DSN. Empty string disables monitoring (safe default). |
| `ASC_APP_ID` / `APPLE_TEAM_ID` | `eas submit` | App Store Connect identifiers. |
| `GOOGLE_SERVICE_ACCOUNT_KEY` | `eas submit` | Play store service-account JSON, uploaded as an EAS secret. |

> Set EAS-side secrets with `eas secret:create --scope project --name NAME --value VALUE`.

---

## 2. Database migrations

Migrations live in `database/postgres/*.sql` and are tracked in
`sync.schema_migrations` by `PreOddsApi.Migrator`. The migrator is the
**only** thing that should mutate schema in production — never run
`psql` ad hoc against prod.

### First deploy
```bash
docker compose -f docker-compose.production.yml run --rm migrator
```
The migrator applies every file in alphabetical order, records each
filename + sha256 hash, and exits 0. If the bundle has been edited
since being applied (hash mismatch), it stops with a non-zero exit.

### Routine deploys
Every release re-runs the migrator before starting the webapi:
```bash
docker compose -f docker-compose.production.yml run --rm migrator \
  && docker compose -f docker-compose.production.yml up -d webapi worker-core worker-football worker-odds
```

### Rollback
There is **no rollback runner**. To roll back you need a hand-written
reverse SQL file applied through `psql` (kept under
`database/postgres/rollback/`). Plan rollbacks before you ship the
forward migration.

---

## 3. Service topology

`docker-compose.production.yml` defines five services:

```
postgres ──┬── migrator (one-shot)
           ├── webapi      (HTTP + SignalR; the public surface)
           ├── worker-core
           ├── worker-football
           └── worker-odds
```

Each worker is independent — you can restart one without disturbing the
others. The webapi and the workers all read the same Postgres; SignalR
is broadcast from the webapi only, the workers post `FixtureUpdated`
events into the hub via the internal HTTP route.

A reverse proxy (nginx / Caddy / your platform's LB) terminates TLS in
front of the webapi. `app.UseHttpsRedirection()` is on, so plain-http
clients are redirected to https.

---

## 4. Health, metrics, logs

| Endpoint | What it tells you |
|---|---|
| `/health/live` | Process is up. No DB ping. Use this for liveness probes. |
| `/health/ready` | DB reachable + sync freshness within 2h. **Degraded** if any worker is stuck — load balancer can keep traffic flowing while dashboards alert. |
| `/metrics` | OpenTelemetry → Prometheus exposition. ASP.NET Core, HttpClient, runtime, Npgsql instrumentation. |
| `/swagger` | **Development only.** Disabled in production. |

Logs are emitted by Serilog with a correlation id stamped by
`CorrelationIdMiddleware`. Every log line carries `CorrelationId` so a
user-reported issue can be traced end-to-end. The default sink is
console; pipe stdout to your log aggregator (Loki, Seq, CloudWatch,
ELK, etc.).

---

## 5. Mobile release flow

```bash
# Build a store-bound binary. The `production` profile is the contract.
eas build --profile production --platform all

# Upload to TestFlight / Play Internal.
eas submit --profile production --platform all
```

`autoIncrement: true` on the production profile bumps build numbers
automatically. Versioning is `appVersionSource: "remote"` — EAS owns
the build counter, your `package.json` version is the marketing
version.

For a hotfix that doesn't touch native code, ship via OTA:
```bash
eas update --branch production --message "fix: <summary>"
```
The mobile app downloads the new bundle on next launch.

---

## 6. Deploy checklist

Before merging the release branch:
- [ ] All CI jobs green (backend tests + integration + Docker build, mobile typecheck/lint/test).
- [ ] Migration files in `database/postgres/` named with the next sequential prefix.
- [ ] No `CHANGE_ME` placeholders touched in `appsettings.json`.
- [ ] Mobile `app.json` version bumped if the release introduces user-visible UI changes.

After deploying:
- [ ] `/health/ready` returns 200 with `Healthy` (not `Degraded`).
- [ ] `/metrics` is scraping (`up{job="preodds-webapi"} == 1` in Prometheus).
- [ ] Pick one log line in your aggregator and verify `CorrelationId` is populated.
- [ ] Hit one read endpoint (`/api/v3/signals?bookmaker_id=2`) and confirm 200 + sample payload.

---

## 7. Common failures

| Symptom | Likely cause | Fix |
|---|---|---|
| Webapi exits with `A strong PREODDS_JWT_SECRET... is required` | Default placeholder in env | Set the secret. |
| `/health/ready` returns 503 | Postgres unreachable from the container | Check `PREODDS_POSTGRES_CONNECTION`, network policy, DNS. |
| `/health/ready` returns 200 with **Degraded** | Workers haven't completed a successful run in the freshness window | Check worker logs. SportMonks token revoked? Free-plan rate limit? |
| Mobile login works but every subsequent call returns 401 | Refresh interceptor not retrying — likely missing `expo-secure-store` | Verify `npm install` ran on the build host or that the fallback to AsyncStorage is working. |
| SignalR live banner stuck on "yeniden bağlanılıyor…" | WebSocket upgrade blocked by reverse proxy | Enable WS upgrade headers on the proxy (`Upgrade`, `Connection`). |

---

## 8. Disaster recovery

Postgres is the source of truth. Back it up daily:
```bash
docker compose -f docker-compose.production.yml exec postgres \
  pg_dump -U preodds preodds | gzip > "preodds-$(date +%F).sql.gz"
```
Store off-host (S3, GCS) with at least 14 days retention. The fixture
sync workers can re-fetch SportMonks data, but user accounts, refresh
tokens, coupons (when persisted server-side) and audit logs are only
in Postgres.
