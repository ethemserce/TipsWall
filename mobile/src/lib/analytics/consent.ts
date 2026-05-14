import AsyncStorage from '@react-native-async-storage/async-storage';

/**
 * KVKK / GDPR consent state for analytics.
 *
 * Three states:
 *   - 'pending'  — first launch, no decision yet → show banner, do not send events
 *   - 'granted'  — user opted in → send events
 *   - 'denied'   — user opted out → do not send events
 *
 * The store mirrors the rest of the app's subscribe pattern (see
 * `settingsStore.ts`) — module-level snapshot + listener Set, no Zustand.
 * Persisted in AsyncStorage so the choice survives restarts.
 */

export type ConsentState = 'pending' | 'granted' | 'denied';

const STORAGE_KEY = 'tipswall.analytics.consent';

let _state: ConsentState = 'pending';
const _listeners = new Set<() => void>();

function notify() {
  for (const l of _listeners) l();
}

export const consentStore = {
  getState(): ConsentState {
    return _state;
  },

  isGranted(): boolean {
    return _state === 'granted';
  },

  /**
   * Persist + notify. Caller should pass 'granted' or 'denied'; 'pending'
   * is reserved for "user hasn't decided yet" and never set programmatically
   * after the first launch.
   */
  async set(next: Exclude<ConsentState, 'pending'>): Promise<void> {
    _state = next;
    notify();
    try {
      await AsyncStorage.setItem(STORAGE_KEY, next);
    } catch {
      // Storage write is best-effort; in-memory state already reflects choice.
    }
  },

  subscribe(listener: () => void): () => void {
    _listeners.add(listener);
    return () => {
      _listeners.delete(listener);
    };
  },

  /**
   * Called once at app boot — pulls persisted choice into the snapshot.
   * Safe to call multiple times.
   */
  async hydrate(): Promise<void> {
    try {
      const v = await AsyncStorage.getItem(STORAGE_KEY);
      if (v === 'granted' || v === 'denied') {
        _state = v;
        notify();
      }
    } catch {
      // Keep default 'pending' if storage read fails.
    }
  },
};
