import { useQuery } from '@tanstack/react-query';

import {
  getFixtureEvents,
  getFixtureH2H,
  getFixtureLineups,
  getFixtureStatistics,
} from '@/src/api/fixtureDetailExtras';

export function useFixtureEvents(fixtureId: number, enabled = true) {
  return useQuery({
    queryKey: ['fixture-events', fixtureId],
    queryFn: () => getFixtureEvents(fixtureId),
    enabled: enabled && fixtureId > 0,
  });
}

export function useFixtureStatistics(fixtureId: number, enabled = true) {
  return useQuery({
    queryKey: ['fixture-statistics', fixtureId],
    queryFn: () => getFixtureStatistics(fixtureId),
    enabled: enabled && fixtureId > 0,
  });
}

export function useFixtureLineups(fixtureId: number, enabled = true) {
  return useQuery({
    queryKey: ['fixture-lineups', fixtureId],
    queryFn: () => getFixtureLineups(fixtureId),
    enabled: enabled && fixtureId > 0,
  });
}

export function useFixtureH2H(fixtureId: number, limit = 10, enabled = true) {
  return useQuery({
    queryKey: ['fixture-h2h', fixtureId, limit],
    queryFn: () => getFixtureH2H(fixtureId, limit),
    enabled: enabled && fixtureId > 0,
  });
}
