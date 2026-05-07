export interface FixtureSummary {
  id: number;
  name: string | null;
  league_id: number;
  season_id: number | null;
  stage_id: number | null;
  round_id: number | null;
  state_id: number | null;
  venue_id: number | null;
  starting_at: string | null;
  has_odds: boolean;
  has_premium_odds: boolean;
  length_minutes: number | null;
  result_info: string | null;
  leg: string | null;
  placeholder: boolean;
}
