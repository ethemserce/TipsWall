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
 * Math summary of a single parlay coupon. Derives from the no-vig implied
 * probabilities (İKO) we snapshot at pick time, so the same numbers are
 * stable as time passes (a leg's iko on disk doesn't move just because the
 * bookmaker re-prices later).
 *
 *   combinedProbPercent  — product of leg iko's (treated as independent)
 *   fairOdd              — 1 / combinedProb. The "no-vig" combined odd.
 *   parlayOdd            — actual product of leg odds (what the user gets)
 *   vigPercent           — how much the parlay odd is below fair (overround)
 *   edgePercent          — combinedProb × parlayOdd − 1, expressed as %
 *
 * Returns null when any leg lacks an iko (e.g. very old coupons before we
 * snapshot it) — the math is meaningless without a complete set.
 */
export interface ParlayMath {
  legCount: number;
  combinedProbPercent: number;
  fairOdd: number;
  parlayOdd: number;
  vigPercent: number;
  edgePercent: number;
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
 * Per-coupon parlay math. Treats legs as independent (the textbook
 * assumption — correlated outcomes will drift but for the user's pre-game
 * pick context that's the closest we get).
 *
 * Returns null when any leg is missing iko — without the no-vig prob we
 * can't compute fair odds. Older coupons created before iko was snapshot
 * fall into this case naturally; the UI hides the section when null.
 */
export function computeParlayMath(coupon: Coupon): ParlayMath | null {
  if (coupon.selections.length === 0) return null;

  let combinedProb = 1.0;
  let parlayOdd = 1.0;
  for (const s of coupon.selections) {
    if (s.iko == null || s.iko <= 0) return null;
    if (s.oddValue <= 0) return null;
    combinedProb *= s.iko / 100; // iko is stored as a percent
    parlayOdd *= s.oddValue;
  }

  const fairOdd = combinedProb > 0 ? 1 / combinedProb : Number.POSITIVE_INFINITY;
  // vig = how much overround the bookmaker bakes into the combined odd.
  // (fairOdd − parlayOdd) / fairOdd. Always non-negative in a fair book;
  // negative would mean the parlay is mispriced in the user's favour.
  const vigPercent = fairOdd > 0 ? ((fairOdd - parlayOdd) / fairOdd) * 100 : 0;
  const edgePercent = (combinedProb * parlayOdd - 1) * 100;

  return {
    legCount: coupon.selections.length,
    combinedProbPercent: combinedProb * 100,
    fairOdd,
    parlayOdd,
    vigPercent,
    edgePercent,
  };
}

/**
 * Same data as `computeCalibration` but as a chronological time-series so
 * the UI can plot "system prediction vs actual hit rate over time". Each
 * point is the running average up through that pick, so the line smooths
 * out short-term streaks and converges as the sample grows.
 */
export interface Streak {
  /** Positive = consecutive hits, negative = consecutive misses, 0 = nothing settled yet. */
  length: number;
  /** Total settled selections (across coupons) the streak was computed from. */
  totalSettled: number;
}

/**
 * Current streak across all settled selections, ordered chronologically.
 * "+3" = last three settled picks all hit; "-2" = last two missed. Used
 * for a single-line motivational header in the stats card so the user
 * sees momentum at a glance.
 *
 * Walks selections newest-first; the run stops as soon as the outcome
 * flips. Coupons share an `updatedAt` timestamp so settlements within
 * the same coupon stay grouped.
 */
export function computeStreak(saved: Coupon[]): Streak {
  const settled: { ts: number; hit: boolean }[] = [];
  for (const c of saved) {
    const ts = Date.parse(c.updatedAt);
    if (Number.isNaN(ts)) continue;
    for (const s of c.selections) {
      if (!selectionStarted(s)) continue;
      if (s.betWinning !== true && s.betWinning !== false) continue;
      settled.push({ ts, hit: s.betWinning === true });
    }
  }
  if (settled.length === 0) return { length: 0, totalSettled: 0 };
  settled.sort((a, b) => b.ts - a.ts); // newest first
  const sign = settled[0].hit;
  let n = 0;
  for (const s of settled) {
    if (s.hit !== sign) break;
    n++;
  }
  return {
    length: sign ? n : -n,
    totalSettled: settled.length,
  };
}

export type RiskTier = 'low' | 'mid' | 'high';

export interface RiskProfile {
  /** Average iko (no-vig implied probability, 0-100) across user picks. */
  averageImpPercent: number;
  /** Bucketed tier based on the average — mirrors the analysis filter chips. */
  tier: RiskTier;
  /** How many selections fed the average. */
  sampleSize: number;
}

/**
 * Translates the user's average pick iko into one of the three risk
 * tiers the analysis filter uses (Düşük / Dengeli / Cüretkâr). High iko
 * = high implied probability = "safe" pick territory. Same thresholds
 * as RISK_THRESHOLDS in AnalysisFiltersSheet so the vocabulary stays
 * consistent across the app.
 *
 * Returns null when no selections have an iko snapshot yet.
 */
export function computeRiskProfile(saved: Coupon[]): RiskProfile | null {
  let ikoSum = 0;
  let n = 0;
  for (const c of saved) {
    for (const s of c.selections) {
      if (s.iko == null || s.iko <= 0) continue;
      ikoSum += s.iko;
      n++;
    }
  }
  if (n === 0) return null;
  const avg = ikoSum / n;
  // RISK_THRESHOLDS uses min/max RATE, not iko, but iko is just
  // 100/rate stripped of the vig — same shape, inverted axis.
  // rate ≤ 1.8 → iko ≳ 55%, rate ≥ 3.0 → iko ≲ 33%.
  const tier: RiskTier =
    avg >= 55 ? 'low' : avg >= 33 ? 'mid' : 'high';
  return { averageImpPercent: avg, tier, sampleSize: n };
}

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
