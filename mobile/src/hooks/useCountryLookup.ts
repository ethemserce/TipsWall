import { useQueries } from '@tanstack/react-query';
import { useMemo } from 'react';

import { getCountry } from '@/src/api/countries';
import type { Country } from '@/src/types/country';

const ONE_DAY = 24 * 60 * 60 * 1000;

/**
 * Batch-fetches country details (used for flags). Country data is essentially
 * static so we cache aggressively for a day.
 */
export function useCountryLookup(ids: (number | null | undefined)[]) {
  const uniqueIds = useMemo(
    () =>
      Array.from(
        new Set(ids.filter((id): id is number => typeof id === 'number')),
      ).sort((a, b) => a - b),
    [ids],
  );

  const queries = useQueries({
    queries: uniqueIds.map((id) => ({
      queryKey: ['country', id] as const,
      queryFn: () => getCountry(id),
      staleTime: ONE_DAY,
      gcTime: ONE_DAY,
      retry: 1,
    })),
  });

  const lookup = useMemo(() => {
    const map = new Map<number, Country>();
    queries.forEach((q, idx) => {
      if (q.data) map.set(uniqueIds[idx], q.data);
    });
    return map;
  }, [queries, uniqueIds]);

  return { lookup };
}
