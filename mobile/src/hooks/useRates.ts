import { useQuery } from '@tanstack/react-query';

import {
  listEarningRate,
  listHotRate,
  listWinningRate,
  type RateQueryParams,
} from '@/src/api/rates';

export type RateKind = 'hot' | 'winning' | 'earning';

const FETCHERS = {
  hot: listHotRate,
  winning: listWinningRate,
  earning: listEarningRate,
} as const;

export function useRate(kind: RateKind, params: RateQueryParams = {}) {
  return useQuery({
    queryKey: ['rate', kind, params],
    queryFn: () => FETCHERS[kind](params),
    staleTime: 60 * 60 * 1000,
  });
}
