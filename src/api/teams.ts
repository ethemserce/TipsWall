import { apiClient, ApiClientError } from '@/src/api/client';
import type { ApiResponse } from '@/src/types/api';
import type { Team } from '@/src/types/team';

export async function getTeam(id: number): Promise<Team> {
  const response = await apiClient.get<ApiResponse<Team>>(`/teams/${id}`);
  const body = response.data;
  if (!body.success || !body.data) {
    throw new ApiClientError(
      body.error?.message ?? 'Team not found',
      body.error?.code ?? 'unknown_error',
      response.status,
    );
  }
  return body.data;
}
