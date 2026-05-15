import { useEffect, useState } from 'react';

import {
  clearAllTokens,
  readAccessToken,
  readRefreshToken,
  writeAccessToken,
  writeRefreshToken,
} from '@/src/lib/auth/tokenStorage';
import { clearAllCoupons } from '@/src/lib/coupons/store';

/**
 * Module-level auth state. Mirrors the coupon-store pattern: a single shared
 * snapshot, listener-based subscriptions, no React context, no extra deps.
 *
 * Tokens are persisted to disk (AsyncStorage for access, expo-secure-store
 * for refresh). The in-memory copy is the hot path — request/response
 * interceptors read from here without touching async storage on every call.
 */

export interface AuthSnapshot {
  accessToken: string | null;
  refreshToken: string | null;
  hydrated: boolean;
}

type Listener = () => void;

let state: AuthSnapshot = {
  accessToken: null,
  refreshToken: null,
  hydrated: false,
};
const listeners = new Set<Listener>();

function emit() {
  for (const l of listeners) l();
}

async function hydrate(): Promise<void> {
  if (state.hydrated) return;
  try {
    const [access, refresh] = await Promise.all([
      readAccessToken(),
      readRefreshToken(),
    ]);
    state = { accessToken: access, refreshToken: refresh, hydrated: true };
  } catch {
    state = { accessToken: null, refreshToken: null, hydrated: true };
  }
  emit();
}

// Kick off hydration once at module load — same pattern as coupon store.
hydrate();

export function getAuthSnapshot(): AuthSnapshot {
  return state;
}

export function subscribeAuth(l: Listener): () => void {
  listeners.add(l);
  return () => {
    listeners.delete(l);
  };
}

export async function setTokens(
  accessToken: string,
  refreshToken: string,
): Promise<void> {
  state = { ...state, accessToken, refreshToken, hydrated: true };
  emit();
  // Persist asynchronously — we don't block the call site on disk I/O.
  await Promise.all([
    writeAccessToken(accessToken),
    writeRefreshToken(refreshToken),
  ]);
}

export async function setAccessToken(accessToken: string): Promise<void> {
  state = { ...state, accessToken };
  emit();
  await writeAccessToken(accessToken);
}

export async function clearTokens(): Promise<void> {
  state = { ...state, accessToken: null, refreshToken: null };
  emit();
  // Picks on disk belonged to whoever was logged in (or the guest path
  // before login). Wipe them at the same time tokens go away so the
  // next session — whether guest or a different user — starts fresh.
  clearAllCoupons();
  await clearAllTokens();
}

export function isLoggedIn(): boolean {
  return state.accessToken != null && state.refreshToken != null;
}

/**
 * Membership tier of the current user, derived from the JWT payload.
 *  - 'guest'   → no access token (or token can't be decoded)
 *  - 'free'    → registered but not premium
 *  - 'premium' → active paid subscription
 *
 * The backend re-stamps `tier` into every freshly issued access token,
 * so an upgrade only takes effect after the next refresh (≤15 min grace).
 * Routes that need stricter enforcement should also gate server-side.
 */
export type MembershipTier = 'guest' | 'free' | 'premium';

export function getTier(): MembershipTier {
  if (state.accessToken == null) return 'guest';
  const payload = decodeJwtPayload(state.accessToken);
  const claim = typeof payload?.tier === 'string' ? payload.tier : null;
  if (claim === 'premium' || claim === 'free') return claim;
  return 'free';
}

/**
 * Parses the base64url payload chunk of a JWT into a plain object.
 * Doesn't verify the signature — that's the server's job; here we only
 * need to read claims to drive UI. Returns null on any decode failure
 * so callers can fall back to the safest tier (guest / free).
 */
function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const parts = token.split('.');
    if (parts.length < 2) return null;
    const padded = parts[1] + '==='.slice((parts[1].length + 3) % 4);
    const base64 = padded.replace(/-/g, '+').replace(/_/g, '/');
    // atob is available in Hermes / RN runtime; if it ever isn't, the
    // try/catch keeps us at the safe "guest" default.
    const json = globalThis.atob ? globalThis.atob(base64) : null;
    if (json == null) return null;
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

/**
 * React subscription hook — re-renders when auth state changes.
 */
export function useAuth(): AuthSnapshot {
  const [snapshot, setSnapshot] = useState<AuthSnapshot>(state);
  useEffect(() => {
    const unsubscribe = subscribeAuth(() => setSnapshot(getAuthSnapshot()));
    setSnapshot(getAuthSnapshot());
    return unsubscribe;
  }, []);
  return snapshot;
}

/**
 * React subscription hook for the membership tier. Re-renders whenever
 * the access token changes (login, logout, refresh). Components can
 * use this to branch UI: hide ROI gauges for guests, show "Üye Ol"
 * CTAs, suppress AdMob for premium, etc.
 */
export function useTier(): MembershipTier {
  const [tier, setTier] = useState<MembershipTier>(() => getTier());
  useEffect(() => {
    const unsubscribe = subscribeAuth(() => setTier(getTier()));
    setTier(getTier());
    return unsubscribe;
  }, []);
  return tier;
}

/**
 * Has the current user verified their email? Reads the `email_verified`
 * claim from the JWT.
 *  - Guests return `true` (no email to verify; no banner to show)
 *  - Social-signin accounts come back `true` immediately (provider
 *    has already certified the address)
 *  - Email+password accounts return `false` until the user clicks the
 *    link in the verification email AND the token is refreshed
 *    (≤15 min after click, or immediately via a manual refresh).
 *
 * Backend stamps the claim into every freshly issued access token, so
 * a forced refreshAccessToken() right after the user clicks the email
 * link surfaces the flip without waiting for natural rotation.
 */
export function getEmailVerified(): boolean {
  if (state.accessToken == null) return true;
  const payload = decodeJwtPayload(state.accessToken);
  const claim = payload?.email_verified;
  return claim === 'true' || claim === true;
}

export function useEmailVerified(): boolean {
  const [v, setV] = useState<boolean>(() => getEmailVerified());
  useEffect(() => {
    const unsubscribe = subscribeAuth(() => setV(getEmailVerified()));
    setV(getEmailVerified());
    return unsubscribe;
  }, []);
  return v;
}
