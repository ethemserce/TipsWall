export interface Team {
  id: number;
  country_id: number | null;
  venue_id: number | null;
  name: string;
  short_code: string | null;
  image_path: string | null;
  founded: number | null;
  type: string | null;
  gender: string | null;
}

export interface TeamSeasonStats {
  league_id: number;
  season_id: number;
  team_id: number;
  as_of_date: string;
  fixture_scope: string;
  matches_played: number | null;
  matches_won: number | null;
  matches_drawn: number | null;
  matches_lost: number | null;
  goals_for: number | null;
  goals_against: number | null;
  goal_difference: number | null;
  clean_sheets: number | null;
  failed_to_score: number | null;
  both_teams_scored: number | null;
  yellow_cards: number | null;
  red_cards: number | null;
  average_goals_for: number | null;
  average_goals_against: number | null;
  points: number | null;
  // "WWDLW" — last 5 outcomes oldest → newest.
  form: string | null;
}

export interface TeamSquadMember {
  player_id: number;
  season_id: number | null;
  name: string;
  display_name: string | null;
  first_name: string | null;
  last_name: string | null;
  image_path: string | null;
  date_of_birth: string | null;
  nationality_id: number | null;
  height: number | null;
  weight: number | null;
  jersey_number: number | null;
  captain: boolean | null;
  position_id: number | null;
  // GOALKEEPER | DEFENDER | MIDFIELDER | ATTACKER | etc.
  position_code: string | null;
}
