// ============================================================================
// CANONICAL MARKET CATALOG
// ----------------------------------------------------------------------------
// Single source of truth for SportMonks market_id ↔ TR/EN short + long labels.
// Locked down with Ethem on 2026-05-16 against İddaa terminology + the
// international betting-market standards. Extended on 2026-05-19 from the
// full SportMonks /v3/odds/markets payload — covers every active market id
// (1–340 range). If you need a new market ID rendered nicely (short code
// for the pick column, long name for card headers), add it HERE — not in
// a screen.
//
// Naming rules:
//   * TR Kısa  → İddaa-flavoured Turkish shorthand (MS, ÇŞ, KG, ...)
//   * EN Kısa  → International betting shorthand (FT, DC, BTTS, ...)
//   * TR Uzun  → Full Turkish label used on card headers + Markets sheet.
//   * EN Uzun  → Full English label, same surfaces, EN locale.
//
// Three earlier developer_name typos were corrected on 2026-05-19 against
// the authoritative SportMonks market list:
//   * id 33 was '1ST_HALF_CORRECT_SCORE' — actual is FIRST_HALF_EXACT_GOALS
//   * id 38 was '2ND_HALF_CORRECT_SCORE' — actual is SECOND_HALF_EXACT_GOALS
//   * id 52 was 'TEAM_DOUBLE_CHANCE'     — actual is HOME_AWAY (no draw)
// The visible TR/EN labels for 33/38 were already aligned with "exact
// goals" rendering, so only the developer_name strings changed. id 52
// label flipped from "Çifte Şans" to "Ev / Deplasman (Beraberliksiz)".
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
  // ── 1X2 / Result family ──────────────────────────────────────────────
  { id: 1,   developerName: 'FULLTIME_RESULT',                          shortTr: 'MS',          shortEn: 'FT',          longTr: 'Maç Sonucu',                       longEn: 'Full Time Result' },
  { id: 31,  developerName: 'HALF_TIME_RESULT',                         shortTr: 'İY',          shortEn: 'HT',          longTr: 'İlk Yarı Sonucu',                  longEn: 'Half Time Result' },
  { id: 97,  developerName: '2ND_HALF_RESULT',                          shortTr: '2Y',          shortEn: '2H',          longTr: 'İkinci Yarı Sonucu',               longEn: '2nd Half Result' },
  { id: 29,  developerName: 'HALF_TIME_FULL_TIME',                      shortTr: 'İY/MS',       shortEn: 'HT/FT',       longTr: 'İlk Yarı / Maç Sonucu',            longEn: 'Half Time / Full Time' },
  { id: 49,  developerName: 'HT_FT_DOUBLE',                             shortTr: 'İY/MS+',      shortEn: 'HT/FT+',      longTr: 'İlk Yarı / Maç Sonucu (Geniş)',    longEn: 'HT/FT Double' },
  { id: 52,  developerName: 'HOME_AWAY',                                shortTr: 'EV/DEP',      shortEn: 'H/A',         longTr: 'Ev Sahibi / Deplasman',            longEn: 'Home / Away (No Draw)' },
  { id: 89,  developerName: 'FULL_TIME_RESULT_ENHANCHED_PRICES',        shortTr: 'MS+',         shortEn: 'FT+',         longTr: 'Maç Sonucu - Geliştirilmiş Oran',  longEn: 'FT Result - Enhanced Prices' },
  { id: 22,  developerName: 'TO_WIN_1ST_HALF',                          shortTr: '1Y Kazanır',  shortEn: 'Win 1H',      longTr: '1. Yarıyı Kazanır',                longEn: 'To Win 1st Half' },
  { id: 23,  developerName: 'TO_WIN_2ND_HALF',                          shortTr: '2Y Kazanır',  shortEn: 'Win 2H',      longTr: '2. Yarıyı Kazanır',                longEn: 'To Win 2nd Half' },
  { id: 40,  developerName: 'TO_WIN_BOTH_HALVES',                       shortTr: '2 Yarı K',    shortEn: 'Win Both',    longTr: 'İki Yarıyı da Kazanır',            longEn: 'To Win Both Halves' },
  { id: 41,  developerName: 'HOME_TEAM_WIN_BOTH_HALVES',                shortTr: 'EV 2 Yarı K', shortEn: 'Home Both',   longTr: 'Ev Sahibi İki Yarıyı da Kazanır',  longEn: 'Home Team Win Both Halves' },
  { id: 39,  developerName: 'AWAY_TEAM_WIN_BOTH_HALVES',                shortTr: 'DEP 2 Yarı K',shortEn: 'Away Both',   longTr: 'Deplasman İki Yarıyı da Kazanır',  longEn: 'Away Team Win Both Halves' },
  { id: 266, developerName: 'TO_WIN_EITHER_HALF',                       shortTr: 'Yarı K',      shortEn: 'Win Either',  longTr: 'Yarılardan Birini Kazanır',        longEn: 'To Win Either Half' },
  { id: 34,  developerName: 'FIRST_10_MIN_WINNER',                      shortTr: 'İlk 10 K',    shortEn: 'First 10 W',  longTr: 'İlk 10 Dakika Kazananı',           longEn: 'First 10 Minutes Winner' },
  { id: 91,  developerName: 'TEN_MINUTE_RESULT',                        shortTr: '10 Dk MS',    shortEn: '10 Min',      longTr: '10 Dakika Sonucu',                 longEn: '10 Minute Result' },
  { id: 101, developerName: 'HALF_WITH_MOST_GOALS',                     shortTr: 'Gollü Yarı',  shortEn: 'Top Half',    longTr: 'En Çok Gollü Yarı',                longEn: 'Half with Most Goals' },
  { id: 120, developerName: 'HOME_TEAM_HIGHEST_SCORING_HALF',           shortTr: 'EV Gollü Y',  shortEn: 'Home Top H',  longTr: 'Ev Sahibi En Gollü Yarısı',        longEn: 'Home Team Highest Scoring Half' },
  { id: 121, developerName: 'AWAY_TEAM_HIGHEST_SCORING_HALF',           shortTr: 'DEP Gollü Y', shortEn: 'Away Top H',  longTr: 'Deplasman En Gollü Yarısı',        longEn: 'Away Team Highest Scoring Half' },

  // ── Double Chance ────────────────────────────────────────────────────
  { id: 2,   developerName: 'DOUBLE_CHANCE',                            shortTr: 'ÇŞ',          shortEn: 'DC',          longTr: 'Çifte Şans',                       longEn: 'Double Chance' },
  { id: 47,  developerName: 'DOUBLE_CHANGE_1ST_HALF',                   shortTr: '1Y ÇŞ',       shortEn: '1H DC',       longTr: 'İlk Yarı Çifte Şans',              longEn: 'Double Chance - 1st Half' },
  { id: 48,  developerName: 'DOUBLE_CHANGE_2ND_HALF',                   shortTr: '2Y ÇŞ',       shortEn: '2H DC',       longTr: 'İkinci Yarı Çifte Şans',           longEn: 'Double Chance - 2nd Half' },

  // ── Draw No Bet ──────────────────────────────────────────────────────
  { id: 10,  developerName: 'DRAW_NO_BET',                              shortTr: 'DNB',         shortEn: 'DNB',         longTr: 'Beraberlikte İade',                longEn: 'Draw No Bet' },
  { id: 305, developerName: 'DRAW_NO_BET_1ST_HALF',                     shortTr: '1Y DNB',      shortEn: '1H DNB',      longTr: '1. Yarı Beraberlikte İade',        longEn: 'Draw No Bet 1st Half' },
  { id: 306, developerName: 'DRAW_NO_BET_2ND_HALF',                     shortTr: '2Y DNB',      shortEn: '2H DNB',      longTr: '2. Yarı Beraberlikte İade',        longEn: 'Draw No Bet 2nd Half' },

  // ── Asian Handicap ───────────────────────────────────────────────────
  { id: 6,   developerName: 'ASIAN_HANDICAP',                           shortTr: 'AH',          shortEn: 'AH',          longTr: 'Asya Handikap',                    longEn: 'Asian Handicap' },
  { id: 26,  developerName: '1ST_HALF_ASIAN_HANDICAP',                  shortTr: '1Y AH',       shortEn: '1H AH',       longTr: 'İlk Yarı Asya Handikap',           longEn: '1st Half Asian Handicap' },
  { id: 303, developerName: '2ND_HALF_ASIAN_HANDICAP',                  shortTr: '2Y AH',       shortEn: '2H AH',       longTr: '2. Yarı Asya Handikap',            longEn: '2nd Half Asian Handicap' },
  { id: 104, developerName: 'ALTERNATIVE_ASIAN_HANDICAP',               shortTr: 'AH+',         shortEn: 'AH+',         longTr: 'Alternatif Asya Handikap',         longEn: 'Alternative Asian Handicap' },
  { id: 106, developerName: 'ALTERNATIVE_1ST_HALF_ASIAN_HANDICAP',      shortTr: '1Y AH+',      shortEn: '1H AH+',      longTr: 'Alt. 1. Yarı Asya Handikap',       longEn: 'Alt. 1st Half Asian Handicap' },

  // ── European / 3-Way Handicap ────────────────────────────────────────
  { id: 9,   developerName: '3_WAY_HANDICAP',                           shortTr: 'HMS',         shortEn: 'EH',          longTr: 'Handikaplı Maç Sonucu',            longEn: '3-Way Handicap / European Handicap' },
  { id: 309, developerName: '3_WAY_HANDICAP_1ST_HALF',                  shortTr: '1Y HMS',      shortEn: '1H EH',       longTr: '1. Yarı Handikaplı Sonuç',         longEn: '3-Way Handicap 1st Half' },
  { id: 310, developerName: '3_WAY_HANDICAP_2ND_HALF',                  shortTr: '2Y HMS',      shortEn: '2H EH',       longTr: '2. Yarı Handikaplı Sonuç',         longEn: '3-Way Handicap 2nd Half' },

  // ── Handicap Result + Alternative ────────────────────────────────────
  { id: 56,  developerName: 'HANDICAP_RESULT',                          shortTr: 'H',           shortEn: 'Hand',        longTr: 'Handikap Sonucu',                  longEn: 'Handicap Result' },
  { id: 32,  developerName: '1ST_HALF_HANDICAP',                        shortTr: '1Y H',        shortEn: '1H Hand',     longTr: '1. Yarı Handikap',                 longEn: '1st Half Handicap' },
  { id: 94,  developerName: 'ALTERNATIVE_HANDICAP_RESULT',              shortTr: 'H+',          shortEn: 'Hand+',       longTr: 'Alternatif Handikap Sonucu',       longEn: 'Alternative Handicap Result' },
  { id: 96,  developerName: 'ALTERNATIVE_1ST_HALF_HANDICAP_RESULT',     shortTr: '1Y H+',       shortEn: '1H Hand+',    longTr: 'Alt. 1. Yarı Handikap',            longEn: 'Alt. 1st Half Handicap Result' },

  // ── Goal Line ────────────────────────────────────────────────────────
  { id: 7,   developerName: 'GOAL_LINE',                                shortTr: 'GÇ',          shortEn: 'GL',          longTr: 'Gol Çizgisi',                      longEn: 'Goal Line' },
  { id: 27,  developerName: '1ST_HALF_GOAL_LINE',                       shortTr: '1Y GÇ',       shortEn: '1H GL',       longTr: 'İlk Yarı Gol Çizgisi',             longEn: '1st Half Goal Line' },
  { id: 105, developerName: 'ALTERNATIVE_GOAL_LINE',                    shortTr: 'GÇ+',         shortEn: 'GL+',         longTr: 'Alternatif Gol Çizgisi',           longEn: 'Alternative Goal Line' },
  { id: 107, developerName: 'ALTERNATIVE_1ST_HALF_GOAL_LINE',           shortTr: '1Y GÇ+',      shortEn: '1H GL+',      longTr: 'Alt. 1. Yarı Gol Çizgisi',         longEn: 'Alt. 1st Half Goal Line' },

  // ── Both Teams to Score ──────────────────────────────────────────────
  { id: 14,  developerName: 'BOTH_TEAMS_TO_SCORE',                      shortTr: 'KG',          shortEn: 'BTTS',        longTr: 'Karşılıklı Gol Var/Yok',           longEn: 'Both Teams to Score' },
  { id: 15,  developerName: 'BOTH_TEAMS_TO_SCORE_IN_1ST_HALF',          shortTr: '1Y KG',       shortEn: '1H BTTS',     longTr: 'İlk Yarı Karşılıklı Gol',          longEn: 'Both Teams to Score in 1st Half' },
  { id: 16,  developerName: 'BOTH_TEAMS_TO_SCORE_IN_2ND_HALF',          shortTr: '2Y KG',       shortEn: '2H BTTS',     longTr: 'İkinci Yarı Karşılıklı Gol',       longEn: 'Both Teams to Score in 2nd Half' },
  { id: 13,  developerName: 'RESULT_BOTH_TEAMS_TO_SCORE',               shortTr: 'MS/KG',       shortEn: 'FT/BTTS',     longTr: 'Maç Sonucu ve Karşılıklı Gol',     longEn: 'Match Result and Both Teams to Score' },
  { id: 122, developerName: 'HALF_TIME_RESULT_BOTH_TEAM_TO_SCORE',      shortTr: 'İY/KG',       shortEn: 'HT/BTTS',     longTr: 'İlk Yarı Sonucu ve Karşılıklı Gol',longEn: 'Half Time Result + Both Teams to Score' },
  { id: 125, developerName: 'BOTH_TEAM_TO_SCORE_1ST_HALF_2ND_HALF',     shortTr: 'KG 1Y/2Y',    shortEn: 'BTTS 1H/2H',  longTr: 'KG - 1. Yarı / 2. Yarı',           longEn: 'Both Teams to Score 1st Half / 2nd Half' },

  // ── Result + Total Goals combo ──────────────────────────────────────
  { id: 37,  developerName: 'RESULT_TOTAL_GOALS',                       shortTr: 'MS/TG',       shortEn: 'FT/TG',       longTr: 'Maç Sonucu ve Toplam Gol',         longEn: 'Match Result and Total Goals' },
  { id: 123, developerName: 'HALF_TIME_RESULT_TOTAL_GOALS',             shortTr: 'İY/TG',       shortEn: 'HT/TG',       longTr: 'İlk Yarı Sonucu ve Toplam Gol',    longEn: 'Half Time Result + Total Goals' },

  // ── Team to Score (full match + per-half) ───────────────────────────
  { id: 36,  developerName: 'HOME_TEAM_TO_SCORE',                       shortTr: 'EV GOL',      shortEn: 'Home TS',     longTr: 'Ev Sahibi Gol Atar',               longEn: 'Home Team to Score' },
  { id: 35,  developerName: 'AWAY_TEAM_TO_SCORE',                       shortTr: 'DEP GOL',     shortEn: 'Away TS',     longTr: 'Deplasman Gol Atar',               longEn: 'Away Team to Score' },
  { id: 24,  developerName: 'TEAM_TO_SCORE_IN_1ST_HALF',                shortTr: 'Takım 1Y Gol',shortEn: 'Team 1H Sc',  longTr: '1. Yarıda Takım Gol Atar',         longEn: 'Team to Score in 1st Half' },
  { id: 25,  developerName: 'TEAM_TO_SCORE_IN_2ND_HALF',                shortTr: 'Takım 2Y Gol',shortEn: 'Team 2H Sc',  longTr: '2. Yarıda Takım Gol Atar',         longEn: 'Team to Score in 2nd Half' },
  { id: 248, developerName: 'TEAM_TO_SCORE_IN_BOTH_HALVES',             shortTr: '2 Yarı Gol',  shortEn: 'Score Both',  longTr: 'Her İki Yarıda da Gol Atar',       longEn: 'Team to Score in Both Halves' },
  { id: 247, developerName: 'FIRST_TEAM_TO_SCORE',                      shortTr: 'İlk Gol',     shortEn: 'First Score', longTr: 'İlk Golü Atan Takım',              longEn: 'First Team to Score' },
  { id: 11,  developerName: 'LAST_TEAM_TO_SCORE',                       shortTr: 'Son Gol',     shortEn: 'Last Score',  longTr: 'Son Golü Atan Takım',              longEn: 'Last Team to Score' },
  { id: 88,  developerName: 'TO_SCORE_IN_HALF',                         shortTr: 'Yarıda Gol',  shortEn: 'Score Half',  longTr: 'Yarıda Gol Atar',                  longEn: 'To Score in Half' },
  { id: 98,  developerName: 'TEAMS_TO_SCORE',                           shortTr: 'Gol Atanlar', shortEn: 'Teams Score', longTr: 'Gol Atan Takımlar',                longEn: 'Teams to Score' },
  { id: 253, developerName: 'TO_SCORE_3_OR_MORE_GOALS',                 shortTr: '3+ Gol',      shortEn: '3+ Goals',    longTr: '3 veya Daha Fazla Gol Atar',       longEn: 'To Score 3 or More Goals' },

  // ── Clean Sheet + Win to Nil ────────────────────────────────────────
  { id: 17,  developerName: 'TEAM_CLEAN_SHEET',                         shortTr: 'GYM',         shortEn: 'CS',          longTr: 'Takım Gol Yemez',                  longEn: 'Team Clean Sheet' },
  { id: 50,  developerName: 'CLEAN_SHEET_HOME',                         shortTr: 'EV GYM',      shortEn: 'Home CS',     longTr: 'Ev Sahibi Gole Kapatır',           longEn: 'Home Clean Sheet' },
  { id: 51,  developerName: 'CLEAN_SHEET_AWAY',                         shortTr: 'DEP GYM',     shortEn: 'Away CS',     longTr: 'Deplasman Gole Kapatır',           longEn: 'Away Clean Sheet' },
  { id: 46,  developerName: 'WIN_TO_NIL',                               shortTr: 'gol yemeden', shortEn: 'Win to Nil',  longTr: 'Gol Yemeden Kazanır',              longEn: 'Win to Nil' },
  { id: 54,  developerName: 'WIN_TO_NIL_HOME',                          shortTr: 'EV GYK',      shortEn: 'Home WTN',    longTr: 'Ev Sahibi Gol Yemeden Kazanır',    longEn: 'Win to Nil - Home' },
  { id: 55,  developerName: 'WIN_TO_NIL_AWAY',                          shortTr: 'DEP GYK',     shortEn: 'Away WTN',    longTr: 'Deplasman Gol Yemeden Kazanır',    longEn: 'Win to Nil - Away' },

  // ── Total Goals / Over-Under ────────────────────────────────────────
  { id: 80,  developerName: 'GOALS_OVER_UNDER',                         shortTr: 'A/Ü',         shortEn: 'O/U',         longTr: 'Toplam Gol Alt / Üst',             longEn: 'Total Goals Over / Under' },
  { id: 81,  developerName: 'ALTERNATIVE_TOTAL_GOALS',                  shortTr: 'A/Ü+',        shortEn: 'O/U+',        longTr: 'Alternatif Toplam Gol Alt / Üst',  longEn: 'Alternative Total Goals Over / Under' },
  { id: 4,   developerName: 'MATCH_GOALS',                              shortTr: 'Maç Gol',     shortEn: 'Match G',     longTr: 'Maç Gol Sayısı',                   longEn: 'Match Goals' },
  { id: 5,   developerName: 'ALTERNATIVE_MATCH_GOALS',                  shortTr: 'Maç Gol+',    shortEn: 'Match G+',    longTr: 'Alternatif Maç Gol Sayısı',        longEn: 'Alternative Match Goals' },
  { id: 28,  developerName: '1ST_HALF_GOALS',                           shortTr: '1Y TG',       shortEn: '1H TG',       longTr: 'İlk Yarı Toplam Gol',              longEn: '1st Half Total Goals' },
  { id: 53,  developerName: '2ND_HALF_GOALS',                           shortTr: '2Y TG',       shortEn: '2H TG',       longTr: 'İkinci Yarı Toplam Gol',           longEn: '2nd Half Total Goals' },
  { id: 82,  developerName: 'TOTAL_GOALS_BOTH_TEAMS_TO_SCORE',          shortTr: 'TG+KG',       shortEn: 'TG+BTTS',     longTr: 'Toplam Gol ve Karşılıklı Gol',     longEn: 'Total Goals and Both Teams to Score' },
  { id: 83,  developerName: 'NUMBER_OF_GOALS_IN_MATCH',                 shortTr: 'Maç TG',      shortEn: 'Match TG',    longTr: 'Maçta Toplam Gol Sayısı',          longEn: 'Number of Goals in Match' },
  { id: 93,  developerName: 'EXACT_TOTAL_GOALS',                        shortTr: 'TGS',         shortEn: 'ETG',         longTr: 'Toplam Gol Sayısı',                longEn: 'Exact Total Goals' },
  { id: 86,  developerName: 'TEAM_TOTAL_GOALS',                         shortTr: 'Takım TG',    shortEn: 'Team TG',     longTr: 'Takım Toplam Gol',                 longEn: 'Team Total Goals' },
  { id: 20,  developerName: 'HOME_TEAM_GOALS',                          shortTr: 'EV Gol',      shortEn: 'Home G',      longTr: 'Ev Sahibi Gol Sayısı',             longEn: 'Home Team Goals' },
  { id: 21,  developerName: 'AWAY_TEAM_GOALS',                          shortTr: 'DEP Gol',     shortEn: 'Away G',      longTr: 'Deplasman Gol Sayısı',             longEn: 'Away Team Goals' },
  { id: 18,  developerName: 'HOME_TEAM_EXACT_GOALS',                    shortTr: 'EV TGS',      shortEn: 'Home EG',     longTr: 'Ev Sahibi Tam Gol Sayısı',         longEn: 'Home Team Exact Goals' },
  { id: 19,  developerName: 'AWAY_TEAM_EXACT_GOALS',                    shortTr: 'DEP TGS',     shortEn: 'Away EG',     longTr: 'Deplasman Tam Gol Sayısı',         longEn: 'Away Team Exact Goals' },
  { id: 33,  developerName: 'FIRST_HALF_EXACT_GOALS',                   shortTr: '1Y TGS',      shortEn: '1H EG',       longTr: 'İlk Yarı Tam Gol Sayısı',          longEn: '1st Half Exact Goals' },
  { id: 38,  developerName: 'SECOND_HALF_EXACT_GOALS',                  shortTr: '2Y TGS',      shortEn: '2H EG',       longTr: '2. Yarı Tam Gol Sayısı',           longEn: '2nd Half Exact Goals' },
  { id: 249, developerName: 'TOTAL_GOALS_3_WAY',                        shortTr: 'TG 3Yön',     shortEn: 'TG 3-Way',    longTr: 'Toplam Gol - 3 Yönlü',             longEn: 'Total Goals - 3 Way' },
  { id: 3,   developerName: 'X_GOAL',                                   shortTr: 'X Gol',       shortEn: 'X Goal',      longTr: 'X Gol',                            longEn: 'X Goal' },
  { id: 87,  developerName: 'FIRST_10_MINUTES_GOALS',                   shortTr: 'İlk 10 Gol',  shortEn: 'First 10 G',  longTr: 'İlk 10 Dakika Gol',                longEn: 'First 10 Minutes Goals' },

  // ── Odd / Even ──────────────────────────────────────────────────────
  { id: 44,  developerName: 'ODD_EVEN',                                 shortTr: 'T/Ç',         shortEn: 'O/E',         longTr: 'Toplam Gol Tek / Çift',            longEn: 'Total Goals Odd / Even' },
  { id: 12,  developerName: 'GOALS_ODD_EVEN',                           shortTr: 'Gol T/Ç',     shortEn: 'Goals O/E',   longTr: 'Goller Tek / Çift',                longEn: 'Goals Odd / Even' },
  { id: 45,  developerName: 'ODD_EVEN_1ST_HALF',                        shortTr: '1Y T/Ç',      shortEn: '1H O/E',      longTr: 'İlk Yarı Tek / Çift',              longEn: '1st Half Goals Odd / Even' },
  { id: 95,  developerName: '1ST_HALF_GOALS_ODD_EVEN',                  shortTr: '1Y G T/Ç',    shortEn: '1H G O/E',    longTr: '1. Yarı Goller Tek / Çift',        longEn: '1st Half Goals Odd / Even' },
  { id: 124, developerName: '2ND_HALF_GOALS_ODD_EVEN',                  shortTr: '2Y T/Ç',      shortEn: '2H O/E',      longTr: 'İkinci Yarı Tek / Çift',           longEn: '2nd Half Goals Odd / Even' },
  { id: 304, developerName: 'ODD_EVEN_2ND_HALF',                        shortTr: '2Y T/Ç*',     shortEn: '2H O/E*',     longTr: '2. Yarı Tek / Çift',               longEn: '2nd Half Odd / Even' },
  { id: 42,  developerName: 'HOME_ODD_EVEN',                            shortTr: 'EV T/Ç',      shortEn: 'Home O/E',    longTr: 'Ev Sahibi Tek / Çift',             longEn: 'Home Odd / Even' },
  { id: 43,  developerName: 'AWAY_ODD_EVEN',                            shortTr: 'DEP T/Ç',     shortEn: 'Away O/E',    longTr: 'Deplasman Tek / Çift',             longEn: 'Away Odd / Even' },

  // ── Correct Score ───────────────────────────────────────────────────
  { id: 57,  developerName: 'CORRECT_SCORE',                            shortTr: 'SKR',         shortEn: 'CS',          longTr: 'Maç Skoru',                        longEn: 'Correct Score' },
  { id: 30,  developerName: 'HALF_TIME_CORRECT_SCORE',                  shortTr: 'İY SKR',      shortEn: 'HT CS',       longTr: 'İlk Yarı Skor',                    longEn: 'Half Time Correct Score' },
  { id: 58,  developerName: 'CORRECT_SCORE_1ST_HALF',                   shortTr: '1Y SKR',      shortEn: '1H CS',       longTr: '1. Yarı Kesin Skor',               longEn: 'Correct Score 1st Half' },
  { id: 59,  developerName: 'CORRECT_SCORE_2ND_HALF',                   shortTr: '2Y SKR',      shortEn: '2H CS',       longTr: '2. Yarı Kesin Skor',               longEn: 'Correct Score 2nd Half' },
  { id: 8,   developerName: 'FINAL_SCORE',                              shortTr: 'Final Skor',  shortEn: 'Final Score', longTr: 'Final Skor',                       longEn: 'Final Score' },

  // ── Time of Goal / Scorer / Method ──────────────────────────────────
  { id: 84,  developerName: 'EARLY_GOAL',                               shortTr: 'Erken Gol',   shortEn: 'Early Goal',  longTr: 'Erken Gol',                        longEn: 'Early Goal' },
  { id: 85,  developerName: 'LATE_GOAL',                                shortTr: 'Geç Gol',     shortEn: 'Late Goal',   longTr: 'Geç Gol',                          longEn: 'Late Goal' },
  { id: 99,  developerName: 'TIME_OF_1ST_TEAM_GOAL',                    shortTr: 'İlk Gol Dk',  shortEn: '1st Goal T',  longTr: 'İlk Takım Golü Zamanı',            longEn: 'Time of 1st Team Goal' },
  { id: 102, developerName: 'TIME_OF_FIRST_GOAL_BRACKETS',              shortTr: 'İlk Gol Aralık',shortEn:'1st G Brack',longTr: 'İlk Gol Zaman Aralığı',            longEn: 'Time of First Goal Brackets' },
  { id: 103, developerName: 'TOTAL_GOAL_MINUTES',                       shortTr: 'Gol Dakikaları',shortEn:'Goal Mins',  longTr: 'Toplam Gol Dakikaları',            longEn: 'Total Goal Minutes' },
  { id: 250, developerName: 'FIRST_GOAL_METHOD',                        shortTr: 'İlk Gol Şek', shortEn: '1st G Method',longTr: 'İlk Gol Şekli',                    longEn: 'First Goal Method' },
  { id: 251, developerName: 'FIRST_GOAL_SCORER',                        shortTr: 'İlk Gol Oyn', shortEn: '1st Scorer',  longTr: 'İlk Golü Atan Oyuncu',             longEn: 'First Goal Scorer' },
  { id: 252, developerName: 'LAST_GOAL_SCORER',                         shortTr: 'Son Gol Oyn', shortEn: 'Last Scorer', longTr: 'Son Golü Atan Oyuncu',             longEn: 'Last Goal Scorer' },
  { id: 128, developerName: 'OWN_GOAL',                                 shortTr: 'KK Gol',      shortEn: 'Own Goal',    longTr: 'Kendi Kalesine Gol',               longEn: 'Own Goal' },
  { id: 90,  developerName: 'GOALSCORERS',                              shortTr: 'Gol Atanlar', shortEn: 'Goalscorers', longTr: 'Gol Atan Oyuncular',               longEn: 'Goalscorers' },
  { id: 92,  developerName: 'TEAM_GOALSCORER',                          shortTr: 'Takım Gol Atan',shortEn:'Team Scorer',longTr: 'Takımın Gol Atan Oyuncusu',        longEn: 'Team Goalscorer' },
  { id: 100, developerName: 'MULTI_SCORERS',                            shortTr: 'Çoklu Gol',   shortEn: 'Multi Score', longTr: 'Çoklu Gol Atan',                   longEn: 'Multi Scorers' },

  // ── Margin / Win Method ─────────────────────────────────────────────
  { id: 126, developerName: 'WINNING_MARGIN',                           shortTr: 'Fark',        shortEn: 'Margin',      longTr: 'Kazanma Farkı',                    longEn: 'Winning Margin' },
  { id: 127, developerName: 'SPECIALS',                                 shortTr: 'Özel',        shortEn: 'Specials',    longTr: 'Özel Bahisler',                    longEn: 'Specials' },

  // ── Corners ─────────────────────────────────────────────────────────
  { id: 67,  developerName: 'CORNER_MARKET',                            shortTr: 'Korner',      shortEn: 'Corners',     longTr: 'Korner',                           longEn: 'Corners' },
  { id: 68,  developerName: 'TOTAL_CORNERS',                            shortTr: 'TK',          shortEn: 'TC',          longTr: 'Toplam Korner',                    longEn: 'Total Corners' },
  { id: 69,  developerName: 'ALTERNATIVE_CORNERS',                      shortTr: 'TK+',         shortEn: 'TC+',         longTr: 'Alternatif Korner',                longEn: 'Alternative Corners' },
  { id: 60,  developerName: '2_WAY_CORNERS',                            shortTr: '2Yön Korner', shortEn: '2W Corn',     longTr: '2 Yönlü Korner',                   longEn: '2-Way Corners' },
  { id: 61,  developerName: 'ASIAN_TOTAL_CORNERS',                      shortTr: 'AKt Korner',  shortEn: 'AT Corn',     longTr: 'Asya Toplam Korner',               longEn: 'Asian Total Corners' },
  { id: 62,  developerName: 'ASIAN_HANDICAP_CORNERS',                   shortTr: 'AH Korner',   shortEn: 'AH Corn',     longTr: 'Asya Handikap Korner',             longEn: 'Asian Handicap Corners' },
  { id: 63,  developerName: '1ST_HALF_ASIAN_CORNERS',                   shortTr: '1Y AH Korn',  shortEn: '1H AH Corn',  longTr: '1. Yarı Asya Korner',              longEn: '1st Half Asian Corners' },
  { id: 70,  developerName: 'FIRST_HALF_CORNERS',                       shortTr: '1Y Korner',   shortEn: '1H Corn',     longTr: 'İlk Yarı Korner',                  longEn: '1st Half Corners' },
  { id: 71,  developerName: 'CORNER_MATCH_BET',                         shortTr: 'Korner MS',   shortEn: 'Corner MS',   longTr: 'Korner Maç Sonucu',                longEn: 'Corner Match Bet' },
  { id: 72,  developerName: 'CORNER_HANDICAP',                          shortTr: 'Korner H',    shortEn: 'Corner H',    longTr: 'Korner Handikap',                  longEn: 'Corner Handicap' },
  { id: 73,  developerName: 'TIME_OF_FIRST_CORNER',                     shortTr: 'İlk Korner Dk',shortEn:'1st Corn T',  longTr: 'İlk Korner Zamanı',                longEn: 'Time of First Corner' },
  { id: 74,  developerName: 'TEAM_CORNERS',                             shortTr: 'Takım Korner',shortEn: 'Team Corn',   longTr: 'Takım Korner',                     longEn: 'Team Corners' },
  { id: 75,  developerName: 'CORNERS_RACE',                             shortTr: 'Korner Yarış',shortEn: 'Corn Race',   longTr: 'Korner Yarışı',                    longEn: 'Corners Race' },
  { id: 76,  developerName: '1ST_MATCH_CORNER',                         shortTr: 'İlk Korner',  shortEn: '1st Corn',    longTr: 'İlk Maç Korneri',                  longEn: '1st Match Corner' },
  { id: 77,  developerName: 'LAST_MATCH_CORNER',                        shortTr: 'Son Korner',  shortEn: 'Last Corn',   longTr: 'Son Maç Korneri',                  longEn: 'Last Match Corner' },
  { id: 78,  developerName: 'MULTICORNERS',                             shortTr: 'Çoklu Korner',shortEn: 'Multi Corn',  longTr: 'Çoklu Korner',                     longEn: 'Multi Corners' },
  { id: 79,  developerName: 'FIRST_10_MINUTES_CORNERS',                 shortTr: 'İlk 10 Korner',shortEn:'1st 10 Corn',longTr: 'İlk 10 Dakika Korner',              longEn: 'First 10 Minutes Corners' },
  { id: 254, developerName: 'THREE_WAY_TOTAL_CORNERS',                  shortTr: 'TK 3Yön',     shortEn: 'TC 3-Way',    longTr: '3 Yönlü Toplam Korner',            longEn: '3-Way Total Corners' },
  { id: 264, developerName: 'MOST_CORNERS',                             shortTr: 'En Çok Korner',shortEn:'Most Corn',   longTr: 'En Çok Korner Atan',               longEn: 'Most Corners' },
  { id: 265, developerName: 'SECOND_HALF_CORNERS',                      shortTr: '2Y Korner',   shortEn: '2H Corn',     longTr: '2. Yarı Korner',                   longEn: '2nd Half Corners' },
  { id: 269, developerName: 'CORNERS_1X2',                              shortTr: 'Korner 1X2',  shortEn: 'Corn 1X2',    longTr: 'Korner 1X2',                       longEn: 'Corners 1X2' },
  { id: 301, developerName: 'CORNERS_1X2_1ST_HALF',                     shortTr: 'Korner 1X2 1Y',shortEn:'C 1X2 1H',    longTr: 'Korner 1X2 1. Yarı',               longEn: 'Corners 1X2 1st Half' },
  { id: 302, developerName: 'CORNERS_OVER_UNDER',                       shortTr: 'Korner A/Ü',  shortEn: 'Corn O/U',    longTr: 'Korner Alt / Üst',                 longEn: 'Corners Over / Under' },
  { id: 322, developerName: 'ODD_EVEN_CORNERS',                         shortTr: 'Korner T/Ç',  shortEn: 'Corn O/E',    longTr: 'Korner Tek / Çift',                longEn: 'Odd / Even Corners' },
  { id: 323, developerName: 'ODD_EVEN_CORNERS_1ST_HALF',                shortTr: 'Korner T/Ç 1Y',shortEn:'C O/E 1H',    longTr: 'Korner Tek/Çift 1. Yarı',          longEn: 'Odd / Even Corners 1st Half' },
  { id: 324, developerName: 'HANDICAP_CORNERS',                         shortTr: 'Korner Hand', shortEn: 'Corn Hand',   longTr: 'Korner Handikap',                  longEn: 'Handicap Corners' },
  { id: 325, developerName: 'HANDICAP_CORNERS_1ST_HALF',                shortTr: 'Korner Hand 1Y',shortEn:'C Hand 1H',  longTr: 'Korner Handikap 1. Yarı',          longEn: 'Handicap Corners 1st Half' },
  { id: 327, developerName: 'LAST_CORNER',                              shortTr: 'Son Korner',  shortEn: 'Last Corn',   longTr: 'Son Korner',                       longEn: 'Last Corner' },
  { id: 328, developerName: 'FIRST_CORNER',                             shortTr: 'İlk Korner',  shortEn: 'First Corn',  longTr: 'İlk Korner',                       longEn: 'First Corner' },
  { id: 329, developerName: '3_WAY_FIRST_CORNER',                       shortTr: '3Y İlk Korner',shortEn:'3W 1st Corn', longTr: '3 Yönlü İlk Korner',               longEn: '3-Way First Corner' },

  // ── Cards ───────────────────────────────────────────────────────────
  { id: 64,  developerName: 'PLAYER_TO_BE_BOOKED',                      shortTr: 'Oyuncu Sarı', shortEn: 'P Booked',    longTr: 'Oyuncu Sarı Kart Görür',           longEn: 'Player to be Booked' },
  { id: 65,  developerName: '1ST_PLAYER_BOOKED',                        shortTr: 'İlk Sarı',    shortEn: '1st Booked',  longTr: 'İlk Sarı Kart Gören',              longEn: '1st Player Booked' },
  { id: 66,  developerName: 'PLAYER_TO_BE_SENT_OFF',                    shortTr: 'Oyuncu Kırmızı',shortEn:'P Sent Off', longTr: 'Oyuncu Kırmızı Kart Görür',        longEn: 'Player to be Sent Off' },
  { id: 255, developerName: 'NUMBER_OF_CARDS',                          shortTr: 'Kart Sayısı', shortEn: 'Card Count',  longTr: 'Kart Sayısı',                      longEn: 'Number of Cards' },
  { id: 272, developerName: 'ASIAN_TOTAL_CARDS',                        shortTr: 'AKt Kart',    shortEn: 'AT Cards',    longTr: 'Asya Toplam Kart',                 longEn: 'Asian Total Cards' },
  { id: 273, developerName: 'ASIAN_HANDICAP_CARDS',                     shortTr: 'AH Kart',     shortEn: 'AH Cards',    longTr: 'Asya Handikap Kart',               longEn: 'Asian Handicap Cards' },
  { id: 274, developerName: 'BOTH_TEAMS_TO_RECEIVE_A_CARD',             shortTr: '2 Takım Kart',shortEn: 'Both Carded', longTr: 'Her İki Takım Kart Görür',         longEn: 'Both Teams to Receive a Card' },
  { id: 276, developerName: 'BOTH_TEAMS_TO_RECEIVE_MORE_THAN_TWO_CARDS',shortTr: '2T 2+ Kart',  shortEn: 'Both 2+ Cd',  longTr: 'Her İki Takım 2+ Kart Görür',      longEn: 'Both Teams to Receive 2+ Cards' },
  { id: 277, developerName: 'HANDICAP_CARDS',                           shortTr: 'Kart Hand',   shortEn: 'Card Hand',   longTr: 'Kart Handikap',                    longEn: 'Handicap Cards' },
  { id: 278, developerName: 'ALTERNATIVE_HANDICAP_CARDS',               shortTr: 'Kart Hand+',  shortEn: 'Card Hand+',  longTr: 'Alt. Kart Handikap',               longEn: 'Alt. Handicap Cards' },
  { id: 279, developerName: 'FIRST_CARD_RECEIVED',                      shortTr: 'İlk Kart',    shortEn: '1st Card',    longTr: 'İlk Kart',                         longEn: 'First Card Received' },
  { id: 280, developerName: 'TIME_OF_FIRST_CARD',                       shortTr: 'İlk Kart Dk', shortEn: '1st Card T',  longTr: 'İlk Kart Zamanı',                  longEn: 'Time of First Card' },
  { id: 281, developerName: 'TEAM_CARDS',                               shortTr: 'Takım Kart',  shortEn: 'Team Cards',  longTr: 'Takım Kart',                       longEn: 'Team Cards' },
  { id: 282, developerName: 'RED_CARD_IN_MATCH',                        shortTr: 'Kırmızı Kart',shortEn: 'Red Card',    longTr: 'Maçta Kırmızı Kart',               longEn: 'Red Card in the Match' },

  // ── Penalty ─────────────────────────────────────────────────────────
  { id: 270, developerName: 'TO_SCORE_A_PENALTY',                       shortTr: 'Penaltı Atar',shortEn: 'Score Pen',   longTr: 'Penaltı Gol Atar',                 longEn: 'To Score a Penalty' },
  { id: 271, developerName: 'TO_MISS_A_PENALTY',                        shortTr: 'Penaltı Kaçırır',shortEn:'Miss Pen', longTr: 'Penaltı Kaçırır',                  longEn: 'To Miss a Penalty' },
  { id: 283, developerName: 'PENALTY_IN_MATCH',                         shortTr: 'Penaltı',     shortEn: 'Pen',         longTr: 'Maçta Penaltı',                    longEn: 'Penalty in the Match' },

  // ── Penalty Shootout ────────────────────────────────────────────────
  { id: 257, developerName: 'PENALTIES_CONVERTED_IN_SHOOTOUT',          shortTr: 'Atılan Pen',  shortEn: 'Pens Scored', longTr: 'Penaltı Atışında Atılan',          longEn: 'Penalties Converted in Shootout' },
  { id: 258, developerName: 'HOME_TEAM_PENALTIES_CONVERTED_IN_SHOOTOUT',shortTr: 'EV Pen Atıl',shortEn: 'Home Pens',    longTr: 'Ev Penaltı Atışında Atılan',       longEn: 'Home Penalties Converted in Shootout' },
  { id: 259, developerName: 'AWAY_TEAM_PENALTIES_CONVERTED_IN_SHOOTOUT',shortTr: 'DEP Pen Atıl',shortEn:'Away Pens',    longTr: 'Dep Penaltı Atışında Atılan',      longEn: 'Away Penalties Converted in Shootout' },
  { id: 260, developerName: 'PENALTIES_TO_GO_TO_SUDDEN_DEATH',          shortTr: 'Ani Ölüm',    shortEn: 'Sudden Death',longTr: 'Penaltıların Ani Ölüme Gitmesi',   longEn: 'Penalties to Sudden Death' },
  { id: 261, developerName: 'TOTAL_PENALTIES_IN_SHOOTOUT',              shortTr: 'Toplam Pen',  shortEn: 'Total Pens',  longTr: 'Toplam Penaltı',                   longEn: 'Total Penalties in Shootout' },
  { id: 262, developerName: 'LAST_PENALTY_IN_SHOOTOUT_RESULT',          shortTr: 'Son Pen Snç', shortEn: 'Last Pen',    longTr: 'Son Penaltı Sonucu',               longEn: 'Last Penalty in Shootout' },
  { id: 263, developerName: 'TEAM_TO_TAKE_LAST_SHOOUTOUT_PENALTY',      shortTr: 'Son Pen Tk',  shortEn: 'Last Pen Tm', longTr: 'Son Penaltıyı Atan Takım',         longEn: 'Team to Take Last Penalty' },

  // ── Extra Time / Progression ────────────────────────────────────────
  { id: 256, developerName: 'TEAM_TO_QUALIFY',                          shortTr: 'Tur Atlayan', shortEn: 'Qualify',     longTr: 'Tur Atlayan Takım',                longEn: 'Team to Qualify' },
  { id: 298, developerName: 'GAME_DECIDED_AFTER_PENALTIES',             shortTr: 'Penaltıda',   shortEn: 'After Pens',  longTr: 'Maç Penaltılarda Biter',           longEn: 'Game Decided After Penalties' },
  { id: 299, developerName: 'GAME_DECIDED_IN_EXTRA_TIME',               shortTr: 'Uzatmada',    shortEn: 'In ET',       longTr: 'Maç Uzatmalarda Biter',            longEn: 'Game Decided in Extra Time' },
  { id: 300, developerName: 'METHOD_OF_VICTORY',                        shortTr: 'Galibiyet Şek',shortEn:'Win Method',  longTr: 'Galibiyet Şekli',                  longEn: 'Method of Victory' },
  { id: 314, developerName: 'WILL_THERE_BE_EXTRA_TIME',                 shortTr: 'Uzatma Olur', shortEn: 'ET?',         longTr: 'Uzatma Olur mu',                   longEn: 'Will There Be Extra Time' },
  { id: 320, developerName: 'TO_PROGRESS',                              shortTr: 'Tur Atlar',   shortEn: 'Progress',    longTr: 'Tur Atlar',                        longEn: 'To Progress' },

  // ── Shots / Offsides / Tackles / Stats ──────────────────────────────
  { id: 267, developerName: 'PLAYER_TOTAL_SHOTS_ON_TARGET',             shortTr: 'Oyn İsbt Şut',shortEn: 'P Shots OT',  longTr: 'Oyuncu Toplam İsabetli Şut',       longEn: 'Player Total Shots on Target' },
  { id: 268, developerName: 'PLAYER_TOTAL_SHOTS',                       shortTr: 'Oyn Şut',     shortEn: 'P Shots',     longTr: 'Oyuncu Toplam Şut',                longEn: 'Player Total Shots' },
  { id: 284, developerName: 'TEAM_SHOTS_ON_TARGET',                     shortTr: 'Takım İsbt Şut',shortEn:'Team SOT',   longTr: 'Takım İsabetli Şut',               longEn: 'Team Shots on Target' },
  { id: 285, developerName: 'TEAM_SHOTS',                               shortTr: 'Takım Şut',   shortEn: 'Team Shots',  longTr: 'Takım Şut',                        longEn: 'Team Shots' },
  { id: 286, developerName: 'TEAM_OFFSIDES',                            shortTr: 'Takım Ofsayt',shortEn: 'Team Off',    longTr: 'Takım Ofsayt',                     longEn: 'Team Offsides' },
  { id: 287, developerName: 'PLAYER_TOTAL_TACKLES',                     shortTr: 'Oyn Müd',     shortEn: 'P Tackles',   longTr: 'Oyuncu Toplam Müdahale',           longEn: 'Player Total Tackles' },
  { id: 288, developerName: 'PLAYER_TOTAL_ASSISTS',                     shortTr: 'Oyn Asist',   shortEn: 'P Assists',   longTr: 'Oyuncu Toplam Asist',              longEn: 'Player Total Assists' },
  { id: 290, developerName: 'PLAYER_TOTAL_PASSES',                      shortTr: 'Oyn Pas',     shortEn: 'P Passes',    longTr: 'Oyuncu Toplam Pas',                longEn: 'Player Total Passes' },
  { id: 291, developerName: 'MATCH_SHOTS_ON_TARGET',                    shortTr: 'Maç İsbt Şut',shortEn: 'Match SOT',   longTr: 'Maç İsabetli Şut',                 longEn: 'Match Shots on Target' },
  { id: 292, developerName: 'MATCH_SHOTS',                              shortTr: 'Maç Şut',     shortEn: 'Match Shots', longTr: 'Maç Şut',                          longEn: 'Match Shots' },
  { id: 293, developerName: 'MATCH_TACKLES',                            shortTr: 'Maç Müd',     shortEn: 'Match Tcks',  longTr: 'Maç Müdahale',                     longEn: 'Match Tackles' },
  { id: 294, developerName: 'MATCH_OFFSIDES',                           shortTr: 'Maç Ofsayt',  shortEn: 'Match Off',   longTr: 'Maç Ofsayt',                       longEn: 'Match Offsides' },
  { id: 330, developerName: 'TEAM_TACKLES',                             shortTr: 'Takım Müd',   shortEn: 'Team Tcks',   longTr: 'Takım Müdahale',                   longEn: 'Team Total Tackles' },
  { id: 331, developerName: 'PLAYER_TO_SCORE',                          shortTr: 'Oyn Gol',     shortEn: 'P Score',     longTr: 'Oyuncu Gol Atar',                  longEn: 'Player to Score' },
  { id: 332, developerName: 'PLAYER_TO_ASSIST',                         shortTr: 'Oyn Asist',   shortEn: 'P Assist',    longTr: 'Oyuncu Asist Yapar',               longEn: 'Player to Assist' },
  { id: 333, developerName: 'PLAYER_TO_SCORE_OR_ASSIST',                shortTr: 'Oyn G/A',     shortEn: 'P G/A',       longTr: 'Oyuncu Gol veya Asist',            longEn: 'Player to Score or Assist' },
  { id: 334, developerName: 'PLAYER_SHOTS_ON_TARGET',                   shortTr: 'Oyn İsbt Şut',shortEn: 'P SOT',       longTr: 'Oyuncu İsabetli Şut',              longEn: 'Player Shots on Target' },
  { id: 335, developerName: 'PLAYER_SHOTS_ON_TARGET_OUTSIDE_BOX',       shortTr: 'Oyn Cz Dışı SOT',shortEn:'P SOT Out', longTr: 'Oyuncu Ceza Dışı İsabetli Şut',    longEn: 'Player SOT Outside Box' },
  { id: 336, developerName: 'PLAYER_SHOTS',                             shortTr: 'Oyn Şut',     shortEn: 'P Shots',     longTr: 'Oyuncu Şut',                       longEn: 'Player Shots' },
  { id: 337, developerName: 'PLAYER_HEADED_SHOTS_ON_TARGET',            shortTr: 'Oyn Kafa SOT',shortEn: 'P Head SOT',  longTr: 'Oyuncu Kafa İsabetli Şut',         longEn: 'Player Headed SOT' },
  { id: 338, developerName: 'PLAYER_TO_COMMIT_A_FOUL',                  shortTr: 'Oyn Faul Yapar',shortEn:'P Foul',     longTr: 'Oyuncu Faul Yapar',                longEn: 'Player to Commit Foul' },
  { id: 339, developerName: 'PLAYER_TO_BE_FOULED',                      shortTr: 'Oyn Faul Yer',shortEn: 'P Fouled',    longTr: 'Oyuncuya Faul Yapılır',            longEn: 'Player to be Fouled' },
  { id: 340, developerName: 'PLAYER_TACKLES_MADE',                      shortTr: 'Oyn Müd',     shortEn: 'P Tackles',   longTr: 'Oyuncu Müdahale',                  longEn: 'Player Tackles Made' },
  { id: 275, developerName: 'TEAM_PERFORMANCES',                        shortTr: 'Takım Perf',  shortEn: 'Team Perf',   longTr: 'Takım Performansı',                longEn: 'Team Performances' },
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

