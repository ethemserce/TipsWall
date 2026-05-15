import { apiClient, ApiClientError } from '@/src/api/client';
import type { ApiResponse } from '@/src/types/api';

export interface MarketPreferencesPayload {
  market_ids: number[];
  cap: number;
  tier: 'guest' | 'free' | 'premium';
  defaults: number[];
}

export async function getMarketPreferences(): Promise<MarketPreferencesPayload> {
  const response = await apiClient.get<ApiResponse<MarketPreferencesPayload>>(
    '/me/market-preferences',
  );
  const body = response.data;
  if (!body.success || !body.data) {
    throw new ApiClientError(
      body.error?.message ?? 'Market preferences fetch failed',
      body.error?.code ?? 'unknown_error',
      response.status,
    );
  }
  return body.data;
}

export async function putMarketPreferences(
  marketIds: number[],
): Promise<MarketPreferencesPayload> {
  const response = await apiClient.put<ApiResponse<MarketPreferencesPayload>>(
    '/me/market-preferences',
    { market_ids: marketIds },
  );
  const body = response.data;
  if (!body.success || !body.data) {
    throw new ApiClientError(
      body.error?.message ?? 'Market preferences update failed',
      body.error?.code ?? 'unknown_error',
      response.status,
    );
  }
  return body.data;
}

/**
 * Anonymous endpoint that returns the curated defaults + cap for the
 * given tier. Used at first launch (and after a tier change) to
 * auto-fill the picker without an account.
 */
export async function getCuratedMarkets(
  tier: 'guest' | 'free' | 'premium',
): Promise<MarketPreferencesPayload> {
  const response = await apiClient.get<ApiResponse<MarketPreferencesPayload>>(
    '/markets/curated',
    { params: { tier } },
  );
  const body = response.data;
  if (!body.success || !body.data) {
    throw new ApiClientError(
      body.error?.message ?? 'Curated markets fetch failed',
      body.error?.code ?? 'unknown_error',
      response.status,
    );
  }
  return body.data;
}
