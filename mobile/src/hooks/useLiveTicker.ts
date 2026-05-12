import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';

import { ensureLiveConnected } from '@/src/lib/liveConnection';

interface FixtureUpdatedPayload {
  fixture_id: number;
  source: string;
  payload: unknown;
  broadcast_at: string;
}

/**
 * Subscribes to the global SignalR 'live-ticker' group so the home screen
 * (or any list view of fixtures) refreshes whenever ANY fixture moves.
 * Throttle is handled implicitly by TanStack Query — invalidate just marks
 * the cache stale; refetch only fires when the screen is visible.
 */
export function useLiveTicker(enabled = true) {
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!enabled) return undefined;
    let mounted = true;
    let unsub: (() => void) | undefined;

    const handler = (envelope: FixtureUpdatedPayload) => {
      if (!envelope) return;
      // Refresh anything keyed by 'fixtures' (list endpoints).
      queryClient.invalidateQueries({ queryKey: ['fixtures'] });
      // Signals (analysis page) embed fixture-level state — live_minute,
      // scores — in each row, so they need the same nudge or the user
      // sees stale minutes after a few seconds even though the home tab
      // shows the latest. Invalidate only refetches active observers.
      queryClient.invalidateQueries({ queryKey: ['signals'] });
      // Also nudge the per-fixture detail cache so any list view that reads
      // fixture-level state (live score / kickoff state) — e.g. CouponsScreen
      // — picks up the change. Invalidate is cheap when the query isn't
      // mounted; React Query only refetches active observers.
      if (envelope.fixture_id) {
        queryClient.invalidateQueries({
          queryKey: ['fixture', envelope.fixture_id],
        });
      }
    };

    (async () => {
      try {
        const conn = await ensureLiveConnected();
        if (!mounted) return;
        conn.on('FixtureUpdated', handler);
        await conn.invoke('JoinLiveTicker');
        unsub = () => {
          conn.off('FixtureUpdated', handler);
          conn
            .invoke('LeaveLiveTicker')
            .catch(() => {
              /* leaving on a stale connection is harmless */
            });
        };
      } catch {
        // Network/auth failure — fall back to polling silently.
      }
    })();

    return () => {
      mounted = false;
      unsub?.();
    };
  }, [enabled, queryClient]);
}
