#!/usr/bin/env bash
# Hetzner CX32 + 100GB Volume one-time bootstrap for TipsWall.
#
# Run ONCE on a fresh Ubuntu 24.04 LTS VPS, as root or via sudo:
#   curl -fsSL https://raw.githubusercontent.com/<owner>/TipsWall/main/api/deploy/bootstrap.sh | sudo bash
#
# What it does:
#   1. Updates apt; installs Docker Engine + compose plugin + git.
#   2. Creates the `tipswall` deploy user with docker access.
#   3. Generates an SSH keypair the GitHub Actions deploy job will use
#      (public key goes in authorized_keys, private key you copy to
#      the GH repo secret HETZNER_SSH_KEY).
#   4. Mounts the Hetzner Volume at /mnt/tipswall-data and lays out
#      the directory structure the compose file expects.
#   5. Clones the repo at /opt/tipswall and copies the env template.
#
# After bootstrap you must:
#   - Fill in /opt/tipswall/api/.env (POSTGRES_PASSWORD, JWT secret, etc.)
#   - Point your DNS A record at the VPS IP
#   - Push the listed secrets into the GitHub repo
#   - Trigger the first deploy from GitHub Actions

set -euo pipefail

DEPLOY_USER="tipswall"
DEPLOY_HOME="/home/${DEPLOY_USER}"
APP_DIR="/opt/tipswall"
DATA_DIR="/mnt/tipswall-data"
REPO_URL="${REPO_URL:-https://github.com/ethemserce/TipsWall.git}"
VOLUME_DEVICE="${VOLUME_DEVICE:-}"  # e.g. /dev/sdb — empty means "auto-detect"

log() { echo -e "\033[1;34m▶\033[0m $*"; }
warn() { echo -e "\033[1;33m!\033[0m $*"; }
die() { echo -e "\033[1;31m✗\033[0m $*" >&2; exit 1; }

if [[ $EUID -ne 0 ]]; then
  die "Run me with sudo / as root."
fi

# ----------------------------------------------------------------------
# 1) apt + Docker
# ----------------------------------------------------------------------
log "Updating apt + installing base packages…"
export DEBIAN_FRONTEND=noninteractive
apt-get update -y
apt-get upgrade -y
apt-get install -y \
  ca-certificates curl gnupg lsb-release \
  git ufw jq unattended-upgrades

if ! command -v docker >/dev/null 2>&1; then
  log "Installing Docker Engine…"
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
    | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  chmod a+r /etc/apt/keyrings/docker.gpg
  echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
       https://download.docker.com/linux/ubuntu \
       $(lsb_release -cs) stable" \
    > /etc/apt/sources.list.d/docker.list
  apt-get update -y
  apt-get install -y docker-ce docker-ce-cli containerd.io \
    docker-buildx-plugin docker-compose-plugin
fi

# ----------------------------------------------------------------------
# 2) Deploy user
# ----------------------------------------------------------------------
if ! id -u "${DEPLOY_USER}" >/dev/null 2>&1; then
  log "Creating deploy user '${DEPLOY_USER}'…"
  useradd --create-home --shell /bin/bash "${DEPLOY_USER}"
fi
usermod -aG docker "${DEPLOY_USER}"

# Ensure the user can read /opt/tipswall.
mkdir -p "${APP_DIR}"
chown -R "${DEPLOY_USER}:${DEPLOY_USER}" "${APP_DIR}"

# ----------------------------------------------------------------------
# 3) SSH key for GitHub Actions deploy
# ----------------------------------------------------------------------
SSH_DIR="${DEPLOY_HOME}/.ssh"
KEY_PATH="${SSH_DIR}/github_actions"
mkdir -p "${SSH_DIR}"
chmod 700 "${SSH_DIR}"

if [[ ! -f "${KEY_PATH}" ]]; then
  log "Generating ed25519 SSH key for GitHub Actions…"
  sudo -u "${DEPLOY_USER}" ssh-keygen -t ed25519 \
    -C "github-actions@tipswall" -f "${KEY_PATH}" -N ""
fi
touch "${SSH_DIR}/authorized_keys"
chmod 600 "${SSH_DIR}/authorized_keys"
grep -qxF "$(cat "${KEY_PATH}.pub")" "${SSH_DIR}/authorized_keys" \
  || cat "${KEY_PATH}.pub" >> "${SSH_DIR}/authorized_keys"
chown -R "${DEPLOY_USER}:${DEPLOY_USER}" "${SSH_DIR}"

