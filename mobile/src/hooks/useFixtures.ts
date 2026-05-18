import { useQuery } from '@tanstack/react-query';

import { listFixtures, type ListFixturesParams } from '@/src/api/fixtures';
import { getStateBucket } from '@/src/lib/fixtureState';

const LIVE_POLL_MS = 30 * 1000;
// Earliest the kickoff backstop arms (before starting_at) and how long
// it stays armed after kickoff. Five minutes ahead catches network
// drift; ten minutes after covers a slow backend transition (live tick
// runs every 30s + SportMonks API has its own ingestion latency).
const KICKOFF_LEAD_MS = 5 * 60 * 1000;
const KICKOFF_GRACE_MS = 10 * 60 * 1000;

interface UseFixturesOptions {
  refetchIntervalMs?: number;
}

/**
 * Fixture list query. Polls every 30s under either of two conditions:
 *
 *   1. Some fixture in the result set is currently `live` — the
 *      historical backstop, keeps live minutes ticking when SignalR
 *      drops.
 *   2. Some `upcoming` fixture's kickoff time falls within the
 *      [-5 min, +10 min] window relative to now — covers the
 *      previously-silent state transition. Ethem reported watching
 *      a 20:00 slate from 19:55 to 20:05: matches stayed "upcoming"
 *      in the UI until he closed/reopened the app. Cause: anyLive
 *      was false (nothing had transitioned yet from the local point
 *      of view), so refetchInterval was false, so the only path to
 *      see the state flip was a SignalR event. When SignalR misses
 *      one (silent drop, scheduler hiccup, backend tick delay) the
 *      UI sat on stale data indefinitely.
 *
 * Why backstop polling matters more broadly: the SignalR client has
 * a finite retry budget (0,1,2,5,10,20s) and stops trying after
 * 6 failures. Without a second mechanism, a 30-second network blip
 * leaves live minutes frozen until the user manually refreshes —
 * the bug Ethem first saw where the list sat on minute 24 while
 * the match was actually at 39.
 *
 * Caller can override the polling cadence entirely with
 * `refetchIntervalMs` for screens that need aggressive freshness even
 * when nothing's about to kick off.
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
      const now = Date.now();
      const anyLive = items.some(
        (f) => getStateBucket(f.state_id) === 'live',
      );
      const anyKickoffImminent = items.some((f) => {
        if (getStateBucket(f.state_id) !== 'upcoming') return false;
        if (!f.starting_at) return false;
        const startsAt = new Date(f.starting_at).getTime();
        if (Number.isNaN(startsAt)) return false;
        return startsAt - KICKOFF_LEAD_MS <= now
          && now <= startsAt + KICKOFF_GRACE_MS;
      });
      return (anyLive || anyKickoffImminent) ? LIVE_POLL_MS : false;
    },
    // Keep the cache "fresh" for the polling interval so SignalR's
    // invalidateQueries doesn't double-trigger work when the polling
    // tick is about to fire anyway.
    staleTime: LIVE_POLL_MS / 2,
  });
}
