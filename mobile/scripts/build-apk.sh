#!/usr/bin/env bash
# Local Android APK build wrapper.
#
# Problem this solves: `google-services.json` + `GoogleService-Info.plist`
# are .gitignore'd (Firebase config — should never be committed), but
# `eas build --local` only uploads git-tracked files. Without an extra
# step, the build fails at prebuild with ENOENT on google-services.json.
#
# Workaround: temporarily comment out the two ignore lines, run the
# build, then restore them. The `trap` ensures restore fires whether
# the build succeeds, fails, or the user Ctrl+C's mid-run.
#
# Pair with the gradle-daemon cleanup so stale locks from a prior
# crashed build don't block this one.
#
# Usage: from `mobile/`, run `./scripts/build-apk.sh`.
#
# Why not commit the Firebase files? They carry the Firebase project_id
# / app_id pair — public on the device once shipped, but committing them
# is still avoided so a public repo / leaked diff doesn't surface them
# without rotation friction.

set -euo pipefail

cd "$(dirname "$0")/.."

GITIGNORE="$(pwd)/.gitignore"

restore_gitignore() {
  # The replacements are no-ops if the lines are already uncommented
  # (sed runs idempotent on the un-prefixed match), so this is safe to
  # call even on early-exit paths.
  sed -i 's|^#google-services\.json$|google-services.json|'       "$GITIGNORE" 2>/dev/null || true
  sed -i 's|^#GoogleService-Info\.plist$|GoogleService-Info.plist|' "$GITIGNORE" 2>/dev/null || true
}
trap restore_gitignore EXIT

echo "[build-apk] uncommenting Firebase config lines in .gitignore…"
sed -i 's|^google-services\.json$|#google-services.json|'       "$GITIGNORE"
sed -i 's|^GoogleService-Info\.plist$|#GoogleService-Info.plist|' "$GITIGNORE"

echo "[build-apk] killing stale Gradle daemons + lock files…"
pkill -9 -f 'GradleDaemon|gradle' 2>/dev/null || true
rm -f "$HOME/.gradle/caches/journal-1/journal-1.lock" 2>/dev/null || true

echo "[build-apk] starting EAS local Android build (profile=preview)…"
eas build --platform android --profile preview --local "$@"
