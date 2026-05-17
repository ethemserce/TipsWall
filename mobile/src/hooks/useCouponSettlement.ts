import { useQueries } from '@tanstack/react-query';
import { useEffect, useMemo } from 'react';

import { getFixture } from '@/src/api/fixtures';
import { getFixtureOddsRates } from '@/src/api/fixtureOdds';
import { decideSettlement } from '@/src/lib/coupons/settle';
import {
  selectionStarted,
  updateSelectionWinning,
  useCouponStore,
} from '@/src/lib/coupons/store';
import { getStateBucket } from '@/src/lib/fixtureState';
import { notify } from '@/src/lib/toasts';
import type { Coupon, CouponSelection } from '@/src/lib/coupons/types';

/**
 * Walks every saved coupon and stamps `betWinning` once the host fixture
 * is FINISHED and SportMonks publishes a verdict on the outcome.
 *
 * Two guards keep the slip from flipping mid-match:
 *   1. Skip selections whose fixture hasn't kicked off yet (selectionStarted).
 *   2. Only consult `winning` once the fixture state bucket is "finished"
 *      (FT / AET / FT pen. / WO / AWARDED). The backend can publish a
 *      transient `winning: true|false` during live play based on the
 *      running score — we ignore that and treat in-play coupons as
 *      strictly pending.
 *
 * Self-healing: if a previous run (or backend bug) already stamped a flag
 * on a still-live selection, we clear it back to null so the UI returns
 * to "pending". This is what makes the red-miss-mid-match issue go away
 * without a per-device data migration.
 */
