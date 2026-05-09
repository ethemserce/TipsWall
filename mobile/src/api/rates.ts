import { apiClient, ApiClientError } from '@/src/api/client';
import type { ApiPagination, ApiResponse } from '@/src/types/api';
import type { RateListResponse } from '@/src/types/rateResult';

export interface RateQueryParams {
  bookmakerId?: number;
  marketId?: number;
  window?: string;
  matchState?: number;
  minRate?: number;
  maxRate?: number;
  minWinningPercent?: number;
  minEarningPercent?: number;
  minSampleCount?: number;
  fixtureDate?: string;
  page?: number;
  perPage?: number;
}

export interface RateListResult {
  data: RateListResponse;
  pagination: ApiPagination;
}

function paramsToQuery(p: RateQueryParams) {
  return {
    bookmaker_id: p.bookmakerId,
    market_id: p.marketId,
    window: p.window,
    match_state: p.matchState,
    min_rate: p.minRate,
    max_rate: p.maxRate,
    min_winning_percent: p.minWinningPercent,
    min_earning_percent: p.minEarningPercent,
    min_sample_count: p.minSampleCount,
    fixture_date: p.fixtureDate,
    page: p.page,
    per_page: p.perPage,
  };
}

async function fetchRate(path: string, params: RateQueryParams): Promise<RateListResult> {
  const response = await apiClient.get<ApiResponse<RateListResponse>>(path, {
    params: paramsToQuery(params),
  });
  const body = response.data;
  if (!body.success || !body.data) {
    throw new ApiClientError(
      body.error?.message ?? 'Rate fetch failed',
      body.error?.code ?? 'unknown_error',
      response.status,
      path,
    );
  }
  return {
    data: body.data,
    pagination: body.pagination ?? {
      page: 1,
      per_page: body.data.items.length,
      total: body.data.items.length,
      total_pages: 1,
    },
  };
}

export function listHotRate(params: RateQueryParams = {}) {
  return fetchRate('/hot-rate', params);
}

export function listWinningRate(params: RateQueryParams = {}) {
  return fetchRate('/winning-rate', params);
}

export function listEarningRate(params: RateQueryParams = {}) {
  return fetchRate('/earning-rate', params);
}
