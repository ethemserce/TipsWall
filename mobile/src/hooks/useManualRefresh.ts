import { useCallback, useState } from 'react';

/**
 * Wraps a React Query refetch so the RefreshControl spinner only fires
 * for user-initiated pull-to-refresh, NOT for background polling /
 * SignalR-triggered invalidations / window-focus refetches.
 *
 * The naive pattern is `<RefreshControl refreshing={isFetching} />`,
 * which leaks the spinner onto every poll tick (every 30s while a live
 * match is in the result set). With this hook, the spinner only spins
 * while the user is actively pulling.
 *
 * Usage:
 *   const { data, refetch, ... } = useFixtures(...);
 *   const { refreshing, onRefresh } = useManualRefresh(refetch);
 *   <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
 */
export function useManualRefresh(refetch: () => Promise<unknown>) {
  const [refreshing, setRefreshing] = useState(false);
  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    try {
      await refetch();
    } finally {
      setRefreshing(false);
    }
  }, [refetch]);
  return { refreshing, onRefresh };
}
