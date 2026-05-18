// ============================================================================
// CANONICAL MARKET CATALOG
// ----------------------------------------------------------------------------
// Single source of truth for SportMonks market_id ↔ TR/EN short + long labels.
// Locked down with Ethem on 2026-05-16 against İddaa terminology + the
// international betting-market standards. If you need a new market ID
// rendered nicely (short code for the pick column, long name for card
// headers), add it HERE — not in a screen.
//
// Naming rules:
//   * TR Kısa  → İddaa-flavoured Turkish shorthand (MS, ÇŞ, KG, ...)
//   * EN Kısa  → International betting shorthand (FT, DC, BTTS, ...)
//   * TR Uzun  → Full Turkish label used on card headers + Markets sheet.
//   * EN Uzun  → Full English label, same surfaces, EN locale.
//
// Earlier the mobile maps had ID 31 wrongly mapped to "İlk Yarı / Maç Sonu"
// (HT/FT combo) and ID 52 to plain "Çifte Şans". Both were SportMonks ID
// collisions: 31 = HALF_TIME_RESULT (just the half-time result), 29 =
// HALF_TIME_FULL_TIME (combo), 2 = DOUBLE_CHANCE (regular), 52 =
// TEAM_DOUBLE_CHANCE (per-team double-chance). Backend CuratedMarkets.cs
// comments are authoritative for the developer_name column.
// ============================================================================

export interface MarketEntry {
  /** SportMonks market_id (matches odds.markets.id) */
  id: number;
  /** SportMonks developer_name (matches odds.markets.developer_name) */
  developerName: string;
  /** TR shorthand surfaced on the pick column (header column "TAHMİN") */
  shortTr: string;
  /** EN shorthand surfaced when the active locale is English */
  shortEn: string;
  /** TR long label rendered on OddsRatesCard headers + Markets sheet */
  longTr: string;
  /** EN long label, same surfaces, EN locale */
  longEn: string;
}

