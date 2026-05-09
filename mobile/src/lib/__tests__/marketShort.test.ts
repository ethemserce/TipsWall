import { MARKET_SHORT, marketShort, shortenOutcome } from '@/src/lib/marketShort';

describe('marketShort', () => {
  test('known markets resolve to their TR shortcode', () => {
    expect(marketShort(1)).toBe('MS');
    expect(marketShort(14)).toBe('KG');
    expect(marketShort(80)).toBe('A/Ü');
  });

  test('unknown markets fall back to provided name', () => {
    expect(marketShort(9999, 'Custom Market')).toBe('Custom Market');
  });

  test('unknown markets without fallback render as M<id>', () => {
    // Defensive shape so the UI never shows a blank pill, even for a
    // market id we haven't translated yet.
    expect(marketShort(9999)).toBe('M9999');
  });

  test('every defined market has a non-empty shortcode', () => {
    for (const [id, code] of Object.entries(MARKET_SHORT)) {
      expect(code.length).toBeGreaterThan(0);
      expect(marketShort(Number(id))).toBe(code);
    }
  });
});

describe('shortenOutcome — 1X2 (market 1)', () => {
  test('Home → 1', () => {
    expect(shortenOutcome('Home', 1)).toBe('1');
  });
  test('Draw → X', () => {
    expect(shortenOutcome('Draw', 1)).toBe('X');
  });
  test('Away → 2', () => {
    expect(shortenOutcome('Away', 1)).toBe('2');
  });
});

describe('shortenOutcome — KG (market 14)', () => {
  test('Yes → Var', () => {
    expect(shortenOutcome('Yes', 14)).toBe('Var');
  });
});

describe('shortenOutcome — Exact Goals (markets 18/19)', () => {
  test('strips team prefix and "Goals" suffix', () => {
    // Bookmaker feeds a long label; we keep only the numeric tail so the
    // coupon row stays compact.
    expect(shortenOutcome('AGF Aarhus - 3+ Goals', 18)).toBe('3+');
    expect(shortenOutcome('AGF Aarhus - 1 Goal', 18)).toBe('1');
    expect(shortenOutcome('Some Team - 0 Goals', 19)).toBe('0');
  });

  test('label without team prefix still strips suffix', () => {
    expect(shortenOutcome('5+ Goals', 18)).toBe('5+');
  });
});

describe('shortenOutcome — passthrough for unknown markets', () => {
  test('unknown market id returns label unchanged', () => {
    expect(shortenOutcome('Some Outcome', 99999)).toBe('Some Outcome');
  });
});
