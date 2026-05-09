# TipsWall

Monorepo for the TipsWall sports analytics product. Combines the .NET 8 backend
+ workers (`api/`) and the Expo SDK 54 React Native client (`mobile/`).

This file is loaded into every Claude Code session that opens this repo —
keep it tight and current. Update it when conventions change or major
features land.

## Layout

```
TipsWall/
├─ api/        # .NET 8 minimal-hosting WebAPI + 3 SportMonks workers
├─ mobile/     # Expo SDK 54 React Native client (Hermes, expo-router)
└─ Logo/       # brand assets (images, etc.)
```

History from the original `PreOddsApi` and `PreOddsMobile` repos is
preserved via `git subtree` — every commit before 2026-05-10 lives in the
combined log. New work goes through normal commits at the monorepo root.

## Backend (`api/`)

- **Stack**: .NET 8, Npgsql + NpgsqlDataSource (multiplexing pool), Serilog
  (Console + File + Sentry sink), OpenTelemetry (HttpClient + Runtime +
  Npgsql via `.AddSource("Npgsql")`), Sentry.AspNetCore, JWT with refresh
  rotation, System.Text.Json with `JsonNamingPolicy.SnakeCaseLower`.
- **Workers**: 3 SportMonks workers — Core (port 8081 health), Football
  (8082), Odds (8083). Shared `WorkerObservability` + `WorkerHealthEndpoint`
  cross-linked via `<Compile Include="..\..\WorkerObservability.cs" Link>`.
  Health endpoints bind to `http://localhost:{port}/` (NOT `+`) — Windows
  needs an admin urlacl reservation for `+` and `http://localhost` works
  for the kubelet-style "curl localhost from inside the pod" probe contract.
- **VAR active flag** in fixture listing uses `created_at > now() - 90s`
  (NOT `last_synced_at` — the worker bumps that every sync, leaving the
  badge stuck on the home list for the rest of the match).
- **Run locally** (Postgres on :15432 expected):
  ```
  cd api/PreOddsApi.WebApi
  ASPNETCORE_URLS="http://localhost:28333;http://<LAN_IP>:28333" \
    dotnet run --no-build --no-launch-profile
  ```
  Workers similar; `WORKER_HEALTH_PORT=808X` per worker.

## Mobile (`mobile/`)

- **Stack**: Expo SDK 54, React Native 0.81, Hermes, expo-router file-based
  routing, TanStack Query, custom subscription store (NOT Zustand —
  preferred pattern: module-level snapshot + listener Set, see
  `src/lib/auth/authStore.ts`), expo-secure-store w/ AsyncStorage fallback,
  i18next + expo-localization (TR detected from device, otherwise EN).
- **Routes** (root Stack hoists detail pages out of `(tabs)` so back
  preserves the originating tab):
  ```
  app/
  ├─ _layout.tsx             # root Stack
  ├─ (tabs)/                 # bottom tabs: home / analysis / leagues / coupons / settings
  ├─ fixture/[id].tsx        # match detail
  ├─ league/[id].tsx         # league detail (matches/stats/standings)
  └─ team/[id].tsx           # team detail
  ```
- **Settings store** (`src/lib/settings/settingsStore.ts`): persists
  `themeMode` (`system|light|dark`), `languageMode` (`system|en|tr`),
  `oddsHidden`. AsyncStorage; subscribe pattern; `i18n/index.ts` re-syncs
  language on change.
- **Hide odds** mode: ORAN columns disappear in tabular displays
  (OddsRatesCard, RateMatchCard); coupon legs / suggestions / top picks
  show the tip text (`MS 1`, `KG Var`) where the odd value used to be.
  Aggregates (parlay total, fair odd) are hidden entirely.
- **Gesture dictionary** — uniform thresholds across the app:
  `dominance > 1.5×`, `recognition > 12px`, `trigger > 50px`. Used for:
  - home: state filter swipe (all/live/upcoming/finished)
  - fixture detail: tab content swipe + top-area swipe to sibling fixtures
    in the same league + matchday
  - league detail: tab swipe (matches/stats/standings) + week swipe inside
    Matches tab
  - peek overlay: sibling-fixture swipe when locked
- **Long-press fixture peek** (home / league / team): 2s lock → X to
  close + scrollable timeline. Closure-via-ref pattern keeps PanResponder
  stable while reading the latest state.
- **Metric naming**:
  - DSO → **HIT** (winning_percent — historical hit rate)
  - VBET → **ROI** (earning_percent — return on investment)
  - İKO → **IMP** (no-vig implied probability)
  - KZ/KY → **W/L** (win/loss counts)

  TS field names (`dso`, `vbet`, `iko`) intentionally NOT renamed — coupons
  in AsyncStorage carry these keys; renaming would force a migration.
- **Theme-aware logo**: `assets/images/logo.png` (white) for dark theme,
  `assets/images/logo-black.png` for light. AppBrand swaps via
  `useEffectiveScheme`.
- **Run locally**:
  ```
  cd mobile
  NODE_OPTIONS="--max-old-space-size=8192" \
    EXPO_PUBLIC_API_BASE_URL="http://<LAN_IP>:28333" \
    REACT_NATIVE_PACKAGER_HOSTNAME="<LAN_IP>" \
    npx expo start --port 19002 --host lan
  ```
  The `NODE_OPTIONS` heap bump is necessary — bundle is ~16 MB and Metro
  hits V8 OOM (exit 134) on stock 4 GB heap.

## Conventions

- **i18n first**: any user-facing string goes through `t()`. Keys live in
  `mobile/src/lib/i18n/locales/{en,tr}.json`. Don't hardcode Turkish in
  `.tsx` — keep the codebase locale-neutral.
- **Turkish-aware string handling**: `toLocaleLowerCase('tr-TR')` for
  search matching so `İstanbul`/`ISTANBUL`/`istanbul` collapse correctly.
- **Tip-as-tap**: in OddsRatesCard / RateMatchCard the tip cell (not the
  odd cell) is the coupon add/remove tap target. The odd cell is display-
  only; when `oddsHidden` is on the entire ORAN column is removed.
- **No CouponBadge in (tabs)**: detail screens are root-level routes, so
  they sit above (tabs) in the stack. The badge is rendered in (tabs)
  layout — it intentionally doesn't show on detail pages.

## Running the full stack (Windows)

1. `docker start preodds-postgres` (port 15432)
2. WebAPI on `<LAN_IP>:28333`
3. 3 workers on health 8081/8082/8083
4. Mobile Metro on `<LAN_IP>:19002` with `--host lan`

LAN IP changes between sessions — `Get-NetIPAddress -InterfaceAlias 'Wi-Fi'`
gets the current one. Windows Firewall sometimes blocks 19002/28333 on
fresh networks; `New-NetFirewallRule` once per port.

## Pending business decisions

- **SportMonks subscription**: user planned to purchase Starter (€29) +
  Odds & Predictions (€15) + 3× Extra Leagues (€12) ≈ €56/month. Once
  active, update worker `AllowedLeagueIds` config for the chosen 5–8
  leagues. Recommended top-5: TR Süper Lig + EN PL + ES La Liga + IT
  Serie A + DE Bundesliga.