// ── Outcome label shortener ───────────────────────────────────────────
// Market-id sets driving the locale-aware outcome rewrites below.
// Adding a new market id to the right set keeps the rewrite logic in
// one place — no per-marketId branches inside shortenOutcome.

// 1X2 (home/draw/away). FT result + half-time result + 10-minute result.
const RESULT_1X2_MARKETS = new Set([1, 31, 91]);

// 2-way result (home/away only, no draw possible) — HOME_AWAY style.
const HOME_AWAY_MARKETS = new Set([52]);

// Yes/No outcomes. Covers BTTS family, per-side score, clean sheet,
// win-to-nil, both-halves win/score, early/late goal, 3+ goals, penalty
// markets, card-presence, extra-time/penalty progression flags.
const YES_NO_MARKETS = new Set([
  14, 15, 16, 35, 36, 46, 50, 51, 17, 54, 55,
  39, 40, 41, 248, 253,
  84, 85, 270, 271, 274, 276, 282, 283,
  298, 299, 314, 320,
]);

// Odd/Even outcomes. Full match, 1H, 2H, per-side, corner odd/even.
const ODD_EVEN_MARKETS = new Set([44, 12, 45, 95, 124, 304, 42, 43, 322, 323]);

// Over/Under outcomes. Total goals + per-half totals + alt totals + 3-way
// total + match goals families + corner totals.
const OVER_UNDER_MARKETS = new Set([
  80, 81, 28, 53, 83, 93, 17, 4, 5, 86, 20, 21, 249, 105, 107, 302,
  61, 68, 69,
]);

