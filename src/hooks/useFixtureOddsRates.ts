import { useQuery } from '@tanstack/react-query';

import {
  getFixtureOddsRates,
  type FixtureOddsRatesParams,
} from '@/src/api/fixtureOdds';

export function useFixtureOddsRates(params: FixtureOddsRatesParams) {
  return useQuery({
    queryKey: [
      'fixture-odds-rates',
      params.fixtureId,
      params.bookmakerId,
      params.marketIds.join(','),
      params.window ?? 'all',
    ],
    queryFn: () => getFixtureOddsRates(params),
    enabled: params.fixtureId > 0 && params.bookmakerId > 0 && params.marketIds.length > 0,
  });
}
