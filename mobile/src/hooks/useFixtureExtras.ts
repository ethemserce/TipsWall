import { useQuery } from '@tanstack/react-query';

import {
  getFixtureEvents,
  getFixtureExpectedGoals,
  getFixtureH2H,
  getFixtureLineups,
  getFixtureMatchFacts,
  getFixtureSidelined,
  getFixtureStatistics,
  getFixtureTrends,
  getFixtureTvStations,
  getFixtureValueBets,
  getFixtureWeather,
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

export function useFixtureTrends(fixtureId: number, enabled = true) {
  return useQuery({
    queryKey: ['fixture-trends', fixtureId],
    queryFn: () => getFixtureTrends(fixtureId),
    enabled: enabled && fixtureId > 0,
  });
}

export function useFixtureMatchFacts(fixtureId: number, limit = 30, enabled = true) {
  return useQuery({
    queryKey: ['fixture-match-facts', fixtureId, limit],
    queryFn: () => getFixtureMatchFacts(fixtureId, limit),
    enabled: enabled && fixtureId > 0,
  });
}

export function useFixtureWeather(fixtureId: number, enabled = true) {
  return useQuery({
    queryKey: ['fixture-weather', fixtureId],
    queryFn: () => getFixtureWeather(fixtureId),
    enabled: enabled && fixtureId > 0,
  });
}

export function useFixtureTvStations(fixtureId: number, enabled = true) {
  return useQuery({
    queryKey: ['fixture-tv-stations', fixtureId],
    queryFn: () => getFixtureTvStations(fixtureId),
    enabled: enabled && fixtureId > 0,
  });
}

export function useFixtureValueBets(fixtureId: number, enabled = true) {
  return useQuery({
    queryKey: ['fixture-value-bets', fixtureId],
    queryFn: () => getFixtureValueBets(fixtureId),
    enabled: enabled && fixtureId > 0,
  });
}

export function useFixtureExpectedGoals(fixtureId: number, enabled = true) {
  return useQuery({
    queryKey: ['fixture-expected-goals', fixtureId],
    queryFn: () => getFixtureExpectedGoals(fixtureId),
    enabled: enabled && fixtureId > 0,
  });
}

export function useFixtureSidelined(fixtureId: number, enabled = true) {
  return useQuery({
    queryKey: ['fixture-sidelined', fixtureId],
    queryFn: () => getFixtureSidelined(fixtureId),
    enabled: enabled && fixtureId > 0,
  });
}
