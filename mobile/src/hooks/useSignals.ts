import { useQuery } from '@tanstack/react-query';

import { listSignals, type SignalQueryParams } from '@/src/api/signals';

export function useSignals(params: SignalQueryParams = {}) {
  return useQuery({
    queryKey: ['signals', params],
    queryFn: () => listSignals(params),
    // The signal payload itself rarely changes (analytics runs nightly)
    // but each row carries fixture-level state — live_minute, scores —
    // that has to stay in sync with the home tab. 30s staleTime lets a
    // focus/return refetch, the 60s polling is the safety net for users
    // who keep the screen open during a live match, and useLiveTicker
    // invalidates this same key on SignalR pushes for near-instant
    // updates whenever a worker tick lands.
    staleTime: 30 * 1000,
    refetchInterval: 60 * 1000,
    refetchIntervalInBackground: false,
  });
}
