// Shared between RateMatchCard (analysis) and OddsRatesCard (detail) so the
// coupon stores the same shortcode regardless of where the user picked from.
export const MARKET_SHORT: Record<number, string> = {
  1: 'MS',
  10: 'DNB',
  14: 'KG',
  18: 'EV',
  19: 'DEP',
  31: 'İY MS',
  33: 'İY',
  38: '2Y',
  44: 'T/Ç',
  52: 'EV/DEP',
  80: 'A/Ü',
};

export function marketShort(marketId: number, fallbackName?: string | null): string {
  return MARKET_SHORT[marketId] ?? fallbackName ?? `M${marketId}`;
}

/**
 * Strip exact-goals labels down to their core ("AGF Aarhus - 3+ Goals" → "3+")
 * and translate common 1/X/2-style outcomes for display + coupon storage.
 */
export function shortenOutcome(label: string, marketId: number): string {
  if (marketId === 18 || marketId === 19) {
    const dash = label.lastIndexOf(' - ');
    const tail = dash >= 0 ? label.slice(dash + 3) : label;
    return tail.replace(/\s+Goals?$/i, '');
  }
  if (marketId === 33 || marketId === 38) {
    return label.replace(/\s+Goals?$/i, '');
  }
  if (marketId === 1) {
    if (label === 'Home') return '1';
    if (label === 'Draw') return 'X';
    if (label === 'Away') return '2';
  }
  if (marketId === 14) {
    if (label === 'Yes') return 'Var';
    if (label === 'No') return 'Yok';
  }
  if (marketId === 44) {
    if (label === 'Odd') return 'Tek';
    if (label === 'Even') return 'Çift';
  }
  return label;
}
