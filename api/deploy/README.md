# Deployment — single VPS + GitHub Actions

This directory has everything the CI/CD pipeline needs to deploy
TipsWall to a single VPS (tested on Hetzner CX32 and GoDaddy 2vCPU/4GB
Standard; any Ubuntu 22.04+ host works):

```
api/
├── Dockerfile.webapi              # already shipped
├── Dockerfile.worker.*            # already shipped
├── Dockerfile.migrator            # already shipped
├── docker-compose.production.yml  # production stack
└── deploy/
    ├── Caddyfile                  # TLS reverse proxy
    ├── .env.production.example    # config template
    ├── bootstrap.sh               # one-time VPS setup
    └── README.md                  # this file

.github/workflows/
├── test.yml                       # PR/main build + jest
└── deploy.yml                     # build → push GHCR → SSH deploy
```

## What happens on push to `main`

```
push (api/** changed)
  ↓
test.yml: backend dotnet build + mobile typecheck + jest
  ↓
deploy.yml:
  1. Builds 5 Docker images in parallel → pushes to GHCR
     (tagged with both `latest` and the commit SHA)
  2. SSH into the VPS:
       - git pull (latest compose file + migrations)
       - docker compose pull (latest images, pinned to SHA)
       - docker compose up -d
       - waits for webapi health
```

Postgres data is persisted to `/mnt/tipswall-data` on the Hetzner
Volume — survives any container restart, compose down, OS upgrade.

## One-time setup (60-90 min)

### 1. Order the Hetzner CX32 + Volume

- **Server**: CX32 (4 vCPU, 8 GB, 80 GB) in **Falkenstein** or **Helsinki**
  (lower latency to Turkey than Nuremberg)
- **OS image**: Ubuntu 24.04 LTS
- **Volume**: 100 GB attached to the same server, ext4 (format step is
  in `bootstrap.sh`, you don't need to pre-format)
- **Network**: enable IPv6, keep the default Hetzner firewall *off*
  (we use `ufw` on the host instead)

Save the IP that Hetzner assigns. You'll need it twice — for DNS and
for the GitHub secret.

### 2. Point DNS at the server

In your registrar (Cloudflare Registrar, Namecheap, etc.):

```
A   api.tipswall.com   <hetzner-ipv4>   TTL 300
AAAA api.tipswall.com  <hetzner-ipv6>   TTL 300   (optional)
```

Caddy will fetch a Let's Encrypt cert on first start. The cert flow
needs port 80 reachable from the public internet, which `ufw` opens.

### 3. SSH to the box as root and run bootstrap

```bash
ssh root@<hetzner-ipv4>
curl -fsSL https://raw.githubusercontent.com/<owner>/TipsWall/main/api/deploy/bootstrap.sh \
  | REPO_URL=https://github.com/<owner>/TipsWall.git sudo bash
```

The script:
- installs Docker + git + ufw
- creates the `tipswall` deploy user, adds them to the docker group
- generates an ed25519 SSH keypair (private key printed at the end —
  *copy it out of the terminal output*; this is the only time you'll
  see it)
- formats and mounts the Volume at `/mnt/tipswall-data`
- clones the repo to `/opt/tipswall`
- copies the env template to `/opt/tipswall/api/.env`
- enables `ufw` (22, 80, 443) and a nightly `pg_dump` cron

### 4. Fill in the env file

```bash
ssh tipswall@<ip>          # the deploy user, not root
sudo nano /opt/tipswall/api/.env
```

Fill in at minimum:

| Key | Notes |
|---|---|
| `GHCR_OWNER` | Lowercase GitHub username/org that owns the images |
| `DOMAIN` | `api.tipswall.com` (or whatever DNS points here) |
| `POSTGRES_PASSWORD` | strong random — `openssl rand -base64 24` |
| `PREODDS_JWT_SECRET` | 32+ chars — `openssl rand -base64 48` |
| `AUTH_ISSUER` / `AUTH_AUDIENCE` | `https://<DOMAIN>` |
| `CORS_ORIGIN_0` | `https://tipswall.com` (your marketing site) |
| `SPORTMONKS_API_TOKEN` | From SportMonks dashboard |

Leave the social-signin envs empty for now — see Faz 9 doc in
`CLAUDE.md` once you've set up the Apple/Google consoles.

### 5. Set the GitHub repo secrets

Repo → Settings → Secrets and variables → Actions → **New repo secret**:

| Secret | Source |
|---|---|
| `DEPLOY_HOST` | Server IPv4 from step 1 |
| `DEPLOY_USER` | `tipswall` |
| `DEPLOY_SSH_KEY` | Private key printed at the end of `bootstrap.sh` |
| `DEPLOY_SSH_PORT` | Custom SSH port, omit if 22 (most hosts) |
| `GHCR_USER` | Your GitHub username |
| `GHCR_PULL_TOKEN` | A PAT with `read:packages` scope (settings → developer settings → fine-grained tokens) |

Then create a **GitHub environment** called `production`:

- Repo → Settings → Environments → New environment → `production`
- (Optional) Add a manual approval step so only specific reviewers can
  promote to prod — the `deploy` workflow references this environment.

### 6. Kick off the first deploy

Either push a commit to `main`, or:

- Repo → Actions → `deploy` → Run workflow → Branch: main

Watch the run. First build takes ~10 min (no cache); subsequent
deploys hit GHA cache and finish in ~3 min.

When the workflow turns green, hit `https://api.tipswall.com/health/live`
in a browser — should return 200 with a small JSON body. Caddy's
Let's Encrypt issuance takes 10-30 seconds on first request.

## Day-2 operations

### Roll back to a previous commit

```bash
ssh tipswall@<ip>
cd /opt/tipswall/api
# Find the SHA you want
docker images | grep tipswall- | head
# Pin it
nano .env   # set IMAGE_TAG=<the-sha>
docker compose --env-file .env up -d
```

### Tail logs

```bash
docker compose --env-file .env logs -f webapi
docker compose --env-file .env logs -f worker-football
```

### Run a manual migration

Migrations run as part of every deploy. To re-run on demand:

```bash
docker compose --env-file .env run --rm migrator
```

### Restore from backup

Backups are at `/mnt/tipswall-data/backup/preodds-*.sql.gz`, kept for
14 days.

```bash
docker compose --env-file .env stop webapi worker-core worker-football worker-odds
zcat /mnt/tipswall-data/backup/preodds-20260511-030000.sql.gz \
  | docker compose --env-file .env exec -T postgres \
    psql -U preodds preodds
docker compose --env-file .env start webapi worker-core worker-football worker-odds
```

### Tune Postgres further

The compose file passes flags suited to an 8 GB host. If you upgrade
to CX42 (16 GB) or move to a managed Postgres, edit the `command:`
block (or remove it and let managed defaults kick in).

## Cost sheet

| Item | Monthly |
|---|---|
| Hetzner CX32 (4 vCPU, 8 GB) | €5.83 |
| Hetzner Volume 100 GB | €4.00 |
| Hetzner Storage Box 1 TB (off-site backups, optional) | €4.00 |
| Domain (Cloudflare Registrar, .com) | $0.83 (~€0.80) |
| **Total** | **~€14.60 / month** |

Everything else (GitHub Actions, GHCR, Caddy, Let's Encrypt, Sentry
free tier, UptimeRobot) is free.