export const MARKET_CATALOG: readonly MarketEntry[] = [
  { id: 1,   developerName: 'FULLTIME_RESULT',                shortTr: 'MS',          shortEn: 'FT',       longTr: 'Maç Sonucu',                       longEn: 'Full Time Result' },
  { id: 2,   developerName: 'DOUBLE_CHANCE',                  shortTr: 'ÇŞ',          shortEn: 'DC',       longTr: 'Çifte Şans',                       longEn: 'Double Chance' },
  { id: 6,   developerName: 'ASIAN_HANDICAP',                 shortTr: 'AH',          shortEn: 'AH',       longTr: 'Asya Handikap',                    longEn: 'Asian Handicap' },
  { id: 9,   developerName: '3_WAY_HANDICAP',                 shortTr: 'HMS',         shortEn: 'EH',       longTr: 'Handikaplı Maç Sonucu',            longEn: '3-Way Handicap / European Handicap' },
  { id: 10,  developerName: 'DRAW_NO_BET',                    shortTr: 'DNB',         shortEn: 'DNB',      longTr: 'Beraberlikte İade',                longEn: 'Draw No Bet' },
  { id: 13,  developerName: 'RESULT_BOTH_TEAMS_TO_SCORE',     shortTr: 'MS/KG',       shortEn: 'FT/BTTS',  longTr: 'Maç Sonucu ve Karşılıklı Gol',     longEn: 'Match Result and Both Teams to Score' },
  { id: 14,  developerName: 'BOTH_TEAMS_TO_SCORE',            shortTr: 'KG',          shortEn: 'BTTS',     longTr: 'Karşılıklı Gol Var/Yok',           longEn: 'Both Teams to Score' },
  { id: 15,  developerName: 'BOTH_TEAMS_TO_SCORE_IN_1ST_HALF', shortTr: '1Y KG',      shortEn: '1H BTTS',  longTr: 'İlk Yarı Karşılıklı Gol',          longEn: 'Both Teams to Score in 1st Half' },
  { id: 16,  developerName: 'BOTH_TEAMS_TO_SCORE_IN_2ND_HALF', shortTr: '2Y KG',      shortEn: '2H BTTS',  longTr: 'İkinci Yarı Karşılıklı Gol',       longEn: 'Both Teams to Score in 2nd Half' },
  { id: 18,  developerName: 'HOME_TEAM_EXACT_GOALS',          shortTr: 'EV TGS',      shortEn: 'Home EG',  longTr: 'Ev Sahibi Tam Gol Sayısı',         longEn: 'Home Team Exact Goals' },
  { id: 19,  developerName: 'AWAY_TEAM_EXACT_GOALS',          shortTr: 'DEP TGS',     shortEn: 'Away EG',  longTr: 'Deplasman Tam Gol Sayısı',         longEn: 'Away Team Exact Goals' },
  { id: 28,  developerName: '1ST_HALF_GOALS',                 shortTr: '1Y TG',       shortEn: '1H TG',    longTr: 'İlk Yarı Toplam Gol',              longEn: '1st Half Total Goals' },
  { id: 29,  developerName: 'HALF_TIME_FULL_TIME',            shortTr: 'İY/MS',       shortEn: 'HT/FT',    longTr: 'İlk Yarı / Maç Sonucu',            longEn: 'Half Time / Full Time' },
  { id: 30,  developerName: 'HALF_TIME_CORRECT_SCORE',        shortTr: 'İY SKR',      shortEn: 'HT CS',    longTr: 'İlk Yarı Skor',                    longEn: 'Half Time Correct Score' },
  { id: 31,  developerName: 'HALF_TIME_RESULT',               shortTr: 'İY',          shortEn: 'HT',       longTr: 'İlk Yarı Sonucu',                  longEn: 'Half Time Result' },
  { id: 33,  developerName: '1ST_HALF_CORRECT_SCORE',         shortTr: 'İY SKR',      shortEn: '1H CS',    longTr: 'İlk Yarı Kesin Skor',              longEn: '1st Half Correct Score' },
  { id: 35,  developerName: 'AWAY_TEAM_TO_SCORE',             shortTr: 'DEP GOL',     shortEn: 'Away TS',  longTr: 'Deplasman Gol Atar',               longEn: 'Away Team to Score' },
  { id: 36,  developerName: 'HOME_TEAM_TO_SCORE',             shortTr: 'EV GOL',      shortEn: 'Home TS',  longTr: 'Ev Sahibi Gol Atar',               longEn: 'Home Team to Score' },
  { id: 37,  developerName: 'RESULT_TOTAL_GOALS',             shortTr: 'MS/TG',       shortEn: 'FT/TG',    longTr: 'Maç Sonucu ve Toplam Gol',         longEn: 'Match Result and Total Goals' },
  { id: 38,  developerName: '2ND_HALF_CORRECT_SCORE',         shortTr: '2Y SKR',      shortEn: '2H CS',    longTr: 'İkinci Yarı Kesin Skor',           longEn: 'Second Half Correct Score' },
  { id: 44,  developerName: 'ODD_EVEN',                       shortTr: 'T/Ç',         shortEn: 'O/E',      longTr: 'Toplam Gol Tek / Çift',            longEn: 'Total Goals Odd / Even' },
  { id: 45,  developerName: 'ODD_EVEN_1ST_HALF',              shortTr: '1Y T/Ç',      shortEn: '1H O/E',   longTr: 'İlk Yarı Tek / Çift',              longEn: '1st Half Goals Odd / Even' },
  { id: 46,  developerName: 'WIN_TO_NIL',                     shortTr: 'gol yemeden', shortEn: 'Win to Nil', longTr: 'Gol Yemeden Kazanır',            longEn: 'Win to Nil' },
  { id: 50,  developerName: 'CLEAN_SHEET_HOME',               shortTr: 'EV GYM',      shortEn: 'Home CS',  longTr: 'Ev Sahibi Gole Kapatır',           longEn: 'Home Clean Sheet' },
  { id: 51,  developerName: 'CLEAN_SHEET_AWAY',               shortTr: 'DEP GYM',     shortEn: 'Away CS',  longTr: 'Deplasman Gole Kapatır',           longEn: 'Away Clean Sheet' },
  { id: 52,  developerName: 'TEAM_DOUBLE_CHANCE',             shortTr: 'TÇŞ',         shortEn: 'Team DC',  longTr: 'Takım Çifte Şans (Ev/Dep)',        longEn: 'Team Double Chance' },
  { id: 53,  developerName: '2ND_HALF_GOALS',                 shortTr: '2Y TG',       shortEn: '2H TG',    longTr: 'İkinci Yarı Toplam Gol',           longEn: '2nd Half Total Goals' },
  { id: 56,  developerName: 'HANDICAP_RESULT',                shortTr: 'H',           shortEn: 'Hand',     longTr: 'Handikap Sonucu',                  longEn: 'Handicap Result' },
  { id: 57,  developerName: 'CORRECT_SCORE',                  shortTr: 'SKR',         shortEn: 'CS',       longTr: 'Maç Skoru',                        longEn: 'Correct Score' },
  { id: 80,  developerName: 'GOALS_OVER_UNDER',               shortTr: 'A/Ü',         shortEn: 'O/U',      longTr: 'Toplam Gol Alt / Üst',             longEn: 'Total Goals Over / Under' },
  { id: 93,  developerName: 'EXACT_TOTAL_GOALS',              shortTr: 'TGS',         shortEn: 'ETG',      longTr: 'Toplam Gol Sayısı',                longEn: 'Exact Total Goals' },
  { id: 97,  developerName: '2ND_HALF_RESULT',                shortTr: '2Y',          shortEn: '2H',       longTr: 'İkinci Yarı Sonucu',               longEn: '2nd Half Result' },
  { id: 124, developerName: '2ND_HALF_GOALS_ODD_EVEN',        shortTr: '2Y T/Ç',      shortEn: '2H O/E',   longTr: 'İkinci Yarı Tek / Çift',           longEn: '2nd Half Goals Odd / Even' },
  // 81/82/83 are additional total-goals families that SportMonks bundles
  // alongside the canonical 80 (GOALS_OVER_UNDER). 81 ships the same
  // shape as 80 (Over/Under outcomes) but with extended total lines —
  // commonly seen in the OddsRatesCard mid-list. 82 combines totals with
  // BTTS; 83 is a discrete exact-match-goal-count market.
  { id: 81,  developerName: 'ALTERNATIVE_TOTAL_GOALS',         shortTr: 'A/Ü+',        shortEn: 'O/U+',     longTr: 'Alternatif Toplam Gol Alt / Üst',  longEn: 'Alternative Total Goals Over / Under' },
  { id: 82,  developerName: 'TOTAL_GOALS_BOTH_TEAMS_TO_SCORE', shortTr: 'TG+KG',       shortEn: 'TG+BTTS',  longTr: 'Toplam Gol ve Karşılıklı Gol',     longEn: 'Total Goals and Both Teams to Score' },
  { id: 83,  developerName: 'NUMBER_OF_GOALS_IN_MATCH',        shortTr: 'Maç TG',      shortEn: 'Match TG', longTr: 'Maçta Toplam Gol Sayısı',          longEn: 'Number of Goals in Match' },
];

