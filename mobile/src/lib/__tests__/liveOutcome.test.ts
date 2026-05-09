import { outcomeLiveStatus } from '@/src/lib/liveOutcome';

describe('outcomeLiveStatus — Fulltime Result (market 1)', () => {
  test('home leads → home wins', () => {
    expect(outcomeLiveStatus({ market_id: 1, label: 'Home' }, { home: 2, away: 1 })).toBe('win');
  });
  test('home leads → away loses', () => {
    expect(outcomeLiveStatus({ market_id: 1, label: 'Away' }, { home: 2, away: 1 })).toBe('loss');
  });
  test('level → draw wins', () => {
    expect(outcomeLiveStatus({ market_id: 1, label: 'Draw' }, { home: 1, away: 1 })).toBe('win');
  });
  test('home leads → draw loses', () => {
    expect(outcomeLiveStatus({ market_id: 1, label: 'Draw' }, { home: 2, away: 1 })).toBe('loss');
  });
});

describe('outcomeLiveStatus — Both Teams To Score (market 14)', () => {
  test('both scored → yes wins', () => {
    expect(outcomeLiveStatus({ market_id: 14, label: 'Yes' }, { home: 1, away: 1 })).toBe('win');
  });
  test('one team scoreless → yes loses', () => {
    expect(outcomeLiveStatus({ market_id: 14, label: 'Yes' }, { home: 3, away: 0 })).toBe('loss');
  });
  test('one team scoreless → no wins', () => {
    expect(outcomeLiveStatus({ market_id: 14, label: 'No' }, { home: 3, away: 0 })).toBe('win');
  });
});

describe('outcomeLiveStatus — Goals Over/Under (market 80)', () => {
  test('total above line → over wins', () => {
    expect(
      outcomeLiveStatus(
        { market_id: 80, label: 'Over', total: '2.5', handicap: null },
        { home: 2, away: 1 },
      ),
    ).toBe('win');
  });
  test('total below line → over loses', () => {
    expect(
      outcomeLiveStatus(
        { market_id: 80, label: 'Over', total: '2.5', handicap: null },
        { home: 1, away: 1 },
      ),
    ).toBe('loss');
  });
  test('total exactly on integer line → null (push, undecided live)', () => {
    expect(
      outcomeLiveStatus(
        { market_id: 80, label: 'Over', total: '3', handicap: null },
        { home: 2, away: 1 },
      ),
    ).toBeNull();
  });
});

describe('outcomeLiveStatus — Draw No Bet (market 10)', () => {
  test('home leads → "1" wins', () => {
    expect(outcomeLiveStatus({ market_id: 10, label: '1' }, { home: 2, away: 0 })).toBe('win');
  });
  test('level → null (would push, no verdict)', () => {
    expect(outcomeLiveStatus({ market_id: 10, label: '1' }, { home: 1, away: 1 })).toBeNull();
  });
});

describe('outcomeLiveStatus — Odd/Even total goals (market 44)', () => {
  test('zero goals → null (book pushes 0)', () => {
    // Most books treat 0 total as a push on odd/even, so we report neither.
    expect(outcomeLiveStatus({ market_id: 44, label: 'Odd' }, { home: 0, away: 0 })).toBeNull();
  });
  test('odd total → odd wins', () => {
    expect(outcomeLiveStatus({ market_id: 44, label: 'Odd' }, { home: 2, away: 1 })).toBe('win');
  });
  test('even total → even wins', () => {
    expect(outcomeLiveStatus({ market_id: 44, label: 'Even' }, { home: 1, away: 1 })).toBe('win');
  });
});

describe('outcomeLiveStatus — Unsupported markets', () => {
  test('half-time-only market id 33 returns null', () => {
    // Resolver doesn't have HT-only score, can't decide → must stay neutral
    // so the UI doesn't lie to the user.
    expect(outcomeLiveStatus({ market_id: 33, label: 'Home' }, { home: 1, away: 0 })).toBeNull();
  });

  test('unknown market id returns null', () => {
    expect(outcomeLiveStatus({ market_id: 99999, label: 'Home' }, { home: 1, away: 0 })).toBeNull();
  });
});
