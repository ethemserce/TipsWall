import AsyncStorage from '@react-native-async-storage/async-storage';

import {
  getCuratedMarkets,
  getMarketPreferences,
  putMarketPreferences,
} from '@/src/api/marketPreferences';
import { getAuthSnapshot, getTier } from '@/src/lib/auth/authStore';

/**
 * Subscriber-pattern store for the user's favourite markets (used by
 * the Analiz screen and the maç detay Analiz tab to narrow the rows
 * shown). Mirrors the rest of the app's snapshot+listener pattern
 * (see settingsStore / coupons.store).
 *
 * Persistence priority:
 *   1. Backend (when logged in) — survives reinstall
 *   2. AsyncStorage — local fallback, used while guest, also lets the
 *      app boot before the round-trip lands
 *
 * Defaults: at first launch with no saved choice, the store auto-fills
 * with the tier-curated set (guest 3, free 10, premium 30). The user
 * is then free to swap markets in Settings within the tier cap. An
 * empty list never persists — we always have at least the tier's
 * default set selected so Analiz/Maç-detay are never empty.
 */

const STORAGE_KEY = 'tipswall.market-prefs.v2';
const HAS_USER_CHOICE_KEY = 'tipswall.market-prefs.user-choice.v1';

interface State {
  marketIds: number[];
  cap: number;
  tier: 'guest' | 'free' | 'premium';
  hydrated: boolean;
  // Tracks whether the current selection came from a deliberate user
  // pick (Settings save) vs. an auto-filled default. When false the
  // store re-runs the default fill if the tier changes (e.g. user
  // logs in or upgrades to premium).
  hasUserChoice: boolean;
}

type Listener = () => void;

let state: State = {
  marketIds: [],
  cap: 3,
  tier: 'guest',
  hydrated: false,
  hasUserChoice: false,
};
const listeners = new Set<Listener>();

function emit() {
  for (const l of listeners) l();
}

async function persistLocal() {
  try {
    await AsyncStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({
        marketIds: state.marketIds,
        cap: state.cap,
        tier: state.tier,
      }),
    );
    await AsyncStorage.setItem(
      HAS_USER_CHOICE_KEY,
      state.hasUserChoice ? '1' : '0',
    );
  } catch {
    // Fire-and-forget; in-memory state already reflects the change.
  }
}

function tierFromAuth(): 'guest' | 'free' | 'premium' {
  // authStore.getTier() reads the JWT 'tier' claim and falls back to
  // 'guest' when there's no access token.
  return getTier();
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
    const tier = tierFromAuth();
    try {
      const raw = await AsyncStorage.getItem(STORAGE_KEY);
      const choiceRaw = await AsyncStorage.getItem(HAS_USER_CHOICE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as {
          marketIds?: number[];
          cap?: number;
          tier?: 'guest' | 'free' | 'premium';
        };
        state = {
          marketIds: Array.isArray(parsed.marketIds) ? parsed.marketIds.filter((n) => Number.isInteger(n)) : [],
          cap: typeof parsed.cap === 'number' ? parsed.cap : 3,
          tier: parsed.tier ?? tier,
          hydrated: true,
          hasUserChoice: choiceRaw === '1',
        };
      } else {
        state = { marketIds: [], cap: 3, tier, hydrated: true, hasUserChoice: false };
      }
    } catch {
      state = { marketIds: [], cap: 3, tier, hydrated: true, hasUserChoice: false };
    }
    emit();

    // If the user is logged in, pull the server-authoritative copy.
    // Otherwise hit the anonymous /markets/curated endpoint for the
    // tier defaults.
    if (getAuthSnapshot().accessToken) {
      try {
        const remote = await getMarketPreferences();
        // If the server has 0 saved markets AND the local cache had
        // no user-driven save either, fall through to the tier defaults
        // and persist them upstream so subsequent loads are instant.
        if (remote.market_ids.length === 0 && !state.hasUserChoice) {
          await applyDefaults(remote.tier, remote.defaults, remote.cap);
        } else {
          state = {
            marketIds: [...remote.market_ids],
            cap: remote.cap,
            tier: remote.tier,
            hydrated: true,
            hasUserChoice: remote.market_ids.length > 0,
          };
          emit();
          await persistLocal();
        }
      } catch {
        // Stay on local cache.
      }
    } else if (!state.hasUserChoice && state.marketIds.length === 0) {
      // Guest first launch: pull curated defaults so the picker isn't
      // empty.
      try {
        const curated = await getCuratedMarkets('guest');
        state = {
          marketIds: [...curated.defaults],
          cap: curated.cap,
          tier: 'guest',
          hydrated: true,
          hasUserChoice: false,
        };
        emit();
        await persistLocal();
      } catch {
        // Network down — keep the empty list, picker will look empty.
      }
    }
  },

  /**
   * Replace the current selection. Logged-in users push the change
   * upstream so it survives reinstall; guests stay local-only.
   * Marks `hasUserChoice = true` so the auto-fill never overrides
   * this list later.
   */
  async replace(marketIds: number[]): Promise<{ ok: boolean; error?: string }> {
    const deduped = Array.from(new Set(marketIds));
    if (deduped.length > state.cap) {
      return { ok: false, error: 'over-cap' };
    }
    state = { ...state, marketIds: deduped, hasUserChoice: true };
    emit();
    await persistLocal();

    if (getAuthSnapshot().accessToken) {
      try {
        const remote = await putMarketPreferences(deduped);
        state = {
          marketIds: [...remote.market_ids],
          cap: remote.cap,
          tier: remote.tier,
          hydrated: true,
          hasUserChoice: true,
        };
        emit();
        await persistLocal();
      } catch {
        return { ok: false, error: 'network' };
      }
    }
    return { ok: true };
  },

  /**
   * Re-run the tier-aware default fill (used after login / tier
   * change). Honours `hasUserChoice` — never overrides a deliberate
   * save.
   */
  async refreshDefaultsForTier(): Promise<void> {
    if (state.hasUserChoice) return;
    const tier = tierFromAuth();
    try {
      const curated = await getCuratedMarkets(tier);
      await applyDefaults(curated.tier, curated.defaults, curated.cap);
    } catch {
      // No-op on network failure.
    }
  },
};

async function applyDefaults(
  tier: 'guest' | 'free' | 'premium',
  defaults: readonly number[],
  cap: number,
) {
  state = {
    marketIds: [...defaults],
    cap,
    tier,
    hydrated: true,
    hasUserChoice: false,
  };
  emit();
  await persistLocal();
  // If logged in, push the auto-filled selection upstream so the
  // server reflects what the user sees.
  if (getAuthSnapshot().accessToken) {
    try {
      await putMarketPreferences(state.marketIds);
    } catch {
      // Stay on local copy; sync on next manual save.
    }
  }
}

export function getMarketPreferenceIds(): number[] {
  return state.marketIds;
}
