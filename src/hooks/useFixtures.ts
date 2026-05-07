import { useQuery } from '@tanstack/react-query';
import { listFixtures, type ListFixturesParams } from '@/src/api/fixtures';

interface UseFixturesOptions {
  refetchIntervalMs?: number;
}

export function useFixtures(
  params: ListFixturesParams,
  options: UseFixturesOptions = {},
) {
  return useQuery({
    queryKey: ['fixtures', params],
    queryFn: () => listFixtures(params),
    refetchInterval: options.refetchIntervalMs,
  });
}
