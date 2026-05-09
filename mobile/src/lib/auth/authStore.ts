import { useEffect, useState } from 'react';

import {
  clearAllTokens,
  readAccessToken,
  readRefreshToken,
  writeAccessToken,
  writeRefreshToken,
} from '@/src/lib/auth/tokenStorage';

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
  await clearAllTokens();
}

export function isLoggedIn(): boolean {
  return state.accessToken != null && state.refreshToken != null;
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
