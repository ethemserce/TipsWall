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
      params.marketIds.join(',') || 'all-calc',
      params.window ?? 'all',
    ],
    queryFn: () => getFixtureOddsRates(params),
    // marketIds may be empty — the API then returns every has_winning_calculations market.
    enabled: params.fixtureId > 0 && params.bookmakerId > 0,
  });
}
