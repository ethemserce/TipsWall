import AsyncStorage from '@react-native-async-storage/async-storage';

import {
  getMarketPreferences,
  putMarketPreferences,
} from '@/src/api/marketPreferences';
import { getAuthSnapshot } from '@/src/lib/auth/authStore';

/**
 * Subscriber-pattern store for the user's preferred markets (used by
 * the Analiz screen and the maç detay Analiz tab to narrow the rows
 * shown). Mirrors the rest of the app's snapshot+listener pattern
 * (see settingsStore / coupons.store).
 *
 * Persistence priority:
 *   1. Backend (when logged in) — survives reinstall
 *   2. AsyncStorage — local fallback, used while guest, also lets the
 *      app boot before the round-trip lands
 *
 * An empty selection means "no filter" — show everything the default
 * available_in_standard+active filter returns. The cap (free 5,
 * premium 30) lives server-side; the local store mirrors the latest
 * value so the picker can disable beyond-cap toggles immediately.
 */

const STORAGE_KEY = 'tipswall.market-prefs.v1';
const DEFAULT_FREE_CAP = 5;

interface State {
  marketIds: number[];
  cap: number;
  hydrated: boolean;
}

type Listener = () => void;

let state: State = { marketIds: [], cap: DEFAULT_FREE_CAP, hydrated: false };
const listeners = new Set<Listener>();

function emit() {
  for (const l of listeners) l();
}

async function persistLocal() {
  try {
    await AsyncStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ marketIds: state.marketIds, cap: state.cap }),
    );
  } catch {
    // Fire-and-forget; in-memory state already reflects the change.
  }
}

export const marketPreferencesStore = {
  getState(): State {
    return state;
  },

  subscribe(l: Listener): () => void {
    listeners.add(l);
    return () => {
      listeners.delete(l);
    };
  },

  async hydrate(): Promise<void> {
    if (state.hydrated) return;
    try {
      const raw = await AsyncStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as { marketIds?: number[]; cap?: number };
        state = {
          marketIds: Array.isArray(parsed.marketIds) ? parsed.marketIds.filter((n) => Number.isInteger(n)) : [],
          cap: typeof parsed.cap === 'number' ? parsed.cap : DEFAULT_FREE_CAP,
          hydrated: true,
        };
      } else {
        state = { ...state, hydrated: true };
      }
    } catch {
      state = { ...state, hydrated: true };
    }
    emit();

    // If the user is logged in, the server's copy is authoritative —
    // overwrite the local cache once the round-trip lands. The fetch
    // is best-effort: a network failure keeps the local snapshot.
    if (getAuthSnapshot().accessToken) {
      try {
        const remote = await getMarketPreferences();
        state = { marketIds: [...remote.market_ids], cap: remote.cap, hydrated: true };
        emit();
        await persistLocal();
      } catch {
        // Stay on the local cache.
      }
    }
  },

  /**
   * Replace the current selection. Logged-in users push the change
   * upstream so it survives reinstall; guests stay local-only.
   */
  async replace(marketIds: number[]): Promise<{ ok: boolean; error?: string }> {
    const deduped = Array.from(new Set(marketIds));
    if (deduped.length > state.cap) {
      return { ok: false, error: 'over-cap' };
    }
    state = { ...state, marketIds: deduped };
    emit();
    await persistLocal();

    if (getAuthSnapshot().accessToken) {
      try {
        const remote = await putMarketPreferences(deduped);
        state = { marketIds: [...remote.market_ids], cap: remote.cap, hydrated: true };
        emit();
        await persistLocal();
      } catch (e) {
        // Roll back? For now keep the local change — the next hydrate
        // will reconcile. A failed PUT is more often a transient
        // network blip than a permanent rejection.
        return { ok: false, error: 'network' };
      }
    }
    return { ok: true };
  },
};

export function getMarketPreferenceIds(): number[] {
  return state.marketIds;
}
