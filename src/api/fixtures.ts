import { getPaged } from '@/src/api/client';
import type { FixtureSummary } from '@/src/types/fixture';

export interface ListFixturesParams {
  date?: string;
  fromDate?: string;
  toDate?: string;
  leagueId?: number;
  seasonId?: number;
  teamId?: number;
  stateId?: number;
  page?: number;
  perPage?: number;
}

export function listFixtures(params: ListFixturesParams = {}) {
  return getPaged<FixtureSummary>('/fixtures', {
    date: params.date,
    from_date: params.fromDate,
    to_date: params.toDate,
    league_id: params.leagueId,
    season_id: params.seasonId,
    team_id: params.teamId,
    state_id: params.stateId,
    page: params.page,
    per_page: params.perPage,
  });
}
