export interface FixtureEvent {
  id: number;
  minute: number | null;
  extra_minute: number | null;
  type_id: number | null;
  type_code: string | null;
  type_name: string | null;
  participant_id: number | null;
  participant_location: 'home' | 'away' | null;
  player_id: number | null;
  player_name: string | null;
  related_player_name: string | null;
  result: string | null;
  info: string | null;
  injured?: boolean | null;
  /**
   * True when the goal was overturned by VAR (server inference). The
   * row stays in the timeline but is rendered with strikethrough +
   * "İPTAL" badge — standard SofaScore/FotMob behaviour. Defaults to
   * false on older backend builds that didn't emit this flag.
   */
  cancelled?: boolean;
}

export interface FixtureStatistic {
  type_id: number;
  type_code: string | null;
  type_name: string | null;
  home_value: number | null;
  away_value: number | null;
}

export interface FixtureLineupPlayer {
  player_id: number | null;
  player_name: string | null;
  jersey_number: number | null;
  formation_field: string | null;
  formation_position: number | null;
  position_code: string | null;
}

export interface FixtureTeamLineup {
  team_id: number | null;
  formation: string | null;
  starters: FixtureLineupPlayer[];
  bench: FixtureLineupPlayer[];
}

export interface FixtureLineups {
  home: FixtureTeamLineup | null;
  away: FixtureTeamLineup | null;
}

export interface FixtureTrendPoint {
  minute: number | null;
  side: 'home' | 'away' | null;
  value: number | null;
}

export interface FixtureTrend {
  type_id: number;
  type_code: string | null;
  type_name: string | null;
  points: FixtureTrendPoint[];
}

export interface FixtureMatchFact {
  id: number;
  type_id: number | null;
  type_name: string | null;
  category: string | null;
  scope: string | null;
  participant: string | null;
  natural_language: string | null;
}

export interface FixtureWeather {
  temperature_day: number | null;
  temperature_evening: number | null;
  wind_speed: number | null;
  wind_direction: number | null;
  humidity: string | null;
  pressure: number | null;
  clouds: string | null;
  description: string | null;
  icon: string | null;
  metric: string | null;
}

export interface FixtureTvStation {
  id: number;
  name: string | null;
  url: string | null;
  image_path: string | null;
}

export interface FixtureExpectedGoals {
  home: number | null;
  away: number | null;
}

export interface FixtureSidelinedItem {
  player_id: number | null;
  player_name: string | null;
  player_image_path: string | null;
  position_code: string | null;
  category: string | null;
  reason: string | null;
  end_date: string | null;
  games_missed: number | null;
}

export interface FixtureSidelined {
  home: FixtureSidelinedItem[];
  away: FixtureSidelinedItem[];
}

export interface FixtureValueBet {
  id: number;
  type_id: number | null;
  type_name: string | null;
  bet: string | null;
  bookmaker: string | null;
  fair_odd: number | null;
  odd: number | null;
  stake: number | null;
  is_value: boolean | null;
}
