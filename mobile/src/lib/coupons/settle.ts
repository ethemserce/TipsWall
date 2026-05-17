import type { StateBucket } from '@/src/lib/fixtureState';

export type SettleDecision =
  | { kind: 'stamp'; winning: boolean }
  | { kind: 'clear' }
  | { kind: 'noop' };

/**
 * Pure helper extracted from the settlement hook so the gating rules can
 * be unit-tested without React Query / hook plumbing. Decides what to do
 * for a single (selection × fixture-state × api-verdict) tuple.
 *
 *   finished + verdict differs from stored → stamp
 *   finished + verdict matches stored      → noop
 *   live + selection currently flagged     → clear (heal premature stamp)
 *   live + selection unflagged             → noop
 *   upcoming / other / unknown bucket      → noop (too uncertain to act)
 *
 * Lives outside `useCouponSettlement.ts` so it stays free of react-native
 * transitive imports — jest can pull it into a node test environment.
 */
export function decideSettlement(
  fixtureBucket: StateBucket | null,
  currentBetWinning: boolean | null | undefined,
  apiWinning: boolean | null | undefined,
): SettleDecision {
  if (fixtureBucket === 'finished') {
    if (apiWinning !== true && apiWinning !== false) return { kind: 'noop' };
    if (currentBetWinning === apiWinning) return { kind: 'noop' };
    return { kind: 'stamp', winning: apiWinning };
  }
  if (fixtureBucket === 'live') {
    if (currentBetWinning === true || currentBetWinning === false) {
      return { kind: 'clear' };
    }
    return { kind: 'noop' };
  }
  return { kind: 'noop' };
}
