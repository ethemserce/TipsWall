export interface RateResult {
  id: string;
  fixture_id: number;
  fixture_signal_id: string | null;
  bookmaker_id: number;
  market_id: number;
  window_code: string;
  outcome_key: string;
  label: string;
  odd_value: number | null;
  total: string | null;
  handicap: string | null;
  win_count: number;
  lost_count: number;
  sample_count: number;
  winning_percent: number | null;
  earning_percent: number | null;
  rank_order: number;
  match_state: number | null;
}

export interface RateSummary {
  total_signals: number;
  total_samples: number;
  avg_winning_percent: number | null;
  avg_earning_percent: number | null;
  avg_odd_value: number | null;
  bet_total: number;
  success_count: number;
  fail_count: number;
  earning_total: number | null;
}

export interface RateListResponse {
  items: RateResult[];
  summary: RateSummary;
  as_of_date: string | null;
}
