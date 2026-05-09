import { useQuery } from '@tanstack/react-query';

import { listSignals, type SignalQueryParams } from '@/src/api/signals';

export function useSignals(params: SignalQueryParams = {}) {
  return useQuery({
    queryKey: ['signals', params],
    queryFn: () => listSignals(params),
    staleTime: 60 * 60 * 1000,
  });
}
