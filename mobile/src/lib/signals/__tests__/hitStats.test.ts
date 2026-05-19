import {
  computeHitStats,
  getSignalWinning,
} from '@/src/lib/signals/hitStats';
import type { RateResult } from '@/src/types/rateResult';

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
    // Default to a finished host fixture (state 5 = FT) so the
    // getSignalWinning gate doesn't filter every test row out.
    // Specific test cases override match_state to assert the gate.
    match_state: 5,
    ...overrides,
  } as RateResult;
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

  test('live host fixture excluded from badge even when bet_winning is non-null', () => {
    // Backend now returns running verdicts mid-match (so 1X2 / CS / DC
    // colour the row by the current scoreboard). The badge gate has to
    // exclude those so it never inflates with mid-match flips.
    const result = computeHitStats(
      [
        signal({ bet_winning: true,  match_state: 22 }),  // INPLAY_2ND_HALF
        signal({ bet_winning: false, match_state: 2 }),   // INPLAY_1ST_HALF
        signal({ bet_winning: true,  match_state: 5 }),   // FT — counts
        signal({ bet_winning: false, match_state: 5 }),   // FT — counts
      ],
      getSignalWinning,
    );
    expect(result.finished).toBe(2);
    expect(result.won).toBe(1);
    expect(result.lost).toBe(1);
    expect(result.hitRate).toBe(50);
  });

  test('upcoming host fixture excluded from badge', () => {
    const result = computeHitStats(
      [
        signal({ bet_winning: true, match_state: 1 }),    // NS
        signal({ bet_winning: true, match_state: 5 }),    // FT
      ],
      getSignalWinning,
    );
    expect(result.finished).toBe(1);
    expect(result.won).toBe(1);
  });
});