const BY_ID = new Map<number, MarketEntry>(MARKET_CATALOG.map((m) => [m.id, m]));

// Back-compat exports — older screens still import these by name.
// Kept as derived maps over MARKET_CATALOG so we don't double-maintain.
export const MARKET_SHORT: Record<number, string> = Object.fromEntries(
  MARKET_CATALOG.map((m) => [m.id, m.shortTr]),
);

export const MARKET_LONG_TR: Record<number, string> = Object.fromEntries(
  MARKET_CATALOG.map((m) => [m.id, m.longTr]),
);

export const MARKET_LONG_EN: Record<number, string> = Object.fromEntries(
  MARKET_CATALOG.map((m) => [m.id, m.longEn]),
);

// Read the active language from the singleton i18next instance directly.
// Previously this went through `require('i18next')` lazily, but in
// Metro/Hermes the lazy require resolves to a stub that never carries
// the configured language — pick names stayed in Turkish even when the
// user switched the app to English. Static import returns the same
// instance that mobile/src/lib/i18n/index.ts configured at startup, so
// .language reflects the latest changeLanguage() call.
import i18nInstance from 'i18next';

function activeLang(): string {
  return (i18nInstance.language ?? '') as string;
}

export function marketShort(
  marketId: number,
  fallbackName?: string | null,
): string {
  const entry = BY_ID.get(marketId);
  if (!entry) return fallbackName ?? `M${marketId}`;
  return activeLang().toLowerCase().startsWith('en')
    ? entry.shortEn
    : entry.shortTr;
}

/**
 * Long-form market label for card headers, localised to the caller's
 * language. Falls back to the SportMonks-supplied name when we
 * don't have a translation — better to show *something* than to break
 * the header for unmapped markets the worker happens to sync.
 */
export function marketLongName(
  marketId: number,
  lang: string | undefined,
  fallbackName?: string | null,
): string {
  const entry = BY_ID.get(marketId);
  if (!entry) return fallbackName ?? `Market #${marketId}`;
  return lang && lang.toLowerCase().startsWith('tr') ? entry.longTr : entry.longEn;
}

/**
 * Strip exact-goals labels down to their core ("AGF Aarhus - 3+ Goals" → "3+")
 * and translate common 1/X/2-style outcomes for display + coupon storage.
 *
 * Match is case-insensitive — SportMonks ships labels in mixed case
 * across providers; earlier the screen rendered the raw English label
 * because we compared against title-case only. Lowering before compare
 * keeps the conversion robust.
 *
 * Yes/No, Odd/Even, Over/Under are locale-aware (i18n.language). 1/X/2
 * stays language-neutral — the same codes are used in every betting
 * market vocabulary worldwide.
 *
 * Market id coverage matches the canonical 33-entry MARKET_CATALOG.
 */
