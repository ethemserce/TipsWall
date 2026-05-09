export interface LeagueTableRow {
  team_id: number | null;
  team_name?: string | null;
  team_image_path?: string | null;
  position: number | null;
  played: number;
  wins: number;
  draws: number;
  losses: number;
  goals_for: number;
  goals_against: number;
  goal_difference: number;
  points: number;
}
