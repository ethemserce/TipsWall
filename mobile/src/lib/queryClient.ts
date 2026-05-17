import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30 * 1000,
      gcTime: 5 * 60 * 1000,
      retry: 1,
      // Re-enabled (was false) so the AppState bridge in `useAppFocusBridge`
      // can drive a refetch when the user brings the app back to the
      // foreground. Without it, stale live minutes / scores stick around
      // until the next SignalR push — and SignalR's 6-step reconnect
      // budget runs out fast if the device sleeps for a while.
      refetchOnWindowFocus: true,
    },
  },
});
