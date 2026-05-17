import { useQuery } from '@tanstack/react-query';

import { listFixtures, type ListFixturesParams } from '@/src/api/fixtures';
import { getStateBucket } from '@/src/lib/fixtureState';

const LIVE_POLL_MS = 30 * 1000;

interface UseFixturesOptions {
  refetchIntervalMs?: number;
}

/**
 * Fixture list query. By default the hook polls every 30s WHEN any
 * fixture in the result set is live — that's the backstop in case
 * SignalR drops. Caller can override with `refetchIntervalMs` (e.g. to
 * force polling on a screen that needs aggressive freshness even when
 * nothing's live yet).
 *
 * Why backstop polling matters: the SignalR client has a finite retry
 * budget (0,1,2,5,10,20s) and stops trying after 6 failures. Without a
 * second mechanism, a 30-second network blip leaves live minutes
 * frozen until the user manually refreshes — the bug Ethem saw where
 * the list sat on minute 24 while the match was actually at 39.
 */
export function useFixtures(
  params: ListFixturesParams,
  options: UseFixturesOptions = {},
) {
  return useQuery({
    queryKey: ['fixtures', params],
    queryFn: () => listFixtures(params),
    refetchInterval: (query) => {
      if (options.refetchIntervalMs != null) return options.refetchIntervalMs;
      const items = query.state.data?.items ?? [];
      const anyLive = items.some(
        (f) => getStateBucket(f.state_id) === 'live',
      );
      return anyLive ? LIVE_POLL_MS : false;
    },
    // Keep the cache "fresh" for the polling interval so SignalR's
    // invalidateQueries doesn't double-trigger work when the polling
    // tick is about to fire anyway.
    staleTime: LIVE_POLL_MS / 2,
  });
}