// Handicap markets where the outcome carries a generic "1/2/Home/Away"
// label that we replace with EV/DEP (TR) or Home/Away (EN). Covers
// Asian, 3-way, plain handicap + alternative + per-half variants for
// match-result-style handicaps. Also includes corner/card handicap
// markets so "H Korner 2" reads "Korner Hand DEP".
const HANDICAP_MARKETS = new Set([
  6, 9, 56, 26, 32, 94, 96, 104, 106, 303, 309, 310,
  62, 72, 273, 277, 278, 324, 325,
]);

// Double Chance — generic (id 2) + per-half (id 47/48). Note id 52 is
// HOME_AWAY (not team double chance) so it's excluded; SportMonks ID
// collision with the deprecated TEAM_DOUBLE_CHANCE name was corrected
// 2026-05-19 against the authoritative market list.
const DOUBLE_CHANCE_MARKETS = new Set([2, 47, 48]);

// Exact-goals markets — strip the ", - <N> Goals" suffix to leave just
// the number ("3" / "3+"). 18/19 ship as "<Team> - 3 Goals" while
// 33/38 ship as "3 Goals" without a team prefix.
const EXACT_GOALS_TEAM_PREFIX = new Set([18, 19]);
const EXACT_GOALS_PLAIN = new Set([33, 38]);

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
 */
