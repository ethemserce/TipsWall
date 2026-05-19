import { getStateBucket } from '@/src/lib/fixtureState';
import { outcomeLiveStatus } from '@/src/lib/liveOutcome';
import type { FixtureDetail } from '@/src/types/fixtureDetail';
import type { RateResult } from '@/src/types/rateResult';

export interface HitStats {
  /** Items whose host fixture has finished (FT/AET/FT pen.) — i.e. winning flag is true OR false, not null. */
  finished: number;
  /** Items where the prediction came true. */
  won: number;
  /** Items where the prediction missed. */
  lost: number;
  /** won / finished as a 0..100 percentage; 0 when finished == 0. */
  hitRate: number;
}

/**
 * Aggregates outcome flags into "X of Y finished came true". Powers
 * the retro-accuracy badge on the analysis screen + the per-fixture
 * summary card on fixture detail.
 *
 * Trusts the winning flag only when it's strictly true or false; null /
 * undefined means the host fixture isn't finished yet and is excluded
 * from both numerator and denominator. Matches the mobile-side
 * `decideSettlement` semantics (lib/coupons/settle.ts) — we only count
 * a settled outcome once the fixture state bucket is `finished`.
 *
 * Generic over the field accessor so the same helper drives both the
 * /signals row shape (RateResult.bet_winning) and the per-fixture odds
 * shape (FixtureOddOutcome.winning) without each call site having to
 * reach inside the model.
 */
export function computeHitStats<T>(
  items: readonly T[],
  getWinning: (item: T) => boolean | null | undefined,
): HitStats {
  let finished = 0;
  let won = 0;
  let lost = 0;
  for (const item of items) {
    const flag = getWinning(item);
    if (flag === true) {
      finished++;
      won++;
    } else if (flag === false) {
      finished++;
      lost++;
    }
  }
  return {
    finished,
    won,
    lost,
    hitRate: finished > 0 ? (won / finished) * 100 : 0,
  };
}

/**
 * Convenience for /signals rows. Backend now returns a running
 * verdict for live matches (so the row UI can paint 1X2 / CS / DC
 * based on the current scoreboard) — but the analysis-page hit-rate
 * badge should only count fully settled outcomes. Gate on the
 * host fixture's bucket here so the badge never inflates with
 * mid-match flips.
 */
export const getSignalWinning = (s: RateResult): boolean | null | undefined => {
  if (getStateBucket(s.match_state ?? null) !== 'finished') return null;
  return s.bet_winning;
};

/**
 * Builds a winning-accessor that mirrors the row-colour resolution used
 * in RateMatchCard / OddsRatesCard: try the client-side outcomeLiveStatus
 * first (so handicap / O-U / 1X2 etc. resolve identically to what the
 * user sees coloured on the card), and fall back to the backend's stored
 * bet_winning when the client resolver can't decide (half-only markets,
 * exotic markets the resolver doesn't cover).
 *
 * Without this, the analysis-screen retro-accuracy pill drifts from the
 * visible row colours: a bet that the client renders as a green "win"
 * via outcomeLiveStatus but for which backend's bet_winning is still
 * null (e.g. before migration 034 deploys for 3-way handicap level
 * cases) would be invisible to the pill. The pill would under-count
 * the wins the user can literally see.
 *
 * Pass the fixture lookup that AnalysisScreen already maintains; we
 * read each signal's host fixture's final score from it.
 */
export function makeSignalWinningWithScore(
  fixtureLookup: ReadonlyMap<number, FixtureDetail>,
): (s: RateResult) => boolean | null | undefined {
  return (s: RateResult) => {
    if (getStateBucket(s.match_state ?? null) !== 'finished') return null;
    const fx = fixtureLookup.get(s.fixture_id)?.fixture;
    const homeScore = fx?.home_score;
    const awayScore = fx?.away_score;
    if (homeScore != null && awayScore != null) {
      const status = outcomeLiveStatus(
        {
          market_id: s.market_id,
          label: s.label,
          total: s.total,
          handicap: s.handicap,
        },
        { home: homeScore, away: awayScore },
      );
      if (status === 'win') return true;
      if (status === 'loss') return false;
    }
    return s.bet_winning;
  };
}
