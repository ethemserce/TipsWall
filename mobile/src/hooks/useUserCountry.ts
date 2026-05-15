import * as Localization from 'expo-localization';
import { useQuery } from '@tanstack/react-query';

import { apiClient } from '@/src/api/client';
import type { ApiResponse } from '@/src/types/api';
import type { Country } from '@/src/types/country';

/**
 * Resolves the user's country_id from their device locale.
 *
 * Device tells us an ISO-3166 alpha-2 code (e.g. "TR", "US"). The
 * backend's catalog.countries table stores the same code in `iso2`, so
 * we look up once per session and cache by region for the lifetime of
 * the QueryClient. Returns null when:
 *   - the device exposes no region code (rare; some emulators),
 *   - the country isn't in our DB (SportMonks coverage gaps).
 *
 * Consumers (Home and Analysis screens) use the resolved id to lift
 * the user's national league above other category=1 leagues.
 */
export function useUserCountry() {
  const locales = Localization.getLocales();
  const regionCode = locales[0]?.regionCode?.toUpperCase() ?? null;

  return useQuery({
    queryKey: ['user-country', regionCode],
    enabled: regionCode != null,
    staleTime: Infinity,
    queryFn: async (): Promise<Country | null> => {
      if (!regionCode) return null;
      const response = await apiClient.get<ApiResponse<Country[]>>('/countries', {
        params: { iso2: regionCode, per_page: 1 },
      });
      const body = response.data;
      if (!body.success || !body.data || body.data.length === 0) return null;
      return body.data[0];
    },
  });
}

export function useUserCountryId(): number | null {
  const { data } = useUserCountry();
  return data?.id ?? null;
}
