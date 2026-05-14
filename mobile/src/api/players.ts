import { apiClient, ApiClientError } from '@/src/api/client';
import type { ApiResponse } from '@/src/types/api';
import type { Player, PlayerSeasonStats } from '@/src/types/player';

export async function getPlayer(id: number): Promise<Player> {
  const response = await apiClient.get<ApiResponse<Player>>(`/players/${id}`);
  const body = response.data;
  if (!body.success || !body.data) {
    throw new ApiClientError(
      body.error?.message ?? 'Player not found',
      body.error?.code ?? 'unknown_error',
      response.status,
    );
  }
  return body.data;
}

export async function getPlayerSeasonStats(
  id: number,
  seasonId?: number,
): Promise<PlayerSeasonStats[]> {
  const qs = seasonId ? `?season_id=${seasonId}` : '';
  const response = await apiClient.get<ApiResponse<PlayerSeasonStats[]>>(
    `/players/${id}/season-stats${qs}`,
  );
  return response.data?.data ?? [];
}
