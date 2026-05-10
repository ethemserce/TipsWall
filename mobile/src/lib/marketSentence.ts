import type { TFunction } from 'i18next';

import { marketShort, shortenOutcome } from './marketShort';

/**
 * Renders a tip / market outcome as a full Turkish sentence — "Ev sahibi
 * kazanır" instead of "MS 1", "Maçta 3 veya daha çok gol" instead of
 * "A/Ü 2.5 Üst". Used everywhere user-facing copy refers to a prediction.
 *
 * Falls back to "{marketShort} · {outcome}" for combinations we haven't
 * mapped yet, so unknown markets degrade to the legacy short form rather
 * than breaking the UI.
 */
export function outcomeSentence(
  t: TFunction,
  marketId: number,
  label: string,
  total: string | null,
  handicap: string | null,
): string {
  const lower = label.toLowerCase().trim();

  // 1X2 — Maç Sonucu
  if (marketId === 1) {
    if (lower === 'home') return t('marketOutcome.ms_home');
    if (lower === 'draw') return t('marketOutcome.ms_draw');
    if (lower === 'away') return t('marketOutcome.ms_away');
  }

  // Draw No Bet — Beraberlikte İade
  if (marketId === 10) {
    if (lower === 'home') return t('marketOutcome.dnb_home');
    if (lower === 'away') return t('marketOutcome.dnb_away');
  }

  // Both Teams To Score — Karşılıklı Gol
  if (marketId === 14) {
    if (lower === 'yes') return t('marketOutcome.btts_yes');
    if (lower === 'no') return t('marketOutcome.btts_no');
  }

  // Total Goals Over/Under (e.g. 2.5 Over → "Maçta 3+ gol")
  if (marketId === 80 && total) {
    const totalNum = Number.parseFloat(total);
    if (Number.isFinite(totalNum)) {
      const overGoals = Math.ceil(totalNum);
      const underGoals = Math.floor(totalNum);
      if (lower === 'over') {
        return t('marketOutcome.over', { goals: overGoals });
      }
      if (lower === 'under') {
        return t('marketOutcome.under', { goals: underGoals });
      }
    }
  }

  // HT/FT — İlk Yarı / Maç Sonu
  if (marketId === 31) {
    const combo = label.replace(/\s+/g, '').toLowerCase();
    const map: Record<string, string> = {
      '1/1': 'iyms_1_1',
      '1/x': 'iyms_1_x',
      '1/2': 'iyms_1_2',
      'x/1': 'iyms_x_1',
      'x/x': 'iyms_x_x',
      'x/2': 'iyms_x_2',
      '2/1': 'iyms_2_1',
      '2/x': 'iyms_2_x',
      '2/2': 'iyms_2_2',
    };
    if (combo in map) return t(`marketOutcome.${map[combo]}`);
  }

  // First Half Result — İlk Yarı Sonucu
  if (marketId === 33) {
    if (lower === 'home') return t('marketOutcome.iy_home');
    if (lower === 'draw') return t('marketOutcome.iy_draw');
    if (lower === 'away') return t('marketOutcome.iy_away');
  }

  // Second Half Result — İkinci Yarı Sonucu
  if (marketId === 38) {
    if (lower === 'home') return t('marketOutcome.y2_home');
    if (lower === 'draw') return t('marketOutcome.y2_draw');
    if (lower === 'away') return t('marketOutcome.y2_away');
  }

  // Odd / Even Total — Tek / Çift
  if (marketId === 44) {
    if (lower === 'odd') return t('marketOutcome.odd_total');
    if (lower === 'even') return t('marketOutcome.even_total');
  }

  // Unknown market+outcome — fall back to the legacy short form.
  return t('marketOutcome.fallback', {
    market: marketShort(marketId),
    outcome: shortenOutcome(label, marketId),
  });
}
