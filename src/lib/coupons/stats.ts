import { couponOutcome, selectionStarted, totalOdd } from '@/src/lib/coupons/store';
import type { Coupon } from '@/src/lib/coupons/types';

export interface CouponStats {
  totalCoupons: number;
  settledCoupons: number;
  wonCoupons: number;
  /** wonCoupons / settledCoupons, percent. Null when no coupons have settled. */
  couponHitRate: number | null;
  /** Average odd of fully-won coupons. Null when none have won yet. */
  avgWinningOdd: number | null;
}

export interface MarketBreakdown {
  /** "MS", "KG", etc. */
  marketShort: string;
  total: number;     // settled selections in this market
  won: number;       // selections that hit
  hitRate: number;   // percent (0..100)
  avgOdd: number;    // average odd across settled selections
}

export interface Calibration {
  /** Settled selections where pick-time DSO snapshot exists. */
  totalSelections: number;
  /** Average pick-time DSO (system's predicted hit rate). */
  systemAvgPercent: number;
  /** Actual hit rate of the same selections. */
  userActualPercent: number;
  /** userActualPercent − systemAvgPercent (positive = beating the system). */
  deltaPoints: number;
}

export interface CalibrationPoint {
  /** 1-based ordinal in chronological order. */
  index: number;
  /** Running average of pick-time DSO up to and including this pick. */
  systemCumPercent: number;
  /** Running actual hit rate up to and including this pick. */
  userCumPercent: number;
  /** This individual pick's outcome — drives dot colour. */
  hit: boolean;
}

/**
 * Coupon-level summary across every saved coupon (settled or not).
 * Counts a coupon as "won" only if every selection hit (matches store
 * couponOutcome semantics).
 */
export function computeCouponStats(saved: Coupon[]): CouponStats {
  let settledCoupons = 0;
  let wonCoupons = 0;
  let oddSumOfWon = 0;
  for (const c of saved) {
    const outcome = couponOutcome(c);
    if (outcome.state === 'pending') continue;
    settledCoupons++;
    if (outcome.state === 'won') {
      wonCoupons++;
      oddSumOfWon += totalOdd(c);
    }
  }
  return {
    totalCoupons: saved.length,
    settledCoupons,
    wonCoupons,
    couponHitRate:
      settledCoupons > 0 ? (wonCoupons / settledCoupons) * 100 : null,
    avgWinningOdd: wonCoupons > 0 ? oddSumOfWon / wonCoupons : null,
  };
}

/**
 * Selection-level breakdown — flattens every settled selection across
 * coupons and groups by marketShort. More data points than coupon-level
 * since a 5-leg coupon contributes 5 rows here.
 */
export function computeMarketBreakdown(saved: Coupon[]): MarketBreakdown[] {
  const buckets = new Map<
    string,
    { total: number; won: number; oddSum: number }
  >();
  for (const c of saved) {
    for (const s of c.selections) {
      if (!selectionStarted(s)) continue;
      if (s.betWinning !== true && s.betWinning !== false) continue;
      const key = s.marketShort;
      const b =
        buckets.get(key) ?? { total: 0, won: 0, oddSum: 0 };
      b.total += 1;
      if (s.betWinning === true) b.won += 1;
      b.oddSum += s.oddValue;
      buckets.set(key, b);
    }
  }
  return Array.from(buckets.entries())
    .map(([marketShort, b]) => ({
      marketShort,
      total: b.total,
      won: b.won,
      hitRate: b.total > 0 ? (b.won / b.total) * 100 : 0,
      avgOdd: b.total > 0 ? b.oddSum / b.total : 0,
    }))
    .sort((a, b) => b.total - a.total);
}

/**
 * Side-by-side comparison of the system's pick-time prediction (DSO snapshot)
 * vs the actual hit rate. A positive delta means the user is choosing
 * outcomes that perform above the system's expectation — i.e. they pick
 * "the good ones" out of the candidate pool.
 *
 * Returns null until at least one settled selection has a DSO snapshot.
 */
export function computeCalibration(saved: Coupon[]): Calibration | null {
  let dsoSum = 0;
  let dsoCount = 0;
  let won = 0;
  let total = 0;
  for (const c of saved) {
    for (const s of c.selections) {
      if (!selectionStarted(s)) continue;
      if (s.betWinning !== true && s.betWinning !== false) continue;
      if (s.dso == null) continue;
      total++;
      if (s.betWinning === true) won++;
      dsoSum += s.dso;
      dsoCount++;
    }
  }
  if (dsoCount === 0) return null;
  const systemAvg = dsoSum / dsoCount;
  const actual = (won / total) * 100;
  return {
    totalSelections: total,
    systemAvgPercent: systemAvg,
    userActualPercent: actual,
    deltaPoints: actual - systemAvg,
  };
}

/**
 * Same data as `computeCalibration` but as a chronological time-series so
 * the UI can plot "system prediction vs actual hit rate over time". Each
 * point is the running average up through that pick, so the line smooths
 * out short-term streaks and converges as the sample grows.
 */
export function computeCalibrationTimeline(saved: Coupon[]): CalibrationPoint[] {
  const settled: { ts: number; dso: number; hit: boolean }[] = [];
  for (const c of saved) {
    // Use the coupon's updatedAt as a proxy for "when this pick became
    // judgable" — saveDraft stamps it once, and updateSelectionWinning
    // re-stamps when a leg settles, so it reflects settlement time.
    const ts = Date.parse(c.updatedAt);
    if (Number.isNaN(ts)) continue;
    for (const s of c.selections) {
      if (!selectionStarted(s)) continue;
      if (s.betWinning !== true && s.betWinning !== false) continue;
      if (s.dso == null) continue;
      settled.push({ ts, dso: s.dso, hit: s.betWinning === true });
    }
  }
  settled.sort((a, b) => a.ts - b.ts);
  const out: CalibrationPoint[] = [];
  let dsoSum = 0;
  let hitCount = 0;
  for (let i = 0; i < settled.length; i++) {
    dsoSum += settled[i].dso;
    if (settled[i].hit) hitCount++;
    const n = i + 1;
    out.push({
      index: n,
      systemCumPercent: dsoSum / n,
      userCumPercent: (hitCount / n) * 100,
      hit: settled[i].hit,
    });
  }
  return out;
}