export function useCouponSettlement(saved: Coupon[]) {
  // Every selection across saved coupons whose match has plausibly
  // started. Keyed by fixture so each match is fetched once even if
  // it appears in multiple coupons.
  const startedByFixture = useMemo(() => {
    const map = new Map<
      number,
      { bookmakerId: number; selections: { coupon: Coupon; selection: CouponSelection }[] }
    >();
    for (const coupon of saved) {
      for (const sel of coupon.selections) {
        if (!selectionStarted(sel)) continue;
        const existing = map.get(sel.fixtureId);
        if (existing) {
          existing.selections.push({ coupon, selection: sel });
        } else {
          map.set(sel.fixtureId, {
            bookmakerId: sel.bookmakerId,
            selections: [{ coupon, selection: sel }],
          });
        }
      }
    }
    return map;
  }, [saved]);

  const fixtureIds = useMemo(
    () => Array.from(startedByFixture.keys()),
    [startedByFixture],
  );

  // Fixture state — the hard gate. Cheap query, cached by id.
  const fixtureQueries = useQueries({
    queries: fixtureIds.map((id) => ({
      queryKey: ['fixture', id],
      queryFn: () => getFixture(id),
      staleTime: 30 * 1000,
      retry: 1,
    })),
  });

  // Odds-rates carry the per-outcome `winning` verdict. We only ask for
  // these when the fixture is finished; in-play fixtures don't need to
  // fetch this (the verdict would be ignored anyway).
  const finishedFixtureIds = useMemo(
    () =>
      fixtureIds.filter((_, idx) => {
        const stateId = fixtureQueries[idx]?.data?.fixture?.state_id ?? null;
        return getStateBucket(stateId) === 'finished';
      }),
    // fixtureQueries identity churns every render; key off the data update
    // timestamps + the fixtureIds list shape instead.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [
      fixtureIds.join(','),
      fixtureQueries.map((q) => q.dataUpdatedAt).join(','),
    ],
  );

  const oddsQueries = useQueries({
    queries: finishedFixtureIds.map((id) => {
      const bookmakerId = startedByFixture.get(id)?.bookmakerId ?? 2;
      return {
        // Match the same key shape the existing useFixtureOddsRates uses,
        // so opening the fixture detail right after settling reuses the
        // cached payload.
        queryKey: ['fixture-odds-rates', id, bookmakerId, 'all-calc', 'all'],
        queryFn: () =>
          getFixtureOddsRates({
            fixtureId: id,
            bookmakerId,
            marketIds: [],
            window: 'all',
          }),
        staleTime: 60 * 1000,
        retry: 1,
      };
    }),
  });

  // Self-healing pass — clears premature stamps on live selections.
  // Delegates the per-selection decision to `decideSettlement` so the
  // rule (clear only when fixture state is confirmed live) lives in a
  // single, unit-tested place.
  useEffect(() => {
    fixtureQueries.forEach((q, idx) => {
      if (!q.data) return;
      const fixtureId = fixtureIds[idx];
      const bucket = startedByFixture.get(fixtureId);
      if (!bucket) return;
      const fixtureBucket = getStateBucket(q.data.fixture?.state_id ?? null);
      const stateId = q.data.fixture?.state_id ?? null;
      for (const { coupon, selection } of bucket.selections) {
        const decision = decideSettlement(
          fixtureBucket,
          selection.betWinning,
          undefined, // no verdict needed for the heal path
        );
        if (decision.kind === 'clear') {
          updateSelectionWinning(coupon.id, selection.id, null);
          // Telemetry: each clear is evidence the backend stamped a verdict
          // mid-match (or the bookmaker leaked one through). Tracking lets
          // the data team see how often this happens in the wild without
          // dripping noise through Sentry's error channel.
          void import('@/src/lib/analytics').then(({ analytics }) =>
            analytics.track('coupon_settle_corrected', {
              fixture_id: selection.fixtureId,
              market_id: selection.marketId,
              state_id: stateId,
              previous_winning: selection.betWinning ?? null,
            }),
          );
        }
      }
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    fixtureIds.join(','),
    fixtureQueries.map((q) => q.dataUpdatedAt).join(','),
    startedByFixture,
  ]);

  // Settlement pass — stamp `winning` only when decideSettlement says
  // 'stamp'. `oddsQueries` is only populated for finished fixtures, so
  // the bucket is already finished when we get here, but we still pass
  // it through the helper for the symmetric "noop if verdict matches"
  // dedupe + to keep the single source of truth.
  useEffect(() => {
    oddsQueries.forEach((q, idx) => {
      if (!q.data) return;
      const fixtureId = finishedFixtureIds[idx];
      const bucket = startedByFixture.get(fixtureId);
      if (!bucket) return;

      for (const { coupon, selection } of bucket.selections) {
        const market = q.data.find((m) => m.market_id === selection.marketId);
        if (!market) continue;
        // Match by (label, total, handicap) only — the oddValue at settle
        // time can drift from pick-time (bookmaker re-prices, line moves)
        // but the outcome identity doesn't. The bet pays out at the original
        // odd we already stored on the selection; what we need from the API
        // is just the verdict (`winning` flag).
        const outcome = market.outcomes.find(
          (o) =>
            (o.label ?? '').toLowerCase() ===
              selection.outcomeLabel.toLowerCase() &&
            (o.total ?? null) === (selection.total ?? null) &&
            (o.handicap ?? null) === (selection.handicap ?? null),
        );
        const decision = decideSettlement(
          'finished',
          selection.betWinning,
          outcome?.winning ?? undefined,
        );
        if (decision.kind !== 'stamp') continue;
        const wasUnset =
          selection.betWinning !== true && selection.betWinning !== false;
        updateSelectionWinning(coupon.id, selection.id, decision.winning);
        if (wasUnset) {
          notify({
            kind: decision.winning ? 'win' : 'loss',
            title: decision.winning
              ? `${selection.marketShort} tuttu!`
              : `${selection.marketShort} kaybetti`,
            body: `${selection.fixtureName} · ${selection.outcomeDisplay ?? selection.outcomeLabel}`,
          });
        }
      }
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    oddsQueries.map((q) => q.dataUpdatedAt).join(','),
    startedByFixture,
    finishedFixtureIds.join(','),
  ]);
}

/** Subscribe + run settlement in one place. */
export function useAutoSettleSavedCoupons() {
  const saved = useCouponStore((s) => s.saved);
  useCouponSettlement(saved);
}
