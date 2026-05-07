export interface FixtureOddOutcome {
  label: string;
  value: number | null;
  total: string | null;
  handicap: string | null;
  participants: string | null;
  sort_order: number | null;
  win_count: number;
  lost_count: number;
  sample_count: number;
  winning_percent: number | null;
  earning_percent: number | null;
}

export interface FixtureOddsMarket {
  market_id: number;
  market_name: string | null;
  outcomes: FixtureOddOutcome[];
}
