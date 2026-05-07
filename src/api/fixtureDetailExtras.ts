import { apiClient, ApiClientError } from '@/src/api/client';
import type { ApiResponse } from '@/src/types/api';
import type { FixtureSummary } from '@/src/types/fixture';
import type {
  FixtureEvent,
  FixtureLineups,
  FixtureStatistic,
} from '@/src/types/fixtureDetailExtras';

async function fetchOk<T>(path: string, params?: Record<string, unknown>): Promise<T> {
  const response = await apiClient.get<ApiResponse<T>>(path, { params });
  const body = response.data;
  if (!body.success || body.data === undefined) {
    throw new ApiClientError(
      body.error?.message ?? 'Request failed',
      body.error?.code ?? 'unknown_error',
      response.status,
    );
  }
  return body.data;
}

export function getFixtureEvents(fixtureId: number) {
  return fetchOk<FixtureEvent[]>(`/fixtures/${fixtureId}/events`);
}

export function getFixtureStatistics(fixtureId: number) {
  return fetchOk<FixtureStatistic[]>(`/fixtures/${fixtureId}/statistics`);
}

export function getFixtureLineups(fixtureId: number) {
  return fetchOk<FixtureLineups>(`/fixtures/${fixtureId}/lineups`);
}

export function getFixtureH2H(fixtureId: number, limit = 10) {
  return fetchOk<FixtureSummary[]>(`/fixtures/${fixtureId}/h2h`, { limit });
}