export function shortenOutcome(
  label: string,
  marketId: number,
  homeName?: string | null,
  awayName?: string | null,
): string {
  if (marketId === 18 || marketId === 19) {
    const dash = label.lastIndexOf(' - ');
    const tail = dash >= 0 ? label.slice(dash + 3) : label;
    return tail.replace(/\s+Goals?$/i, '');
  }
  if (marketId === 33 || marketId === 38) {
    return label.replace(/\s+Goals?$/i, '');
  }
  const lower = label.toLowerCase();
  const en = activeLang().toLowerCase().startsWith('en');
  // Fulltime 1X2 + half-time 1X2 — neutral codes (international standard).
  if (marketId === 1 || marketId === 31) {
    if (lower === 'home') return '1';
    if (lower === 'draw') return 'X';
    if (lower === 'away') return '2';
  }
  // BTTS + per-side "team to score" + clean-sheet + win-to-nil all use Yes/No.
  if (marketId === 14 || marketId === 15 || marketId === 16
      || marketId === 35 || marketId === 36 || marketId === 46
      || marketId === 50 || marketId === 51) {
    if (lower === 'yes') return en ? 'Yes' : 'Var';
    if (lower === 'no') return en ? 'No' : 'Yok';
  }
  // Odd/Even — full match, 1st half, 2nd half all share the mapping.
  if (marketId === 44 || marketId === 45 || marketId === 124) {
    if (lower === 'odd') return en ? 'Odd' : 'Tek';
    if (lower === 'even') return en ? 'Even' : 'Çift';
  }
  // Goals Over/Under — locale-aware. Covers every market that ships
  // bare "Over"/"Under" labels: full-match totals (80, 81), per-half
  // totals (28 1st half, 53 2nd half), exact-total and number-of-
  // goals families (83, 93), and the three-way (17). Adding a new
  // O/U market id here keeps the locale logic in one place.
  if (marketId === 80 || marketId === 81 || marketId === 28
      || marketId === 53 || marketId === 83 || marketId === 93
      || marketId === 17) {
    if (lower === 'over') return en ? 'Over' : 'Üst';
    if (lower === 'under') return en ? 'Under' : 'Alt';
  }
  // Handicap markets — replace the generic team identifier ("1" /
  // "2" / "Home" / "Away") with EV / DEP (or Home / Away in EN) so
  // the row reads "H DEP +1" instead of "H 2 +1". Without this swap
  // users have to remember which side of the handicap the number
  // refers to, which empirically flips in 50% of mental models.
  // Covers ASIAN_HANDICAP (6), 3_WAY_HANDICAP (9), HANDICAP_RESULT
  // (56) and their 1st/2nd half variants.
  if (marketId === 6 || marketId === 9 || marketId === 56) {
    if (lower === '1' || lower === 'home') return en ? 'Home' : 'EV';
    if (lower === '2' || lower === 'away') return en ? 'Away' : 'DEP';
    if (lower === 'x' || lower === 'draw') return en ? 'Draw' : 'Beraberlik';
  }
  // Double Chance + Team Double Chance — universal compact codes used on
  // Turkish bet slips and most European bookmakers. SportMonks ships
  // labels in two shapes — generic ("Home/Draw", "Home or Draw") and
  // team-specific ("Arka Gdynia or Draw", "Draw or Termalica BB
  // Nieciecza"). The team-specific shape requires home/away names to
  // disambiguate which side is "1" and which is "2".
  if (marketId === 2 || marketId === 52) {
    // Generic forms — no team names needed.
    if (lower === 'home/draw' || lower === 'home or draw') return '1X';
    if (lower === 'home/away' || lower === 'home or away') return '12';
    if (lower === 'draw/away' || lower === 'draw or away') return 'X2';
    // Team-specific form: "<X> or <Y>" with one of X / Y being a
    // team name and the other being either "Draw" or the other team.
    const orMatch = lower.split(/\s+or\s+/);
    if (orMatch.length === 2 && homeName && awayName) {
      const h = homeName.toLowerCase();
      const a = awayName.toLowerCase();
      const [left, right] = [orMatch[0].trim(), orMatch[1].trim()];
      // <Home> or Draw / Draw or <Home> → 1X
      if ((left === h && right === 'draw') || (right === h && left === 'draw')) {
        return '1X';
      }
      // <Away> or Draw / Draw or <Away> → X2
      if ((left === a && right === 'draw') || (right === a && left === 'draw')) {
        return 'X2';
      }
      // <Home> or <Away> / <Away> or <Home> → 12
      if ((left === h && right === a) || (left === a && right === h)) {
        return '12';
      }
    }
  }
  return label;
}
