/**
 * Firebase Analytics adapter — the only file that imports
 * `@react-native-firebase/*`. Wrapped in a try/require so removing the
 * package (or running on a platform where it can't load — Expo Go,
 * web) degrades to no-op instead of crashing the bundle.
 *
 * Removal recipe (matches `mobile/FIREBASE.md`):
 *   1. Delete this file
 *   2. Make `./index.ts` a no-op shim (remove the `import * from
 *      './firebase'` line and stub the exports)
 *   3. `npm uninstall @react-native-firebase/app @react-native-firebase/analytics`
 *   4. Drop the `@react-native-firebase/app` plugin entry from `app.json`
 */

type AnalyticsApi = {
  logEvent: (name: string, params?: Record<string, unknown>) => Promise<void>;
  logScreenView: (params: { screen_name: string; screen_class?: string }) => Promise<void>;
  setUserId: (id: string | null) => Promise<void>;
  setUserProperty: (name: string, value: string | null) => Promise<void>;
  setAnalyticsCollectionEnabled: (enabled: boolean) => Promise<void>;
};

let _analytics: AnalyticsApi | null = null;
let _loadAttempted = false;

function loadFirebase(): AnalyticsApi | null {
  if (_loadAttempted) return _analytics;
  _loadAttempted = true;
  try {
    // v22+ modular API. require() so a missing package fails softly at
    // runtime rather than breaking the whole bundle at import time.
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const mod = require('@react-native-firebase/analytics');
    const {
      getAnalytics,
      logEvent,
      logScreenView,
      setUserId,
      setUserProperty,
      setAnalyticsCollectionEnabled,
    } = mod;
    const instance = getAnalytics();

    _analytics = {
      async logEvent(name, params) {
        await logEvent(instance, name, params);
      },
      async logScreenView(params) {
        await logScreenView(instance, params);
      },
      async setUserId(id) {
        await setUserId(instance, id);
      },
      async setUserProperty(name, value) {
        await setUserProperty(instance, name, value);
      },
      async setAnalyticsCollectionEnabled(enabled) {
        await setAnalyticsCollectionEnabled(instance, enabled);
      },
    };
  } catch {
    // Package missing OR native module not linked (Expo Go, web, etc.).
    // Wrapper stays as no-op.
    _analytics = null;
  }
  return _analytics;
}

async function safe<T>(fn: () => Promise<T> | T): Promise<void> {
  try {
    await fn();
  } catch {
    // Never throw from analytics.
  }
}

export async function logEvent(name: string, params?: Record<string, unknown>): Promise<void> {
  const a = loadFirebase();
  if (!a) return;
  await safe(() => a.logEvent(name, params));
}

export async function logScreenView(screenName: string, screenClass?: string): Promise<void> {
  const a = loadFirebase();
  if (!a) return;
  await safe(() => a.logScreenView({ screen_name: screenName, screen_class: screenClass ?? screenName }));
}

export async function setUserId(id: string | null): Promise<void> {
  const a = loadFirebase();
  if (!a) return;
  await safe(() => a.setUserId(id));
}

export async function setUserProperty(name: string, value: string | null): Promise<void> {
  const a = loadFirebase();
  if (!a) return;
  await safe(() => a.setUserProperty(name, value));
}

export async function setCollectionEnabled(enabled: boolean): Promise<void> {
  const a = loadFirebase();
  if (!a) return;
  await safe(() => a.setAnalyticsCollectionEnabled(enabled));
}

export function isAvailable(): boolean {
  return loadFirebase() !== null;
}
