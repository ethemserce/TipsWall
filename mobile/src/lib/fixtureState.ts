/**
 * SportMonks fixture state IDs grouped into UX buckets.
 *
 * Authoritative source: `catalog.states` table on the API server (synced
 * from SportMonks). Verified 2026-05-13:
 *
 *   1  Not Started        NS                    11 Suspended        SUSPENDED
 *   2  1st Half           INPLAY_1ST_HALF       12 Cancelled        CANCELLED
 *   3  Half Time          HT                    13 To Be Announced  TBA
 *   4  Break              BREAK                 14 Walk Over        WO
 *   5  Full Time          FT                    15 Abandoned        ABANDONED
 *   6  Extra Time         INPLAY_ET             16 Delayed          DELAYED
 *   7  After Extra Time   AET                   17 Awarded          AWARDED
 *   8  After Penalties    FT_PEN                18 Interrupted      INTERRUPTED
 *   9  Penalties          INPLAY_PENALTIES      19 Awaiting Updates AWAITING_UPDATES
 *  10  Postponed          POSTPONED             20 Deleted          DELETED
 *  21  Extra Time - Break EXTRA_TIME_BREAK
 *  22  2nd Half           INPLAY_2ND_HALF
 *  23  ET - 2nd Half      INPLAY_ET_SECOND_HALF
 *  25  Penalties - Break  PEN_BREAK
 *  26  Pending            PENDING
 *
 * The previous mapping had 19/22 wrong (claimed PEN/ETB instead of
 * AU/2nd-half) which caused the full-time sub-score to show up during
 * regulation 2nd half.
 */

export type StateBucket = 'upcoming' | 'live' | 'finished' | 'other';

// Any in-play or in-play-break state. The 3 (HT), 4 (BREAK), 21 (ETB)
// and 25 (PEN-BREAK) states still count as "live" for UX purposes —
// the match is ongoing, just paused. 19 (AWAITING_UPDATES) is rare;
// SportMonks uses it when the feed is gapped mid-match, so we treat
// it as live too to avoid the row falling off the live bucket.
const LIVE_STATES = new Set<number>([2, 3, 4, 6, 9, 19, 21, 22, 23, 25]);

// Final results. FT_PEN (8) is final after the shootout; AET (7) is
// final after extra time; WO (14) and AWARDED (17) are admin finishes
// with no further play.
const FINISHED_STATES = new Set<number>([5, 7, 8, 14, 17]);

// Hasn't kicked off yet. TBA (13), DELAYED (16) and PENDING (26) all
// surface in the same "still waiting" bucket as Not Started (1).
const UPCOMING_STATES = new Set<number>([1, 13, 16, 26]);

export function getStateBucket(stateId: number | null | undefined): StateBucket {
  if (stateId == null) return 'other';
  if (LIVE_STATES.has(stateId)) return 'live';
  if (FINISHED_STATES.has(stateId)) return 'finished';
  if (UPCOMING_STATES.has(stateId)) return 'upcoming';
  return 'other';
}

export function isLive(stateId: number | null | undefined): boolean {
  return getStateBucket(stateId) === 'live';
}

/**
 * Match phase — finer-grained than StateBucket so the hero can render
 * the right combination of score / minute / sub-scores per phase.
 *
 * Mapping derives from the catalog.states list above:
 *   pre        — 1 (NS), 13 (TBA), 16 (DELAYED), 26 (PENDING)
 *   1h         — 2 (INPLAY_1ST_HALF)
 *   ht         — 3 (HT), 4 (BREAK)
 *   2h         — 22 (INPLAY_2ND_HALF), 19 (AWAITING_UPDATES — treat as 2H)
 *   et_break   — 21 (EXTRA_TIME_BREAK)
 *   et_1h      — 6 (INPLAY_ET)
 *   et_2h      — 23 (INPLAY_ET_SECOND_HALF)
 *   pen_break  — 25 (PEN_BREAK)
 *   pen        — 9 (INPLAY_PENALTIES)
 *   ft         — 5 (FT, full-time regulation only)
 *   aet        — 7 (AET)
 *   ft_pen     — 8 (FT_PEN)
 *   other      — postponed / cancelled / suspended / abandoned / etc.
 */
export type MatchPhase =
  | 'pre'
  | '1h'
  | 'ht'
  | '2h'
  | 'et_break'
  | 'et_1h'
  | 'et_2h'
  | 'pen_break'
  | 'pen'
  | 'ft'
  | 'aet'
  | 'ft_pen'
  | 'other';

export function getMatchPhase(stateId: number | null | undefined): MatchPhase {
  if (stateId == null) return 'other';
  switch (stateId) {
    case 1:
    case 13:
    case 16:
    case 26: return 'pre';
    case 2:  return '1h';
    case 3:
    case 4:  return 'ht';
    case 22:
    case 19: return '2h';
    case 21: return 'et_break';
    case 6:  return 'et_1h';
    case 23: return 'et_2h';
    case 25: return 'pen_break';
    case 9:  return 'pen';
    case 5:  return 'ft';
    case 7:  return 'aet';
    case 8:  return 'ft_pen';
    default: return 'other';
  }
}

// True for in-play states where the live ticker should run (minute counts up).
export function phaseHasLiveMinute(phase: MatchPhase): boolean {
  return phase === '1h' || phase === '2h'
      || phase === 'et_1h' || phase === 'et_2h';
}

// True when the half-time (1H) sub-score should be visible.
// 1st-half-only phase hides it (the line score is the HT score itself).
export function phaseShowsHalfTime(phase: MatchPhase): boolean {
  return phase !== 'pre' && phase !== '1h' && phase !== 'other';
}

// True when the regulation full-time (90-min) sub-score should be shown
// — only after ET kicks in (or once the match finishes via ET / Pen).
export function phaseShowsFulltimeSub(phase: MatchPhase): boolean {
  return phase === 'et_break' || phase === 'et_1h' || phase === 'et_2h'
      || phase === 'pen_break' || phase === 'pen'
      || phase === 'aet' || phase === 'ft_pen';
}

// True when penalty shootout score should appear.
export function phaseShowsPenaltyScore(phase: MatchPhase): boolean {
  return phase === 'pen_break' || phase === 'pen' || phase === 'ft_pen';
}

export function getStateLabel(stateId: number | null | undefined): string {
  switch (stateId) {
    case 1:  return 'NS';
    case 2:  return '1H';
    case 3:  return 'HT';
    case 4:  return 'BRK';
    case 5:  return 'FT';
    case 6:  return 'ET';
    case 7:  return 'AET';
    case 8:  return 'FT pen.';
    case 9:  return 'PEN';
    case 10: return 'POSTP';
    case 11: return 'SUSP';
    case 12: return 'CANC';
    case 13: return 'TBA';
    case 14: return 'WO';
    case 15: return 'ABAN';
    case 16: return 'DELAY';
    case 17: return 'AWAR';
    case 18: return 'INT';
    case 19: return 'AU';
    case 20: return 'DEL';
    case 21: return 'ETB';
    case 22: return '2H';
    case 23: return 'ET 2H';
    case 25: return 'PEN BRK';
    case 26: return 'PEND';
    default: return '';
  }
}
