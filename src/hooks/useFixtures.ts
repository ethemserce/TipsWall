import { useQuery } from '@tanstack/react-query';
import { listFixtures, type ListFixturesParams } from '@/src/api/fixtures';

export function useFixtures(params: ListFixturesParams) {
  return useQuery({
    queryKey: ['fixtures', params],
    queryFn: () => listFixtures(params),
  });
}
