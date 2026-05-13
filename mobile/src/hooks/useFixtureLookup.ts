import { useQueries } from '@tanstack/react-query';
import { useMemo } from 'react';

import { getFixture } from '@/src/api/fixtures';
import type { FixtureDetail } from '@/src/types/fixtureDetail';

// 30 s matches the live-tier worker tick; useLiveTicker also invalidates
// 'fixture' keys on every SignalR push so the analysis / coupon screens
// see fresh live_minute without the user having to navigate away. 1 h
// was the old value; it left stale minutes (e.g. "45'") on screen for an
// entire match.
const STALE_MS = 30 * 1000;
const GC_MS = 60 * 60 * 1000;

/**
 * Batch-fetch fixture details for the supplied ids and expose a lookup map
 * keyed by fixture id.
 */
export function useFixtureLookup(ids: number[]) {
  const uniqueIds = useMemo(
    () =>
      Array.from(new Set(ids.filter((id) => Number.isFinite(id) && id > 0))).sort(
        (a, b) => a - b,
      ),
    [ids],
  );

  const queries = useQueries({
    queries: uniqueIds.map((id) => ({
      queryKey: ['fixture', id] as const,
      queryFn: () => getFixture(id),
      staleTime: STALE_MS,
      gcTime: GC_MS,
      retry: 1,
      // Periodic refresh while the screen is visible — SignalR is the
      // primary live channel but it falls back to polling when the
      // websocket is unhealthy. 60 s matches the home-tab fixtures hook
      // and the worker's live tier tick.
      refetchInterval: 60 * 1000,
      refetchIntervalInBackground: false,
    })),
  });

  const lookup = useMemo(() => {
    const map = new Map<number, FixtureDetail>();
    queries.forEach((q, idx) => {
      if (q.data) map.set(uniqueIds[idx], q.data);
    });
    return map;
  }, [queries, uniqueIds]);

  return { lookup, isLoading: queries.some((q) => q.isLoading) };
}
