import {
  computeHitStats,
  getOutcomeWinning,
  getSignalWinning,
} from '@/src/lib/signals/hitStats';
import type { RateResult } from '@/src/types/rateResult';
import type { FixtureOddOutcome } from '@/src/types/fixtureOdds';

function signal(overrides: Partial<RateResult> = {}): RateResult {
  return {
    fixture_id: 1,
    bookmaker_id: 2,
    market_id: 1,
    label: 'Home',
    total: null,
    handicap: null,
    participants: null,
    sort_order: null,
    win_count: 0,
    lost_count: 0,
    sample_count: 0,
    winning_percent: null,
    earning_percent: null,
    iko: null,
    value: null,
    bet_winning: null,
    avg_winning_percent: null,
    ...overrides,
  } as RateResult;
}

function outcome(overrides: Partial<FixtureOddOutcome> = {}): FixtureOddOutcome {
  return {
    label: 'Home',
    total: null,
    handicap: null,
    participants: null,
    sort_order: null,
    win_count: 0,
    lost_count: 0,
    sample_count: 0,
    winning_percent: null,
    earning_percent: null,
    iko: null,
    value: null,
    winning: null,
    ...overrides,
  } as FixtureOddOutcome;
}

describe('computeHitStats (signal accessor)', () => {
  test('empty input → all zeros', () => {
    expect(computeHitStats([], getSignalWinning)).toEqual({
      finished: 0,
      won: 0,
      lost: 0,
      hitRate: 0,
    });
  });

  test('only pending (bet_winning null) → all zeros', () => {
    const result = computeHitStats([signal(), signal(), signal()], getSignalWinning);
    expect(result).toEqual({ finished: 0, won: 0, lost: 0, hitRate: 0 });
  });

  test('mixed: 3 won / 2 lost / 5 pending → 3/5 = 60%', () => {
    const result = computeHitStats(
      [
        signal({ bet_winning: true }),
        signal({ bet_winning: true }),
        signal({ bet_winning: true }),
        signal({ bet_winning: false }),
        signal({ bet_winning: false }),
        signal({ bet_winning: null }),
        signal({ bet_winning: null }),
        signal({ bet_winning: null }),
        signal({ bet_winning: null }),
        signal({ bet_winning: null }),
      ],
      getSignalWinning,
    );
    expect(result.finished).toBe(5);
    expect(result.won).toBe(3);
    expect(result.lost).toBe(2);
    expect(result.hitRate).toBe(60);
  });

  test('all won → 100%', () => {
    const result = computeHitStats(
      [signal({ bet_winning: true }), signal({ bet_winning: true })],
      getSignalWinning,
    );
    expect(result.hitRate).toBe(100);
  });

  test('all lost → 0%', () => {
    const result = computeHitStats(
      [signal({ bet_winning: false }), signal({ bet_winning: false })],
      getSignalWinning,
    );
    expect(result.hitRate).toBe(0);
    expect(result.finished).toBe(2);
  });

  test('undefined bet_winning treated like null (pending)', () => {
    const result = computeHitStats(
      [signal({ bet_winning: undefined as unknown as null }), signal({ bet_winning: true })],
      getSignalWinning,
    );
    expect(result.finished).toBe(1);
    expect(result.won).toBe(1);
  });
});

describe('computeHitStats (outcome accessor)', () => {
  test('FixtureOddOutcome shape works with getOutcomeWinning', () => {
    const result = computeHitStats(
      [
        outcome({ winning: true }),
        outcome({ winning: true }),
        outcome({ winning: false }),
        outcome({ winning: null }),
      ],
      getOutcomeWinning,
    );
    expect(result.finished).toBe(3);
    expect(result.won).toBe(2);
    expect(result.hitRate).toBeCloseTo(66.667, 2);
  });
});
