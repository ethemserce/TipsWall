import { useQuery } from '@tanstack/react-query';

import { getFixture } from '@/src/api/fixtures';
import { getStateBucket } from '@/src/lib/fixtureState';

const LIVE_REFETCH_MS = 30 * 1000;

export function useFixture(id: number | null) {
  return useQuery({
    queryKey: ['fixture', id],
    queryFn: () => getFixture(id!),
    enabled: id != null,
    refetchInterval: (query) => {
      const stateId = query.state.data?.fixture.state_id ?? null;
      return getStateBucket(stateId) === 'live' ? LIVE_REFETCH_MS : false;
    },
  });
}
