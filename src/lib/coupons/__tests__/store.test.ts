import { couponOutcome, totalOdd } from '@/src/lib/coupons/store';
import type { Coupon, CouponSelection } from '@/src/lib/coupons/types';

const PAST = '2020-01-01T00:00:00Z'; // safely in the past for any test run
const FUTURE = '2099-12-31T23:59:00Z'; // safely in the future

function selection(overrides: Partial<CouponSelection> = {}): CouponSelection {
  return {
    id: 's1',
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

function coupon(selections: CouponSelection[]): Coupon {
  return {
    id: 'c1',
    name: 'Test',
    createdAt: PAST,
    updatedAt: PAST,
    status: 'saved',
    selections,
  };
}

describe('couponOutcome', () => {
  test('all unsettled started selections → pending', () => {
    const c = coupon([selection({ id: 'a' }), selection({ id: 'b' })]);
    expect(couponOutcome(c).state).toBe('pending');
  });

  test('all selections settled won → won', () => {
    const c = coupon([
      selection({ id: 'a', betWinning: true }),
      selection({ id: 'b', betWinning: true }),
    ]);
    expect(couponOutcome(c).state).toBe('won');
  });

  test('one settled lost → lost regardless of pending legs', () => {
    // Parlay logic: a single miss kills the coupon. The remaining unsettled
    // legs are irrelevant because they can no longer save the slip.
    const c = coupon([
      selection({ id: 'a', betWinning: false }),
      selection({ id: 'b' }), // still pending
    ]);
    expect(couponOutcome(c).state).toBe('lost');
  });

  test('upcoming match with stale betWinning is ignored', () => {
    // A future kickoff carrying a stale `winning` flag from prior data must
    // not flip the state — feeds occasionally surface those before kickoff.
    const c = coupon([
      selection({ id: 'a', startingAt: FUTURE, betWinning: false }),
    ]);
    expect(couponOutcome(c).state).toBe('pending');
  });

  test('null startingAt is treated as started (conservative)', () => {
    // Without a kickoff timestamp we can't prove "not started yet", so we
    // honour any existing flag — otherwise old coupons with missing kickoff
    // metadata would never settle.
    const c = coupon([selection({ startingAt: null, betWinning: true })]);
    expect(couponOutcome(c).state).toBe('won');
  });

  test('settled count reflects only settled selections', () => {
    const c = coupon([
      selection({ id: 'a', betWinning: true }),
      selection({ id: 'b' }),
      selection({ id: 'c', betWinning: false }),
    ]);
    const out = couponOutcome(c);
    expect(out.settled).toBe(2);
    expect(out.won).toBe(1);
  });
});

describe('totalOdd', () => {
  test('multiplies every selection odd', () => {
    const c = coupon([
      selection({ oddValue: 2.0 }),
      selection({ oddValue: 1.5 }),
      selection({ oddValue: 3.0 }),
    ]);
    expect(totalOdd(c)).toBeCloseTo(9.0, 5);
  });

  test('zero or missing odd treated as 1× (no-op leg)', () => {
    // Defensive: a malformed selection shouldn't zero out the entire parlay.
    const c = coupon([
      selection({ oddValue: 2.0 }),
      selection({ oddValue: 0 }),
    ]);
    expect(totalOdd(c)).toBe(2.0);
  });
});
