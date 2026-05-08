import { useQuery } from '@tanstack/react-query';

import { getLeagueTable } from '@/src/api/standings';

export function useLeagueTable(
  leagueId: number | null | undefined,
  seasonId: number | null | undefined,
  enabled: boolean,
) {
  return useQuery({
    queryKey: ['league-table', leagueId, seasonId],
    queryFn: () =>
      getLeagueTable({
        leagueId: leagueId ?? undefined,
        seasonId: seasonId ?? undefined,
      }),
    enabled: enabled && (leagueId != null || seasonId != null),
    staleTime: 60 * 1000,
  });
}
