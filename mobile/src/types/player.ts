export interface Player {
  id: number;
  name: string;
  display_name: string | null;
  first_name: string | null;
  last_name: string | null;
  image_path: string | null;
  date_of_birth: string | null;
  nationality_id: number | null;
  country_id: number | null;
  height: number | null;
  weight: number | null;
  position_id: number | null;
  // GOALKEEPER | DEFENDER | MIDFIELDER | ATTACKER | ...
  position_code: string | null;
  gender: string | null;
  current_team_id: number | null;
  current_team_name: string | null;
  current_team_image_path: string | null;
  current_jersey_number: number | null;
  current_captain: boolean | null;
}

export interface PlayerSeasonStats {
  league_id: number;
  league_name: string | null;
  season_id: number;
  season_name: string | null;
  team_id: number;
  team_name: string | null;
  team_image_path: string | null;
  as_of_date: string;
  fixture_scope: string;
  matches_played: number | null;
  matches_started: number | null;
  matches_subbed_in: number | null;
  matches_subbed_out: number | null;
  minutes_played: number | null;
  goals: number | null;
  assists: number | null;
  own_goals: number | null;
  penalties_scored: number | null;
  penalties_missed: number | null;
  yellow_cards: number | null;
  red_cards: number | null;
}
