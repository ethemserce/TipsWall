import { useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';

import { ensureLiveConnected, getLiveConnection } from '@/src/lib/liveConnection';

interface FixtureUpdatedPayload {
  fixture_id: number;
  source: string;
  payload: unknown;
  broadcast_at: string;
}

/**
 * Joins the SignalR group for the supplied fixture and invalidates the
 * relevant TanStack Query caches whenever the backend pushes an update.
 * The hook is a no-op when fixtureId <= 0.
 */
export function useLiveFixture(fixtureId: number) {
  const queryClient = useQueryClient();

  useEffect(() => {
    if (!fixtureId || fixtureId <= 0) return undefined;
    let mounted = true;
    let unsub: (() => void) | undefined;

    const handler = (envelope: FixtureUpdatedPayload) => {
      if (!envelope || envelope.fixture_id !== fixtureId) return;
      // Hint TanStack Query to refetch any per-fixture data on next access.
      queryClient.invalidateQueries({ queryKey: ['fixture', fixtureId] });
      queryClient.invalidateQueries({ queryKey: ['fixture-events', fixtureId] });
      queryClient.invalidateQueries({ queryKey: ['fixture-statistics', fixtureId] });
      queryClient.invalidateQueries({ queryKey: ['fixture-lineups', fixtureId] });
      queryClient.invalidateQueries({ queryKey: ['fixture-odds-rates', fixtureId] });
    };

    (async () => {
      try {
        const conn = await ensureLiveConnected();
        if (!mounted) return;
        conn.on('FixtureUpdated', handler);
        await conn.invoke('JoinFixture', fixtureId);
        unsub = () => {
          conn.off('FixtureUpdated', handler);
          conn
            .invoke('LeaveFixture', fixtureId)
            .catch(() => {
              /* leaving on a stale connection is harmless */
            });
        };
      } catch {
        // Network/auth failure — degrade gracefully (the polling stays).
      }
    })();

    return () => {
      mounted = false;
      unsub?.();
    };
  }, [fixtureId, queryClient]);

  return getLiveConnection;
}
