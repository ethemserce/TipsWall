import { apiClient, ApiClientError } from '@/src/api/client';
import type { ApiResponse } from '@/src/types/api';
import type { FixtureOddsMarket } from '@/src/types/fixtureOdds';

export interface FixtureOddsRatesParams {
  fixtureId: number;
  bookmakerId: number;
  marketIds: number[];
  window?: string;
}

export async function getFixtureOddsRates({
  fixtureId,
  bookmakerId,
  marketIds,
  window = 'all',
}: FixtureOddsRatesParams): Promise<FixtureOddsMarket[]> {
  const response = await apiClient.get<ApiResponse<FixtureOddsMarket[]>>(
    `/fixtures/${fixtureId}/odds-rates`,
    {
      params: {
        bookmaker_id: bookmakerId,
        market_ids: marketIds.join(','),
        window,
      },
    },
  );
  const body = response.data;
  if (!body.success || !body.data) {
    throw new ApiClientError(
      body.error?.message ?? 'Could not load odds-rates',
      body.error?.code ?? 'unknown_error',
      response.status,
    );
  }
  return body.data;
}
