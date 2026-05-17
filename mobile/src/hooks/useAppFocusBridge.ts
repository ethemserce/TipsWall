import { focusManager, useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';
import { AppState, type AppStateStatus } from 'react-native';

import {
  ensureLiveConnected,
  getLiveStatus,
} from '@/src/lib/liveConnection';

/**
 * Wires React Native's AppState into TanStack Query's focusManager so
 * `refetchOnWindowFocus: true` actually fires when the user pulls the
 * app back to the foreground. RN doesn't have a window/focus event by
 * default — React Query treats the app as "always focused" until we
 * tell it otherwise.
 *
 * Same handler also nudges the SignalR live connection: its built-in
 * auto-reconnect budget (0,1,2,5,10,20s — six attempts) is easy to
 * exhaust if the phone sleeps for several minutes, so we explicitly
 * call `ensureLiveConnected` on resume to kick a fresh start if the
 * state machine has settled at "disconnected".
 *
 * Mount once at the root layout. Idempotent — the focusManager listener
 * replaces itself if mounted twice (only the latest wins), and
 * AppState.addEventListener returns an unsubscriber.
 */
export function useAppFocusBridge() {
  const queryClient = useQueryClient();

  useEffect(() => {
    // Drive focusManager from AppState transitions. 'active' = focused;
    // anything else (background, inactive) = blurred. React Query's
    // own React Native adapter exists but pulling it in just for this
    // bridge is overkill — the imperative call is two lines.
    const sub = AppState.addEventListener('change', (next: AppStateStatus) => {
      const focused = next === 'active';
      focusManager.setFocused(focused);
      if (!focused) return;
      // App is back in the foreground. Try to revive SignalR if its
      // retry policy gave up while we were backgrounded — failing
      // silently is fine, the polling backstop on useFixtures will
      // catch the next live-minute change.
      if (getLiveStatus() !== 'connected') {
        void ensureLiveConnected().catch(() => {});
      }
      // Belt + braces for the live tiles: explicitly invalidate the
      // fixture/signals families so even queries that don't poll
      // (e.g. signals — analysis page) refetch on resume.
      queryClient.invalidateQueries({ queryKey: ['fixtures'] });
      queryClient.invalidateQueries({ queryKey: ['signals'] });
      queryClient.invalidateQueries({ queryKey: ['fixture'] });
    });
    // Set initial state — RN's AppState.currentState is reliable on
    // mount; we want focusManager to match it from the start.
    focusManager.setFocused(AppState.currentState === 'active');
    return () => sub.remove();
  }, [queryClient]);
}
