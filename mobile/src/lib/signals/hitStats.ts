import type { RateResult } from '@/src/types/rateResult';
import type { FixtureOddOutcome } from '@/src/types/fixtureOdds';

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

/** Convenience for /signals rows. */
export const getSignalWinning = (s: RateResult) => s.bet_winning;

/** Convenience for per-fixture odds outcomes. */
export const getOutcomeWinning = (o: FixtureOddOutcome) => o.winning;
