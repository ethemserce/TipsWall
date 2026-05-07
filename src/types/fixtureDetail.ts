import type { FixtureSummary } from '@/src/types/fixture';

export interface FixtureParticipant {
  team_id: number;
  location: string;
  winner: boolean | null;
  position: number | null;
}

export interface FixtureScore {
  id: number;
  type_id: number | null;
  participant_id: number | null;
  participant_location: string | null;
  description: string | null;
  goals: number | null;
}

export interface FixtureDetail {
  fixture: FixtureSummary;
  participants: FixtureParticipant[];
  scores: FixtureScore[];
}
