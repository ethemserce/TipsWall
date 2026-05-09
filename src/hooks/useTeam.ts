import { useQuery } from '@tanstack/react-query';

import { getTeam } from '@/src/api/teams';

const ONE_HOUR = 60 * 60 * 1000;

export function useTeam(id: number | null | undefined) {
  return useQuery({
    queryKey: ['team', id],
    queryFn: () => getTeam(id as number),
    enabled: id != null && id > 0,
    staleTime: ONE_HOUR,
    gcTime: ONE_HOUR,
  });
}
