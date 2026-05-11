import AsyncStorage from '@react-native-async-storage/async-storage';

/**
 * A stable per-install identifier used by the guest-quota system. Not
 * a hardware ID (Apple/Google don't expose those without ATT), just a
 * UUID minted on first launch and persisted to AsyncStorage. Survives
 * app restarts and OS updates; gets wiped on uninstall — which is the
 * right semantic for a "daily picks-as-a-guest" counter.
 *
 * The value is intentionally NOT secret: it's an anonymous handle the
 * server uses to count `app.guest_pick_quotas.picks_today` against.
 */

const STORAGE_KEY = 'preodds.device.id.v1';

let cached: string | null = null;
let inflight: Promise<string> | null = null;

export async function getDeviceId(): Promise<string> {
  if (cached) return cached;
  // De-dupe parallel callers — first call mints, subsequent calls await
  // the same promise so we never insert two different UUIDs in a race.
  if (inflight) return inflight;
  inflight = (async () => {
    try {
      const existing = await AsyncStorage.getItem(STORAGE_KEY);
      if (existing) {
        cached = existing;
        return existing;
      }
      const next = mintUuid();
      await AsyncStorage.setItem(STORAGE_KEY, next);
      cached = next;
      return next;
    } finally {
      inflight = null;
    }
  })();
  return inflight;
}

/**
 * Synchronous reader for places that can't await — returns null when
 * the id hasn't hydrated yet. Callers that need a guaranteed value
 * should still use getDeviceId().
 */
export function getDeviceIdSync(): string | null {
  return cached;
}

// crypto.randomUUID exists in modern Hermes / iOS / Android RN runtimes.
// Fallback uses Math.random — fine for a guest counter (not security-
// sensitive) and only fires on the unusual case where crypto isn't
// available yet.
function mintUuid(): string {
  const cryptoObj = (globalThis as { crypto?: { randomUUID?: () => string } }).crypto;
  if (cryptoObj?.randomUUID) return cryptoObj.randomUUID();
  const rand = (n: number) =>
    Math.floor(Math.random() * Math.pow(16, n))
      .toString(16)
      .padStart(n, '0');
  return `${rand(8)}-${rand(4)}-4${rand(3)}-${rand(4)}-${rand(12)}`;
}
