/**
 * Crash + error reporting wrapper. Sentry is initialised lazily so the app
 * still boots cleanly when `@sentry/react-native` isn't installed yet (e.g.
 * fresh checkout before npm install) or when no DSN is configured (dev).
 *
 * DSN is read from EXPO_PUBLIC_SENTRY_DSN. Sentry instance lives module-level
 * so callers don't need to re-discover it.
 */

interface SentryLike {
  init(options: Record<string, unknown>): void;
  captureException(err: unknown, hint?: Record<string, unknown>): void;
  captureMessage(msg: string, level?: string): void;
  setUser(user: { id?: string; username?: string } | null): void;
  setTag(key: string, value: string): void;
}

let sentry: SentryLike | null = null;
let initialised = false;

export function initMonitoring(): void {
  if (initialised) return;
  initialised = true;

  const dsn = process.env.EXPO_PUBLIC_SENTRY_DSN?.trim();
  if (!dsn) return;

  try {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const mod = require('@sentry/react-native') as SentryLike;
    mod.init({
      dsn,
      // Trace 10% of transactions in prod; nothing in dev to keep noise low.
      tracesSampleRate: __DEV__ ? 0 : 0.1,
      // We don't have a release pipeline yet — once EAS Update is wired,
      // pass the EAS update id here so each rollout gets its own bucket.
      enableAutoSessionTracking: true,
      enableNativeCrashHandling: true,
    });
    sentry = mod;
  } catch {
    sentry = null;
  }
}

export function captureException(err: unknown, context?: Record<string, unknown>): void {
  if (!sentry) {
    if (__DEV__) console.warn('[monitoring] uncaught:', err, context);
    return;
  }
  sentry.captureException(err, context ? { extra: context } : undefined);
}

export function captureMessage(msg: string, level: 'info' | 'warning' | 'error' = 'info'): void {
  if (!sentry) {
    if (__DEV__) console.log(`[monitoring/${level}]`, msg);
    return;
  }
  sentry.captureMessage(msg, level);
}

export function identifyUser(userId: string | null, username?: string): void {
  if (!sentry) return;
  if (userId == null) sentry.setUser(null);
  else sentry.setUser({ id: userId, username });
}

export function tagEnvironment(key: string, value: string): void {
  if (!sentry) return;
  sentry.setTag(key, value);
}
