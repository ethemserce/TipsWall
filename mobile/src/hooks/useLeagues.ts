import { useQuery } from '@tanstack/react-query';

import { listLeagues } from '@/src/api/leagues';

interface UseLeaguesOptions {
  active?: boolean;
  search?: string;
  perPage?: number;
}

const FIVE_MINUTES = 5 * 60 * 1000;

// Catalog-style query — leagues are essentially static for the duration
// of a season, so a 5-minute staleTime keeps requests rare while still
// letting backend admin tweaks (logo updates, name corrections) trickle
// in within minutes.
export function useLeagues(options: UseLeaguesOptions = {}) {
  return useQuery({
    queryKey: ['leagues', options],
    queryFn: () =>
      listLeagues({
        active: options.active ?? true,
        search: options.search,
        perPage: options.perPage ?? 200,
      }),
    staleTime: FIVE_MINUTES,
  });
}
