import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';

import { listMarkets } from '@/src/api/markets';
import type { Market } from '@/src/types/market';

const ONE_DAY = 24 * 60 * 60 * 1000;

/**
 * Fetches all active markets once and exposes a lookup Map. Markets are
 * static-ish reference data so we cache for a day.
 */
export function useMarkets() {
  const query = useQuery({
    queryKey: ['markets', 'active'],
    queryFn: () => listMarkets({ active: true, perPage: 200 }),
    staleTime: ONE_DAY,
    gcTime: ONE_DAY,
  });

  const lookup = useMemo(() => {
    const map = new Map<number, Market>();
    for (const m of query.data?.items ?? []) map.set(m.id, m);
    return map;
  }, [query.data]);

  return { lookup, isLoading: query.isLoading };
}
