import { useQueries } from '@tanstack/react-query';
import { useMemo } from 'react';

import { getLeague } from '@/src/api/leagues';
import type { League } from '@/src/types/league';

const ONE_HOUR = 60 * 60 * 1000;

/**
 * Batch-fetches league details for the given ids and exposes a lookup map.
 * Each league is cached for an hour and shared across screens.
 */
export function useLeagueLookup(ids: number[]) {
  const uniqueIds = useMemo(
    () => Array.from(new Set(ids)).sort((a, b) => a - b),
    [ids],
  );

  const queries = useQueries({
    queries: uniqueIds.map((id) => ({
      queryKey: ['league', id] as const,
      queryFn: () => getLeague(id),
      staleTime: ONE_HOUR,
      gcTime: ONE_HOUR,
      retry: 1,
    })),
  });

  const lookup = useMemo(() => {
    const map = new Map<number, League>();
    queries.forEach((q, idx) => {
      if (q.data) map.set(uniqueIds[idx], q.data);
    });
    return map;
  }, [queries, uniqueIds]);

  return {
    lookup,
    isLoading: queries.some((q) => q.isLoading),
  };
}
