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

  home_team_id?: number | null;
  home_team_name?: string | null;
  home_team_short_code?: string | null;
  home_team_image_path?: string | null;
  home_score?: number | null;

  away_team_id?: number | null;
  away_team_name?: string | null;
  away_team_short_code?: string | null;
  away_team_image_path?: string | null;
  away_score?: number | null;

  live_minute?: number | null;

  home_red_cards?: number | null;
  away_red_cards?: number | null;
  home_var_active?: boolean | null;
  away_var_active?: boolean | null;

  // Venue + main referee — joined server-side from football.venues and
  // football.fixture_referees. Null when SportMonks hasn't published
  // them yet (e.g. youth-league friendlies).
  venue_name?: string | null;
  referee_name?: string | null;
}
