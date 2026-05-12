import { apiClient, ApiClientError } from '@/src/api/client';
import type { ApiResponse } from '@/src/types/api';
import type { FixtureSummary } from '@/src/types/fixture';
import type {
  FixtureEvent,
  FixtureLineups,
  FixtureMatchFact,
  FixtureStatistic,
  FixtureTrend,
  FixtureTvStation,
  FixtureValueBet,
  FixtureWeather,
} from '@/src/types/fixtureDetailExtras';

async function fetchOk<T>(path: string, params?: Record<string, unknown>): Promise<T> {
  try {
    const response = await apiClient.get<ApiResponse<T>>(path, { params });
    const body = response.data;
    if (!body.success || body.data === undefined) {
      throw new ApiClientError(
        body.error?.message ?? 'Request failed',
        body.error?.code ?? 'unknown_error',
        response.status,
        path,
      );
    }
    return body.data;
  } catch (err) {
    if (err instanceof ApiClientError) throw err;
    const anyErr = err as { response?: { status?: number; data?: { error?: { code?: string; message?: string } } }; message?: string };
    throw new ApiClientError(
      anyErr.response?.data?.error?.message ?? anyErr.message ?? 'Request failed',
      anyErr.response?.data?.error?.code ?? 'network_error',
      anyErr.response?.status,
      path,
    );
  }
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

export function getFixtureTrends(fixtureId: number) {
  return fetchOk<FixtureTrend[]>(`/fixtures/${fixtureId}/trends`);
}

export function getFixtureMatchFacts(fixtureId: number, limit = 30) {
  return fetchOk<FixtureMatchFact[]>(`/fixtures/${fixtureId}/match-facts`, { limit });
}

export function getFixtureWeather(fixtureId: number) {
  return fetchOk<FixtureWeather | null>(`/fixtures/${fixtureId}/weather`);
}

export function getFixtureTvStations(fixtureId: number) {
  return fetchOk<FixtureTvStation[]>(`/fixtures/${fixtureId}/tv-stations`);
}

export function getFixtureValueBets(fixtureId: number) {
  return fetchOk<FixtureValueBet[]>(`/fixtures/${fixtureId}/value-bets`);
}
