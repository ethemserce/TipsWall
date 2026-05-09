import { apiClient, ApiClientError, getPaged } from '@/src/api/client';
import type { ApiResponse } from '@/src/types/api';
import type { FixtureSummary } from '@/src/types/fixture';
import type { FixtureDetail } from '@/src/types/fixtureDetail';

export interface ListFixturesParams {
  date?: string;
  fromDate?: string;
  toDate?: string;
  leagueId?: number;
  seasonId?: number;
  teamId?: number;
  stateId?: number;
  page?: number;
  perPage?: number;
}

export function listFixtures(params: ListFixturesParams = {}) {
  return getPaged<FixtureSummary>('/fixtures', {
    date: params.date,
    from_date: params.fromDate,
    to_date: params.toDate,
    league_id: params.leagueId,
    season_id: params.seasonId,
    team_id: params.teamId,
    state_id: params.stateId,
    page: params.page,
    per_page: params.perPage,
  });
}

export async function getFixture(id: number): Promise<FixtureDetail> {
  const response = await apiClient.get<ApiResponse<FixtureDetail>>(
    `/fixtures/${id}`,
  );
  const body = response.data;
  if (!body.success || !body.data) {
    throw new ApiClientError(
      body.error?.message ?? 'Fixture not found',
      body.error?.code ?? 'unknown_error',
      response.status,
    );
  }
  return body.data;
}
