// React Native injects __DEV__ at runtime; declare it so TypeScript and
// ts-jest are happy even though the identifier never resolves in tests
// (typeof guards below handle the Jest/Node case).
declare const __DEV__: boolean | undefined;

export interface LiveScore {
  home: number;
  away: number;
}

export interface OutcomeShape {
  market_id: number;
  label: string;
  total?: string | null;
  handicap?: string | null;
}

// Markets the resolver intentionally doesn't decide live — either because
// the data we have (full-match score) can't answer them (half-only markets)
// or because they're known but skipped. Listing them keeps the dev-warn
// below from spamming when these legitimately can't be settled.
const KNOWN_UNRESOLVABLE_MARKETS = new Set<number>([
  31, // Half Time Result — needs HT score, not FT
  33, // First Half Exact Goals
  38, // Second Half Exact Goals
]);

const warnedMarkets = new Set<number>();

function warnUnknownMarket(marketId: number): void {
  if (KNOWN_UNRESOLVABLE_MARKETS.has(marketId)) return;
  if (warnedMarkets.has(marketId)) return;
  warnedMarkets.add(marketId);
  // Loud during development so we notice missing market translations
  // promptly; silent in production + tests (typeof guard handles the
  // Node/Jest environment where __DEV__ doesn't exist).
  if (typeof __DEV__ !== 'undefined' && __DEV__) {
    console.warn(
      `[liveOutcome] no resolver for market_id=${marketId}; outcome will render as undecided.`,
    );
  }
}

/**
 * Decide whether a single bet outcome would settle as a winner or loser if
 * the match ended at the given live score. Returns null when the result
 * isn't resolvable (push, unsupported market, missing line).
 */
export function outcomeLiveStatus(
  outcome: OutcomeShape,
  score: LiveScore,
): 'win' | 'loss' | null {
  const label = (outcome.label ?? '').toLowerCase();
  const total = score.home + score.away;
  switch (outcome.market_id) {
    case 1: // Fulltime Result
      if (label === 'home') return score.home > score.away ? 'win' : 'loss';
      if (label === 'draw') return score.home === score.away ? 'win' : 'loss';
      if (label === 'away') return score.away > score.home ? 'win' : 'loss';
      return null;
    case 52: // Home/Away (draw refunded — push when level)
      if (label === 'home') {
        if (score.home > score.away) return 'win';
        if (score.home < score.away) return 'loss';
        return null;
      }
      if (label === 'away') {
        if (score.away > score.home) return 'win';
        if (score.away < score.home) return 'loss';
        return null;
      }
      return null;
    case 14: // Both Teams To Score
      if (label === 'yes') {
        if (score.home > 0 && score.away > 0) return 'win';
        return 'loss';
      }
      if (label === 'no') {
        if (score.home === 0 || score.away === 0) return 'win';
        return 'loss';
      }
      return null;
    case 80: { // Goals Over/Under
      const lineStr = outcome.total ?? outcome.handicap ?? null;
      if (!lineStr) return null;
      const line = parseFloat(lineStr);
      if (!Number.isFinite(line)) return null;
      if (label === 'over') {
        if (total > line) return 'win';
        if (total < line) return 'loss';
        return null;
      }
      if (label === 'under') {
        if (total < line) return 'win';
        if (total > line) return 'loss';
        return null;
      }
      return null;
    }
    case 10: { // Draw No Bet — outcomes are "1" / "2"
      if (label === '1') {
        if (score.home > score.away) return 'win';
        if (score.home < score.away) return 'loss';
        return null;
      }
      if (label === '2') {
        if (score.away > score.home) return 'win';
        if (score.away < score.home) return 'loss';
        return null;
      }
      return null;
    }
    case 44: { // Odd / Even (total goals)
      if (total === 0) return null; // 0 is treated as a push by most books
      const isOdd = total % 2 === 1;
      if (label === 'odd') return isOdd ? 'win' : 'loss';
      if (label === 'even') return !isOdd ? 'win' : 'loss';
      return null;
    }
    case 18: // Home Team Exact Goals — labels e.g. "Aarhus - 1 Goal", "Aarhus - 3+ Goals"
    case 19: { // Away Team Exact Goals
      const target = outcome.market_id === 18 ? score.home : score.away;
      const goals = parseExactGoalsLabel(outcome.label);
      if (goals == null) return null;
      if (goals.plus) return target >= goals.value ? 'win' : 'loss';
      return target === goals.value ? 'win' : 'loss';
    }
    // 33/38 (First/Second Half Exact Goals) need the half-time/2nd-half-only
    // score, which the fixture summary doesn't expose — leave neutral.
    default:
      warnUnknownMarket(outcome.market_id);
      return null;
  }
}

/** Test-only: clear the warned-once memo so warning behaviour is deterministic. */
export function __resetWarnedMarketsForTests(): void {
  warnedMarkets.clear();
}

function parseExactGoalsLabel(
  label: string,
): { value: number; plus: boolean } | null {
  // "Aarhus - 0 Goals" / "0 Goals" / "1 Goal" / "5+ Goals"
  const tail = label.includes(' - ') ? label.split(' - ').pop() ?? '' : label;
  const match = tail.match(/^(\d+)(\+?)\s+Goals?$/i);
  if (!match) return null;
  return { value: parseInt(match[1], 10), plus: match[2] === '+' };
}
