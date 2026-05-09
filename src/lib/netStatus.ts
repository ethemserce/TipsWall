import { useEffect, useState } from 'react';

/**
 * Network reachability bus. Wraps @react-native-community/netinfo with the
 * same lazy-load pattern as monitoring/secure-store: when the package is
 * missing or fails to initialise we fall back to "online" so the app
 * doesn't gratuitously alarm users.
 *
 * Status mirrors the NetInfo state machine but trimmed to what the UI
 * cares about: `online` = traffic should flow, `offline` = banner up.
 */

export type NetStatus = 'online' | 'offline' | 'unknown';

interface NetInfoLike {
  addEventListener(listener: (state: { isConnected: boolean | null; isInternetReachable: boolean | null }) => void): () => void;
  fetch(): Promise<{ isConnected: boolean | null; isInternetReachable: boolean | null }>;
}

let netInfo: NetInfoLike | null = null;
try {
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  netInfo = require('@react-native-community/netinfo').default as NetInfoLike;
} catch {
  netInfo = null;
}

let status: NetStatus = 'unknown';
const listeners = new Set<(s: NetStatus) => void>();

function setStatus(next: NetStatus): void {
  if (status === next) return;
  status = next;
  for (const l of listeners) l(next);
}

function decide(state: { isConnected: boolean | null; isInternetReachable: boolean | null }): NetStatus {
  // isInternetReachable is the more useful signal — a phone on Wi-Fi with no
  // gateway reports `isConnected: true` but `isInternetReachable: false`.
  // When NetInfo doesn't know yet (null), preserve current status instead
  // of flapping to "offline".
  if (state.isInternetReachable === false || state.isConnected === false) return 'offline';
  if (state.isInternetReachable === true && state.isConnected === true) return 'online';
  return status === 'unknown' ? 'online' : status;
}

if (netInfo) {
  netInfo
    .fetch()
    .then((s) => setStatus(decide(s)))
    .catch(() => {
      /* swallow */
    });
  netInfo.addEventListener((s) => setStatus(decide(s)));
} else {
  // No NetInfo at runtime — assume online so the UI doesn't false-alarm.
  setStatus('online');
}

export function getNetStatus(): NetStatus {
  return status;
}

export function subscribeNetStatus(listener: (s: NetStatus) => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function useNetStatus(): NetStatus {
  const [snapshot, setSnapshot] = useState<NetStatus>(status);
  useEffect(() => subscribeNetStatus(setSnapshot), []);
  return snapshot;
}
