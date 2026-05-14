import { useQuery } from '@tanstack/react-query';

import { getPlayer, getPlayerSeasonStats } from '@/src/api/players';

const ONE_HOUR = 60 * 60 * 1000;

/**
 * Player reference (bio + current team + jersey). Cache aggressively —
 * dob / image / nationality don't change minute-to-minute.
 */
export function usePlayer(id: number | null | undefined) {
  return useQuery({
    queryKey: ['player', id],
    queryFn: () => getPlayer(id as number),
    enabled: id != null && id > 0,
    staleTime: ONE_HOUR,
    gcTime: ONE_HOUR,
  });
}

/**
 * Per-(league × season × team) totals fed by the analytics tier's
 * nightly RunSeasonPlayerStatsAsync. The first row is always the
 * latest as_of_date, so the screen can default-show the current
 * season without UI plumbing.
 */
export function usePlayerSeasonStats(
  playerId: number | null | undefined,
  seasonId?: number,
) {
  return useQuery({
    queryKey: ['player-season-stats', playerId, seasonId ?? null],
    queryFn: () => getPlayerSeasonStats(playerId as number, seasonId),
    enabled: playerId != null && playerId > 0,
    staleTime: 5 * 60 * 1000,
    gcTime: ONE_HOUR,
  });
}
