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

// Turkish long-form names for the market headers. Card titles previously
// surfaced SportMonks' raw English market.name ("Fulltime Result", "Both
// Teams To Score") — these Turkish equivalents replace that on the UI,
// keyed by market_id so we don't depend on the upstream string.
export const MARKET_LONG_TR: Record<number, string> = {
  1: 'Maç Sonucu',
  10: 'Beraberlikte İade',
  14: 'Karşılıklı Gol',
  18: 'Ev Sahibi Kesin Skor',
  19: 'Deplasman Kesin Skor',
  31: 'İlk Yarı / Maç Sonu',
  33: 'İlk Yarı Kesin Skor',
  38: 'İkinci Yarı Kesin Skor',
  39: 'Deplasman İki Yarıyı da Önde Bitirir',
  41: 'Ev Sahibi İki Yarıyı da Önde Bitirir',
  44: 'Toplam Gol Tek / Çift',
  50: 'Ev Sahibi Kalesini Gole Kapatır',
  51: 'Deplasman Kalesini Gole Kapatır',
  52: 'Çifte Şans',
  80: 'Toplam Gol Alt / Üst',
};

export function marketShort(marketId: number, fallbackName?: string | null): string {
  return MARKET_SHORT[marketId] ?? fallbackName ?? `M${marketId}`;
}

/**
 * Long-form Turkish market label for card headers. Falls back to the
 * SportMonks-supplied English name when we don't have a translation —
 * better to show *something* than to break the header for unmapped
 * markets the worker happens to sync.
 */
export function marketLongName(
  marketId: number,
  fallbackName?: string | null,
): string {
  return MARKET_LONG_TR[marketId] ?? fallbackName ?? `Market #${marketId}`;
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
