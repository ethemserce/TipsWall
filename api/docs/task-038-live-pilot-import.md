# Task 038 - Live Pilot Import

## Goal

Run a controlled, real SportMonks import in production-like conditions to validate the
end-to-end pipeline: API client → sync runner → PostgreSQL writers → V3 read API.

This task delivers the **operations runbook**, **pilot config preset**, and **diagnostics
endpoints**. It does not enable any sync flags by default — the operator must take
explicit action to start the pilot.

## Pre-flight Checklist

Before enabling the pilot:

- [ ] PostgreSQL database is reachable and migrations 001–008 are applied.
- [ ] `PREODDS_POSTGRES_CONNECTION` env var is set (or `ConnectionStrings:PreOddsApiPostgresDb`).
- [ ] `PREODDS_JWT_SECRET` is set to a strong value (≥32 chars, not `CHANGE_ME...`) outside Development.
- [ ] SportMonks API token is set via `SportMonks:ApiToken` (or `PREODDS_SPORTMONKS_TOKEN` env var).
- [ ] SportMonks subscription includes the football endpoints used (fixtures by date).
- [ ] CI green on the deployed commit.

## Pilot Config

Copy the preset and edit secrets:

```
cp PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/appsettings.pilot.example.json \
   PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture/appsettings.json
```

Edit `appsettings.json` — set `SportMonks:ApiToken` and `ConnectionStrings:PreOddsApiPostgresDb`.
**Never commit `appsettings.json`** — it is gitignored.

The preset enables only:

- `SportMonksFixtureSync` (today + tomorrow only)
- Reference data (leagues, teams, states, venues — runs once every 6 hours)

Everything else (players, standings, transfers, TV, news, prematch odds, inplay odds,
analytics) stays disabled.

## Step-by-Step Enable

1. Apply pilot config (above).
2. Start the football worker:
   ```
   dotnet run --project PreOddsApi.Worker/SportMonks/SportMonks.Football.Fixture
   ```
3. Wait 2-3 minutes for the first poll cycle.
4. Verify via diagnostics:
   - `GET /api/v3/admin/sync-status` — every job_key should have a `last_success_at` set
     and no `last_error`.
   - `GET /api/v3/admin/recent-requests?limit=20` — recent requests should be `status_code=200`.
5. Spot-check writes:
   - `select count(*) from football.fixtures` — should grow.
   - `select count(*) from sync.raw_payloads` — should grow.

## Monitoring Queries

Open a `psql` session and watch:

```sql
-- last successful run per job
select job_key, entity_name, last_success_at, last_error_at, last_error
from sync.sync_jobs j
left join sync.sync_cursors c on c.sync_job_id = j.id
order by last_success_at desc nulls last;

-- API call success rate (last hour)
select status_code, count(*)
from sync.api_requests
where started_at > now() - interval '1 hour'
group by status_code;

-- Recent failures
select endpoint, error, started_at
from sync.api_requests
where error is not null
order by started_at desc
limit 20;
```

## Rollback

To stop the pilot immediately:

1. Stop the worker process.
2. Set `SportMonksFixtureSync:Enabled=false` in `appsettings.json`.
3. Restart the worker (or just leave it stopped).

The synced rows in PostgreSQL stay — you can clean them with truncates if needed:

```sql
truncate football.fixture_participants, football.fixture_scores cascade;
truncate football.fixtures cascade;
truncate sync.raw_payloads, sync.api_requests, sync.sync_cursors cascade;
```

## Quota Considerations

SportMonks charges per request. The pilot config keeps usage minimal:

- `DefaultPerPage=25` (lower than 50 default)
- `FixtureIntervalSeconds=600` (10 min between fixture polls vs 5 min default)
- `DaysForward=1` (2 dates vs 8 dates default)
- `RequestDelayMs=2000` (where applicable, vs 1000 default)
- `PollingIntervalSeconds=120` (slower top-level loop)

Expected request volume: ~12 fixture-by-date requests per hour + 1 reference cycle every
6 hours. Adjust intervals upward if quota is tight.

## Acceptance Criteria

Task 38 is accepted when:

1. `dotnet build PreOddsApi.sln --configuration Release` is 0 errors.
2. `GET /api/v3/admin/sync-status` returns the registered jobs (even with no successes yet).
3. `GET /api/v3/admin/recent-requests?limit=10` returns up to 10 most recent api_requests rows.
4. `appsettings.pilot.example.json` is checked in and clearly marked as the pilot preset.
5. This runbook is checked in.
6. Repository contains no real SportMonks token.

## Out of Scope

- Production deployment automation (Docker/k8s/systemd).
- Multi-region replication.
- Backups (separate ops task).
- Quota-aware request budgeting beyond per-page delay.
- Inplay odds pilot — defer until prematch + fixture pilot is stable.
