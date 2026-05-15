import { apiClient, ApiClientError } from '@/src/api/client';
import type { ApiPagination, ApiResponse } from '@/src/types/api';
import type { RateListResponse } from '@/src/types/rateResult';

export type SignalSort = 'confidence' | 'winning' | 'earning' | 'odd' | 'edge';

export interface SignalQueryParams {
  bookmakerId?: number;
  marketId?: number;
  marketIds?: number[];
  leagueId?: number;
  window?: string;
  matchState?: number;
  minRate?: number;
  maxRate?: number;
  minWinningPercent?: number;
  minEarningPercent?: number;
  minSampleCount?: number;
  fixtureDate?: string;
  valueOnly?: boolean;
  topPerFixture?: number;
  sort?: SignalSort;
  sortDir?: 'asc' | 'desc';
  page?: number;
  perPage?: number;
}

export interface SignalListResult {
  data: RateListResponse;
  pagination: ApiPagination;
}

export async function listSignals(params: SignalQueryParams = {}): Promise<SignalListResult> {
  const response = await apiClient.get<ApiResponse<RateListResponse>>('/signals', {
    params: {
      bookmaker_id: params.bookmakerId,
      market_id: params.marketId,
      // Comma-separated list — backend prefers this over single market_id
      // when both are present.
      market_ids:
        params.marketIds && params.marketIds.length > 0
          ? params.marketIds.join(',')
          : undefined,
      league_id: params.leagueId,
      window: params.window,
      match_state: params.matchState,
      min_rate: params.minRate,
      max_rate: params.maxRate,
      min_winning_percent: params.minWinningPercent,
      min_earning_percent: params.minEarningPercent,
      min_sample_count: params.minSampleCount,
      fixture_date: params.fixtureDate,
      value_only: params.valueOnly ? true : undefined,
      top_per_fixture: params.topPerFixture,
      sort: params.sort,
      sort_dir: params.sortDir,
      page: params.page,
      per_page: params.perPage,
    },
  });
  const body = response.data;
  if (!body.success || !body.data) {
    throw new ApiClientError(
      body.error?.message ?? 'Signals fetch failed',
      body.error?.code ?? 'unknown_error',
      response.status,
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
