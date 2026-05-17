import { decideSettlement } from '@/src/lib/coupons/settle';

describe('decideSettlement', () => {
  describe('finished bucket', () => {
    test('verdict differs from stored → stamp with new value', () => {
      expect(decideSettlement('finished', null, true)).toEqual({
        kind: 'stamp',
        winning: true,
      });
      expect(decideSettlement('finished', null, false)).toEqual({
        kind: 'stamp',
        winning: false,
      });
      expect(decideSettlement('finished', false, true)).toEqual({
        kind: 'stamp',
        winning: true,
      });
    });

    test('verdict matches stored → noop (idempotent)', () => {
      expect(decideSettlement('finished', true, true)).toEqual({ kind: 'noop' });
      expect(decideSettlement('finished', false, false)).toEqual({ kind: 'noop' });
    });

    test('null / undefined api verdict → noop (waiting on bookmaker)', () => {
      // FT can come before the bookmaker stamps `winning` (markets without
      // has_winning_calculations rely on score-derived eval); leave alone.
      expect(decideSettlement('finished', null, null)).toEqual({ kind: 'noop' });
      expect(decideSettlement('finished', null, undefined)).toEqual({ kind: 'noop' });
      expect(decideSettlement('finished', true, null)).toEqual({ kind: 'noop' });
    });
  });

  describe('live bucket — self-healing', () => {
    test('selection has a stale flag → clear', () => {
      // This is the bug the hook fixes — backend's evaluate_outcome publishes
      // a transient winning during play; we revert it to pending until FT.
      expect(decideSettlement('live', true, undefined)).toEqual({ kind: 'clear' });
      expect(decideSettlement('live', false, undefined)).toEqual({ kind: 'clear' });
    });

    test('clearing ignores any api verdict — live verdict is untrustable', () => {
      // Even if the API returns winning=true mid-match, we should not stamp.
      expect(decideSettlement('live', null, true)).toEqual({ kind: 'noop' });
      expect(decideSettlement('live', true, true)).toEqual({ kind: 'clear' });
    });

    test('selection is unflagged + match is live → noop', () => {
      expect(decideSettlement('live', null, undefined)).toEqual({ kind: 'noop' });
      expect(decideSettlement('live', undefined, undefined)).toEqual({ kind: 'noop' });
    });
  });

  describe('upcoming / other / null bucket — too uncertain', () => {
    test('upcoming bucket → noop regardless of inputs', () => {
      expect(decideSettlement('upcoming', null, undefined)).toEqual({ kind: 'noop' });
      expect(decideSettlement('upcoming', true, true)).toEqual({ kind: 'noop' });
    });

    test('other bucket (postponed / cancelled / abandoned) → noop', () => {
      // Deliberately do NOT clear stale flags here — admin-finished states
      // (WO 14, AWARDED 17) sit in "finished" already; "other" covers
      // ambiguous lifecycle states where the right call is to wait, not
      // to overwrite whatever the user / settlement pass last stored.
      expect(decideSettlement('other', true, undefined)).toEqual({ kind: 'noop' });
      expect(decideSettlement('other', false, undefined)).toEqual({ kind: 'noop' });
    });

    test('null bucket (state query loading / failed) → noop', () => {
      // Offline / flaky network: clearing on inconclusive data would erase
      // legitimate settlements. Leave the existing flag alone.
      expect(decideSettlement(null, true, undefined)).toEqual({ kind: 'noop' });
      expect(decideSettlement(null, false, true)).toEqual({ kind: 'noop' });
    });
  });
});