# ----------------------------------------------------------------------
# 4) Hetzner Volume mount
# ----------------------------------------------------------------------
if [[ -z "${VOLUME_DEVICE}" ]]; then
  # Hetzner volumes are typically /dev/sdb on a single-disk CX32.
  VOLUME_DEVICE=$(lsblk -dpno NAME,TYPE,MOUNTPOINT \
    | awk '$2=="disk" && $3=="" {print $1; exit}')
fi

if [[ -n "${VOLUME_DEVICE}" && -b "${VOLUME_DEVICE}" ]]; then
  log "Setting up Hetzner Volume at ${VOLUME_DEVICE}…"
  if ! blkid "${VOLUME_DEVICE}" >/dev/null 2>&1; then
    log "Formatting ${VOLUME_DEVICE} as ext4 (one-time, irreversible)…"
    mkfs.ext4 -L tipswall "${VOLUME_DEVICE}"
  fi
  mkdir -p "${DATA_DIR}"
  UUID=$(blkid -s UUID -o value "${VOLUME_DEVICE}")
  if ! grep -q "${UUID}" /etc/fstab; then
    echo "UUID=${UUID} ${DATA_DIR} ext4 defaults,nofail 0 2" >> /etc/fstab
  fi
  mount -a
  mkdir -p "${DATA_DIR}/postgres" "${DATA_DIR}/backup"
  chown -R "${DEPLOY_USER}:${DEPLOY_USER}" "${DATA_DIR}"
else
  warn "No unmounted block device found. Skipping volume mount."
  warn "If you attached a Hetzner Volume, re-run with VOLUME_DEVICE=/dev/sdX."
  mkdir -p "${DATA_DIR}/postgres" "${DATA_DIR}/backup"
  chown -R "${DEPLOY_USER}:${DEPLOY_USER}" "${DATA_DIR}"
fi

# ----------------------------------------------------------------------
# 5) Clone repo + env template
# ----------------------------------------------------------------------
if [[ ! -d "${APP_DIR}/.git" ]]; then
  log "Cloning ${REPO_URL} into ${APP_DIR}…"
  sudo -u "${DEPLOY_USER}" git clone "${REPO_URL}" "${APP_DIR}"
fi

ENV_PATH="${APP_DIR}/api/.env"
if [[ ! -f "${ENV_PATH}" ]]; then
  log "Copying .env.production.example → ${ENV_PATH}"
  cp "${APP_DIR}/api/deploy/.env.production.example" "${ENV_PATH}"
  chown "${DEPLOY_USER}:${DEPLOY_USER}" "${ENV_PATH}"
  chmod 600 "${ENV_PATH}"
fi

# ----------------------------------------------------------------------
# 6) Firewall
# ----------------------------------------------------------------------
log "Configuring ufw (ssh, http, https)…"
ufw --force reset
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 443/udp  # HTTP/3
ufw --force enable

# ----------------------------------------------------------------------
# 7) Backup cron (pg_dump nightly → /mnt/tipswall-data/backup)
# ----------------------------------------------------------------------
log "Installing nightly backup cron…"
cat > /etc/cron.daily/tipswall-backup <<'CRON'
#!/usr/bin/env bash
set -euo pipefail
cd /opt/tipswall/api || exit 0
[[ -f .env ]] || exit 0
TS=$(date +%Y%m%d-%H%M%S)
OUT="/mnt/tipswall-data/backup/preodds-${TS}.sql.gz"
docker compose --env-file .env exec -T postgres \
  pg_dump -U preodds preodds | gzip > "${OUT}"
# Keep last 14 days.
find /mnt/tipswall-data/backup -name 'preodds-*.sql.gz' -mtime +14 -delete
CRON
chmod +x /etc/cron.daily/tipswall-backup

# ----------------------------------------------------------------------
# Done
# ----------------------------------------------------------------------
log "Bootstrap complete."
echo
echo "Next steps:"
echo
echo " 1. Fill in /opt/tipswall/api/.env (passwords, JWT secret, etc.)."
echo " 2. Point your DNS A record to this VPS IP:"
echo "      $(curl -s ifconfig.me || echo '<run: curl ifconfig.me>')"
echo " 3. Copy the GitHub-actions private key into the repo secret"
echo "    HETZNER_SSH_KEY:"
echo
echo "    --- BEGIN PRIVATE KEY ---"
cat "${KEY_PATH}"
echo "    --- END PRIVATE KEY ---"
echo
echo " 4. Set the rest of the repo secrets — see api/deploy/README.md."
echo " 5. Trigger the deploy workflow from GitHub Actions."
echo
