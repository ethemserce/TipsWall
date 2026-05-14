# Firebase Analytics — TipsWall mobile

This document is the operator manual for the `@react-native-firebase/*`
integration on the mobile app. Read it whenever:

- you need to wire a **fresh environment** (new Firebase project, new dev box),
- you want to **rotate** the credentials,
- you decide to **remove** Firebase entirely (3-step recipe at the bottom).

The integration is intentionally thin: a single wrapper at
`src/lib/analytics/` brokers every call to Firebase, a KVKK-compliant
consent gate guards each event, and the only file in the codebase that
imports `@react-native-firebase/analytics` is `src/lib/analytics/firebase.ts`.

---

## What gets tracked

- **Screen views** — every expo-router path change emits `screen_view`
  via `useTrackScreens` mounted in `app/_layout.tsx`.
- **Custom events** —
  - `login` (with `method: 'password' | 'apple' | 'google'`)
  - `sign_up` (`method: 'password'`)
  - `add_to_tip_list` (fixture / market / outcome / current draft size)

Adding new events: import `analytics` from `@/src/lib/analytics`, call
`analytics.track('event_name', { ...params })`. Never import
`@react-native-firebase/*` directly anywhere else — the wrapper is the
removability seam.

## Consent flow

- First launch: `consentStore.getState() === 'pending'`. The
  `AnalyticsConsentBanner` renders at the bottom of every screen until
  the user picks.
- Accept → `consentStore` flips to `'granted'`, persisted in
  AsyncStorage, Firebase `setAnalyticsCollectionEnabled(true)` fires.
- Deny → `'denied'` persisted, collection disabled, any previously-set
  user id cleared.
- Settings → "Kullanım verisi paylaş" toggle flips the state without
  re-showing the banner.
- Until `'granted'`, every `analytics.*` call is a silent no-op. Safe to
  sprinkle calls anywhere; consent gating is centralised in the wrapper.

## First-time setup (per environment)

### 1. Firebase Console

- https://console.firebase.google.com → **Add project** → `TipsWall`
  (or `TipsWall Staging` for non-prod environments).
- Enable Google Analytics during project creation.
- **Add Android app**: package name `com.tipswall.app` (must match
  `app.json` → `android.package`). Download `google-services.json`.
- **Add iOS app**: bundle id `com.tipswall.app` (must match
  `app.json` → `ios.bundleIdentifier`). Download `GoogleService-Info.plist`.

### 2. Drop the files into the repo

```
mobile/google-services.json          # Android — gitignored
mobile/GoogleService-Info.plist      # iOS     — gitignored
```

Both are in `.gitignore`. For CI / EAS:

- **EAS Secrets** (recommended): upload each file as a project-scoped
  secret (`EAS_GOOGLE_SERVICES_ANDROID`, `EAS_GOOGLE_SERVICES_IOS`) and
  reference them in `eas.json` via `env`/file-resolution helpers.
- **CI-side `git-crypt`**: works but adds friction; prefer EAS Secrets.

### 3. Verify the build picks them up

```
cd mobile
npx expo prebuild --clean   # regenerates /ios and /android with config plugins
```

The config plugin contributed by `@react-native-firebase/app` injects
the Google Services file references. Validate by opening the generated
`android/app/google-services.json` and `ios/<App>/GoogleService-Info.plist`.

### 4. Run a build

`@react-native-firebase/*` requires a native module — **Expo Go cannot
load it**. Use a development build (`eas build --profile development`)
or the existing `preview` profile to test.

## Removal recipe (3 steps)

If you decide Firebase Analytics isn't the right tool:

1. **Make `firebase.ts` a no-op shim** — replace the file content with
   `export {}` stubs that match the same exported names, or delete it
   and stub the same exports inline in `src/lib/analytics/index.ts`.
   Every call site keeps compiling because the wrapper's public API
   doesn't change.

2. **Uninstall the packages**:
   ```
   cd mobile
   npm uninstall @react-native-firebase/app @react-native-firebase/analytics
   ```
   `expo-build-properties` can stay (other plugins may use it) or go.

3. **Drop the plugin entry** from `mobile/app.json`:
   - remove `"@react-native-firebase/app"` from the `plugins` array,
   - remove the `googleServicesFile` entries from `ios` and `android`.

After step 3, `npx expo prebuild --clean` re-emits clean native projects
without Firebase. The KVKK consent UI can be removed too if it's no
longer relevant, but leaving it in place costs nothing — it tracks a
boolean that nobody reads.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `Default FirebaseApp is not initialized` at startup | `google-services.json` / `GoogleService-Info.plist` missing or invalid — check the path on disk + `app.json` references |
| Bundle build fails on iOS with `non-modular header` | `useFrameworks: static` plugin not applied — verify `expo-build-properties` plugin entry is present and has `ios.useFrameworks = "static"` |
| Events never appear in Firebase Console | Confirm the user accepted the consent banner; check Settings toggle; remember DebugView is the live feed (Reports tab is delayed ~24 h) |
| Crash on opening a screen after upgrading expo SDK | Native Firebase pod versions can lag the JS package — run `npx expo prebuild --clean && npx pod-install` |
