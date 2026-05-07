/**
 * SportMonks fixture state IDs grouped into UX buckets.
 * Source: SportMonks football states catalog (id 1..27).
 */
export type StateBucket = 'upcoming' | 'live' | 'finished' | 'other';

const LIVE_STATES = new Set<number>([2, 3, 4, 6, 19, 20, 22, 25, 26]);
const FINISHED_STATES = new Set<number>([5, 7, 8, 21]);
const UPCOMING_STATES = new Set<number>([1, 15, 16, 18]);

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
    case 1:
      return 'NS';
    case 2:
      return '1H';
    case 3:
      return 'HT';
    case 4:
      return '2H';
    case 5:
      return 'FT';
    case 6:
      return 'ET';
    case 7:
      return 'AET';
    case 8:
      return 'FT pen.';
    case 9:
      return 'CAN';
    case 10:
      return 'POSTP';
    case 11:
      return 'INT';
    case 12:
      return 'ABAN';
    case 13:
      return 'SUS';
    case 15:
      return 'DELAY';
    case 16:
      return 'TBA';
    case 17:
      return 'WO';
    case 18:
      return 'AU';
    case 19:
      return 'PEN';
    case 21:
      return 'AP';
    case 22:
      return 'ETB';
    case 25:
      return 'PEN.';
    case 26:
      return 'PEN.';
    default:
      return '';
  }
}
