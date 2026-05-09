import { apiClient, ApiClientError } from '@/src/api/client';
import type { ApiResponse } from '@/src/types/api';
import type { LeagueTableRow } from '@/src/types/standings';

export async function getLeagueTable(params: {
  leagueId?: number;
  seasonId?: number;
  stageId?: number;
}): Promise<LeagueTableRow[]> {
  const response = await apiClient.get<ApiResponse<LeagueTableRow[]>>(
    '/standings/table',
    {
      params: {
        league_id: params.leagueId,
        season_id: params.seasonId,
        stage_id: params.stageId,
      },
    },
  );
  const body = response.data;
  if (!body.success || !body.data) {
    throw new ApiClientError(
      body.error?.message ?? 'Standings not available',
      body.error?.code ?? 'unknown_error',
      response.status,
    );
  }
  return body.data;
}
