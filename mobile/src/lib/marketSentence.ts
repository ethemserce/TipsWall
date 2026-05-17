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

  // HT/FT — İlk Yarı / Maç Sonu (SportMonks id 29, not 31)
  if (marketId === 29) {
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

  // Half-Time Result — İlk Yarı Sonucu (SportMonks id 31)
  if (marketId === 31) {
    if (lower === 'home') return t('marketOutcome.iy_home');
    if (lower === 'draw') return t('marketOutcome.iy_draw');
    if (lower === 'away') return t('marketOutcome.iy_away');
  }

  // Second-Half Result — İkinci Yarı Sonucu (SportMonks id 97)
  if (marketId === 97) {
    if (lower === 'home') return t('marketOutcome.y2_home');
    if (lower === 'draw') return t('marketOutcome.y2_draw');
    if (lower === 'away') return t('marketOutcome.y2_away');
  }

  // BTTS halves (15: 1H BTTS, 16: 2H BTTS)
  if (marketId === 15 || marketId === 16) {
    const half = marketId === 15 ? '1y' : '2y';
    if (lower === 'yes') return t(`marketOutcome.btts_${half}_yes`, { defaultValue: marketId === 15 ? 'İlk yarı karşılıklı gol' : 'İkinci yarı karşılıklı gol' });
    if (lower === 'no') return t(`marketOutcome.btts_${half}_no`, { defaultValue: marketId === 15 ? 'İlk yarıda iki taraf birlikte gol atamaz' : 'İkinci yarıda iki taraf birlikte gol atamaz' });
  }

  // Double Chance — Çifte Şans
  if (marketId === 2) {
    const combo = label.replace(/\s+/g, '').toLowerCase();
    if (combo === 'home/draw' || combo === 'homeordraw' || combo === '1x') return t('marketOutcome.dc_1x', { defaultValue: 'Ev sahibi kazanır veya berabere' });
    if (combo === 'home/away' || combo === 'homeoraway' || combo === '12') return t('marketOutcome.dc_12', { defaultValue: 'Beraberlik dışı sonuç' });
    if (combo === 'draw/away' || combo === 'draworaway' || combo === 'x2') return t('marketOutcome.dc_x2', { defaultValue: 'Deplasman kazanır veya berabere' });
  }

  // BTTS + Result combo (id 13) — 'home/yes' tarzı combo
  if (marketId === 13) {
    const combo = label.replace(/[\s-]/g, '').toLowerCase();
    if (combo === 'home/yes') return t('marketOutcome.mskg_1_var', { defaultValue: 'Ev kazanır, karşılıklı gol var' });
    if (combo === 'draw/yes') return t('marketOutcome.mskg_x_var', { defaultValue: 'Berabere, karşılıklı gol var' });
    if (combo === 'away/yes') return t('marketOutcome.mskg_2_var', { defaultValue: 'Deplasman kazanır, karşılıklı gol var' });
    if (combo === 'home/no') return t('marketOutcome.mskg_1_yok', { defaultValue: 'Ev kazanır, karşılıklı gol yok' });
    if (combo === 'draw/no') return t('marketOutcome.mskg_x_yok', { defaultValue: 'Berabere, karşılıklı gol yok' });
    if (combo === 'away/no') return t('marketOutcome.mskg_2_yok', { defaultValue: 'Deplasman kazanır, karşılıklı gol yok' });
  }

  // Home Team To Score / Away Team To Score (35, 36)
  if (marketId === 35 || marketId === 36) {
    const side = marketId === 36 ? 'home' : 'away';
    if (lower === 'yes') return t(`marketOutcome.${side}_scores_yes`, { defaultValue: side === 'home' ? 'Ev sahibi gol atar' : 'Deplasman gol atar' });
    if (lower === 'no') return t(`marketOutcome.${side}_scores_no`, { defaultValue: side === 'home' ? 'Ev sahibi gol atamaz' : 'Deplasman gol atamaz' });
  }

  // Clean Sheet Home / Away (50, 51)
  if (marketId === 50 || marketId === 51) {
    const side = marketId === 50 ? 'home' : 'away';
    if (lower === 'yes') return t(`marketOutcome.${side}_clean_yes`, { defaultValue: side === 'home' ? 'Ev sahibi gol yemez' : 'Deplasman gol yemez' });
    if (lower === 'no') return t(`marketOutcome.${side}_clean_no`, { defaultValue: side === 'home' ? 'Ev sahibi gol yer' : 'Deplasman gol yer' });
  }

  // First-half goals over/under (id 28) — same shape as id 80 but for 1H
  if (marketId === 28 && total) {
    const totalNum = Number.parseFloat(total);
    if (Number.isFinite(totalNum)) {
      const overGoals = Math.ceil(totalNum);
      const underGoals = Math.floor(totalNum);
      if (lower === 'over') return t('marketOutcome.over_1y', { goals: overGoals, defaultValue: `İlk yarıda ${overGoals}+ gol` });
      if (lower === 'under') return t('marketOutcome.under_1y', { goals: underGoals, defaultValue: `İlk yarıda en çok ${underGoals} gol` });
    }
  }

  // Second-half goals over/under (id 53)
  if (marketId === 53 && total) {
    const totalNum = Number.parseFloat(total);
    if (Number.isFinite(totalNum)) {
      const overGoals = Math.ceil(totalNum);
      const underGoals = Math.floor(totalNum);
      if (lower === 'over') return t('marketOutcome.over_2y', { goals: overGoals, defaultValue: `İkinci yarıda ${overGoals}+ gol` });
      if (lower === 'under') return t('marketOutcome.under_2y', { goals: underGoals, defaultValue: `İkinci yarıda en çok ${underGoals} gol` });
    }
  }

  // Odd/Even halves (45 = 1H, 124 = 2H)
  if (marketId === 45 || marketId === 124) {
    const half = marketId === 45 ? '1y' : '2y';
    if (lower === 'odd') return t(`marketOutcome.odd_${half}`, { defaultValue: marketId === 45 ? 'İlk yarı toplam gol tek' : 'İkinci yarı toplam gol tek' });
    if (lower === 'even') return t(`marketOutcome.even_${half}`, { defaultValue: marketId === 45 ? 'İlk yarı toplam gol çift' : 'İkinci yarı toplam gol çift' });
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
