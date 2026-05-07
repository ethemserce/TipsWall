import { getPaged } from '@/src/api/client';
import type { RateResult } from '@/src/types/rateResult';

export interface RateQueryParams {
  bookmakerId?: number;
  marketId?: number;
  window?: string;
  matchState?: number;
  page?: number;
  perPage?: number;
}

function paramsToQuery(p: RateQueryParams) {
  return {
    bookmaker_id: p.bookmakerId,
    market_id: p.marketId,
    window: p.window,
    match_state: p.matchState,
    page: p.page,
    per_page: p.perPage,
  };
}

export function listHotRate(params: RateQueryParams = {}) {
  return getPaged<RateResult>('/hot-rate', paramsToQuery(params));
}

export function listWinningRate(params: RateQueryParams = {}) {
  return getPaged<RateResult>('/winning-rate', paramsToQuery(params));
}

export function listEarningRate(params: RateQueryParams = {}) {
  return getPaged<RateResult>('/earning-rate', paramsToQuery(params));
}