export function shortenOutcome(
  label: string,
  marketId: number,
  homeName?: string | null,
  awayName?: string | null,
): string {
  if (EXACT_GOALS_TEAM_PREFIX.has(marketId)) {
    const dash = label.lastIndexOf(' - ');
    const tail = dash >= 0 ? label.slice(dash + 3) : label;
    return tail.replace(/\s+Goals?$/i, '');
  }
  if (EXACT_GOALS_PLAIN.has(marketId)) {
    return label.replace(/\s+Goals?$/i, '');
  }
  const lower = label.toLowerCase();
  const en = activeLang().toLowerCase().startsWith('en');
  // Fulltime 1X2 + half-time 1X2 — neutral codes (international standard).
  if (RESULT_1X2_MARKETS.has(marketId)) {
    if (lower === 'home') return '1';
    if (lower === 'draw') return 'X';
    if (lower === 'away') return '2';
  }
  // Home/Away (no draw) — 2-way result.
  if (HOME_AWAY_MARKETS.has(marketId)) {
    if (lower === 'home') return '1';
    if (lower === 'away') return '2';
  }
  if (YES_NO_MARKETS.has(marketId)) {
    if (lower === 'yes') return en ? 'Yes' : 'Var';
    if (lower === 'no') return en ? 'No' : 'Yok';
  }
  if (ODD_EVEN_MARKETS.has(marketId)) {
    if (lower === 'odd') return en ? 'Odd' : 'Tek';
    if (lower === 'even') return en ? 'Even' : 'Çift';
  }
  if (OVER_UNDER_MARKETS.has(marketId)) {
    if (lower === 'over') return en ? 'Over' : 'Üst';
    if (lower === 'under') return en ? 'Under' : 'Alt';
  }
  // Handicap markets — replace the generic team identifier ("1" /
  // "2" / "Home" / "Away") with EV / DEP (or Home / Away in EN) so
  // the row reads "H DEP +1" instead of "H 2 +1". Without this swap
  // users have to remember which side of the handicap the number
  // refers to, which empirically flips in 50% of mental models.
  if (HANDICAP_MARKETS.has(marketId)) {
    if (lower === '1' || lower === 'home') return en ? 'Home' : 'EV';
    if (lower === '2' || lower === 'away') return en ? 'Away' : 'DEP';
    if (lower === 'x' || lower === 'draw') return en ? 'Draw' : 'Beraberlik';
  }
  // Double Chance — universal compact codes used on Turkish bet slips
  // and most European bookmakers. SportMonks ships labels in two
  // shapes — generic ("Home/Draw", "Home or Draw") and team-specific
  // ("Arka Gdynia or Draw", "Draw or Termalica BB Nieciecza"). The
  // team-specific shape requires home/away names to disambiguate.
  if (DOUBLE_CHANCE_MARKETS.has(marketId)) {
    if (lower === 'home/draw' || lower === 'home or draw') return '1X';
    if (lower === 'home/away' || lower === 'home or away') return '12';
    if (lower === 'draw/away' || lower === 'draw or away') return 'X2';
    const orMatch = lower.split(/\s+or\s+/);
    if (orMatch.length === 2 && homeName && awayName) {
      const h = homeName.toLowerCase();
      const a = awayName.toLowerCase();
      const [left, right] = [orMatch[0].trim(), orMatch[1].trim()];
      if ((left === h && right === 'draw') || (right === h && left === 'draw')) {
        return '1X';
      }
      if ((left === a && right === 'draw') || (right === a && left === 'draw')) {
        return 'X2';
      }
      if ((left === h && right === a) || (left === a && right === h)) {
        return '12';
      }
    }
  }
  return label;
}
