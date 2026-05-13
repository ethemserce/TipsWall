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
