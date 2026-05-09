import { useQueries } from '@tanstack/react-query';
import { useMemo } from 'react';

import { getFixture } from '@/src/api/fixtures';
import type { FixtureDetail } from '@/src/types/fixtureDetail';

const ONE_HOUR = 60 * 60 * 1000;

/**
 * Batch-fetch fixture details for the supplied ids and expose a lookup map
 * keyed by fixture id. Each query is cached for an hour and shared with the
 * detail screen, so revisiting a fixture from the rate list is instant.
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
      staleTime: ONE_HOUR,
      gcTime: ONE_HOUR,
      retry: 1,
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
