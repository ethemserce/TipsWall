import { useQuery } from '@tanstack/react-query';

import { getTeamSquad } from '@/src/api/teams';

const ONE_HOUR = 60 * 60 * 1000;

/**
 * Active roster for the latest team_squads.season_id (or pinned by
 * caller). Player rows change daily-ish via the worker — 1h stale
 * gives the UI a warm cache while a swipe-to-refresh keeps the door
 * open for forced reloads.
 */
export function useTeamSquad(
  teamId: number | null | undefined,
  seasonId?: number,
) {
  return useQuery({
    queryKey: ['team-squad', teamId, seasonId ?? null],
    queryFn: () => getTeamSquad(teamId as number, seasonId),
    enabled: teamId != null && teamId > 0,
    staleTime: ONE_HOUR,
    gcTime: ONE_HOUR,
  });
}
