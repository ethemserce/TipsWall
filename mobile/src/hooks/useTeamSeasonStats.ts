import { useQuery } from '@tanstack/react-query';

import { getTeamSeasonStats } from '@/src/api/teams';

const FIVE_MIN = 5 * 60 * 1000;

/**
 * Team's season stats grouped by (league × season). The worker
 * refreshes analytics.season_team_stats every nightly tier; the daily
 * snapshot cron pokes it again at 02:00 UTC. 5-min stale is plenty
 * for the UI.
 */
export function useTeamSeasonStats(
  teamId: number | null | undefined,
  seasonId?: number,
) {
  return useQuery({
    queryKey: ['team-season-stats', teamId, seasonId ?? null],
    queryFn: () => getTeamSeasonStats(teamId as number, seasonId),
    enabled: teamId != null && teamId > 0,
    staleTime: FIVE_MIN,
    gcTime: 60 * 60 * 1000,
  });
}
