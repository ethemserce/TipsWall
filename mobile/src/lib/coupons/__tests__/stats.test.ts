import {
  computeCalibration,
  computeCalibrationTimeline,
  computeCouponStats,
  computeMarketBreakdown,
  computeParlayMath,
} from '@/src/lib/coupons/stats';
import type { Coupon, CouponSelection } from '@/src/lib/coupons/types';

const PAST = '2020-01-01T00:00:00Z';

function selection(overrides: Partial<CouponSelection> = {}): CouponSelection {
  return {
    id: 's',
    fixtureId: 1,
    fixtureName: 'A vs B',
    startingAt: PAST,
    bookmakerId: 2,
    marketId: 1,
    marketShort: 'MS',
    outcomeLabel: 'Home',
    total: null,
    handicap: null,
    oddValue: 2.0,
    dso: null,
    vbet: null,
    iko: null,
    sampleCount: null,
    ...overrides,
  };
}

function coupon(selections: CouponSelection[], updatedAt = PAST): Coupon {
  return {
    id: `c-${Math.random()}`,
    name: 'Test',
    createdAt: PAST,
    updatedAt,
    status: 'saved',
    selections,
  };
}

describe('computeCouponStats', () => {
  test('counts settled vs total and computes hit rate', () => {
    const cs = [
      coupon([selection({ betWinning: true })]),
      coupon([selection({ betWinning: true })]),
      coupon([selection({ betWinning: false })]),
      coupon([selection()]), // pending
    ];
    const stats = computeCouponStats(cs);
    expect(stats.totalCoupons).toBe(4);
    expect(stats.settledCoupons).toBe(3);
    expect(stats.wonCoupons).toBe(2);
    expect(stats.couponHitRate).toBeCloseTo((2 / 3) * 100, 5);
  });

  test('returns null hit rate when nothing has settled', () => {
    const cs = [coupon([selection()])];
    expect(computeCouponStats(cs).couponHitRate).toBeNull();
  });

  test('avgWinningOdd considers only fully-won coupons', () => {
    const cs = [
      coupon([selection({ oddValue: 2.0, betWinning: true })]),
      coupon([selection({ oddValue: 4.0, betWinning: true })]),
      coupon([selection({ oddValue: 1.5, betWinning: false })]), // lost
    ];
    expect(computeCouponStats(cs).avgWinningOdd).toBeCloseTo(3.0, 5);
  });
});

describe('computeMarketBreakdown', () => {
  test('aggregates by marketShort across coupons', () => {
    const cs = [
      coupon([
        selection({ marketShort: 'MS', betWinning: true, oddValue: 2.0 }),
        selection({ marketShort: 'KG', betWinning: true, oddValue: 1.8 }),
      ]),
      coupon([
        selection({ marketShort: 'MS', betWinning: false, oddValue: 3.0 }),
      ]),
    ];
    const rows = computeMarketBreakdown(cs);
    const ms = rows.find((r) => r.marketShort === 'MS');
    expect(ms).toBeDefined();
    expect(ms!.total).toBe(2);
    expect(ms!.won).toBe(1);
    expect(ms!.hitRate).toBeCloseTo(50, 5);
  });
});

describe('computeCalibration', () => {
  test('returns null when no settled selection has a DSO snapshot', () => {
    const cs = [coupon([selection({ betWinning: true })])]; // no dso
    expect(computeCalibration(cs)).toBeNull();
  });

  test('positive delta when user beats the system', () => {
    // System predicted 50% on average; user actually hit 100% → +50pp delta.
    const cs = [
      coupon([selection({ dso: 50, betWinning: true })]),
      coupon([selection({ dso: 50, betWinning: true })]),
    ];
    const cal = computeCalibration(cs)!;
    expect(cal.systemAvgPercent).toBeCloseTo(50, 5);
    expect(cal.userActualPercent).toBeCloseTo(100, 5);
    expect(cal.deltaPoints).toBeCloseTo(50, 5);
  });

  test('negative delta when user underperforms', () => {
    const cs = [
      coupon([selection({ dso: 70, betWinning: false })]),
      coupon([selection({ dso: 70, betWinning: true })]),
    ];
    const cal = computeCalibration(cs)!;
    expect(cal.systemAvgPercent).toBeCloseTo(70, 5);
    expect(cal.userActualPercent).toBeCloseTo(50, 5);
    expect(cal.deltaPoints).toBeCloseTo(-20, 5);
  });
});

describe('computeParlayMath', () => {
  test('null when any leg lacks iko', () => {
    const c = coupon([selection({ iko: null, oddValue: 2.0 })]);
    expect(computeParlayMath(c)).toBeNull();
  });

  test('positive edge when leg odds beat the no-vig fair odd', () => {
    // Two legs, each priced fair-and-then-some: iko=50% (fair odd 2.0)
    // but bookmaker offers 2.10 (mispriced in user's favour).
    const c = coupon([
      selection({ id: 'a', iko: 50, oddValue: 2.10 }),
      selection({ id: 'b', iko: 50, oddValue: 2.10 }),
    ]);
    const m = computeParlayMath(c)!;
    expect(m.legCount).toBe(2);
    expect(m.combinedProbPercent).toBeCloseTo(25, 5); // 0.5 × 0.5
    expect(m.fairOdd).toBeCloseTo(4, 5); // 1/0.25
    expect(m.parlayOdd).toBeCloseTo(4.41, 5); // 2.10 × 2.10
    // edge = 0.25 × 4.41 − 1 = 0.1025 → 10.25%
    expect(m.edgePercent).toBeCloseTo(10.25, 4);
    // vig is negative when the parlay is in the user's favour (4.41 > 4)
    expect(m.vigPercent).toBeLessThan(0);
  });

  test('typical bookmaker vig drops parlay below fair odd', () => {
    // Two legs with 5% vig: each leg iko 50% (no-vig) but odd is 1.91
    // (≈ 4.5% under fair). Combined parlay should show vig.
    const c = coupon([
      selection({ id: 'a', iko: 50, oddValue: 1.91 }),
      selection({ id: 'b', iko: 50, oddValue: 1.91 }),
    ]);
    const m = computeParlayMath(c)!;
    expect(m.fairOdd).toBeCloseTo(4, 5);
    expect(m.parlayOdd).toBeCloseTo(3.6481, 4);
    // vig = (4 − 3.6481) / 4 = 8.8% — vigs compound across legs.
    expect(m.vigPercent).toBeGreaterThan(8);
    expect(m.vigPercent).toBeLessThan(10);
    expect(m.edgePercent).toBeLessThan(0);
  });

  test('returns null on empty coupon', () => {
    expect(computeParlayMath(coupon([]))).toBeNull();
  });
});

describe('computeCalibrationTimeline', () => {
  test('produces a chronologically sorted cumulative series', () => {
    const cs = [
      coupon(
        [selection({ id: 'a', dso: 60, betWinning: true })],
        '2024-01-01T00:00:00Z',
      ),
      coupon(
        [selection({ id: 'b', dso: 40, betWinning: false })],
        '2024-01-02T00:00:00Z',
      ),
    ];
    const points = computeCalibrationTimeline(cs);
    expect(points).toHaveLength(2);
    expect(points[0].userCumPercent).toBeCloseTo(100, 5); // 1/1
    expect(points[1].userCumPercent).toBeCloseTo(50, 5); // 1/2
    expect(points[1].systemCumPercent).toBeCloseTo(50, 5); // (60+40)/2
  });
});
