import { apiClient, ApiClientError, getPaged } from '@/src/api/client';
import type { ApiResponse } from '@/src/types/api';
import type { League } from '@/src/types/league';

export function listLeagues(params: {
  countryId?: number;
  active?: boolean;
  search?: string;
  page?: number;
  perPage?: number;
} = {}) {
  return getPaged<League>('/leagues', {
    country_id: params.countryId,
    active: params.active,
    search: params.search,
    page: params.page,
    per_page: params.perPage,
  });
}

export async function getLeague(id: number): Promise<League> {
  const response = await apiClient.get<ApiResponse<League>>(`/leagues/${id}`);
  const body = response.data;
  if (!body.success || !body.data) {
    throw new ApiClientError(
      body.error?.message ?? 'League not found',
      body.error?.code ?? 'unknown_error',
      response.status,
    );
  }
  return body.data;
}
