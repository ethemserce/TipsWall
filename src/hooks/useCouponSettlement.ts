import { useQueries } from '@tanstack/react-query';
import { useEffect, useMemo } from 'react';

import { getFixtureOddsRates } from '@/src/api/fixtureOdds';
import {
  selectionStarted,
  updateSelectionWinning,
  useCouponStore,
} from '@/src/lib/coupons/store';
import { notify } from '@/src/lib/toasts';
import type { Coupon, CouponSelection } from '@/src/lib/coupons/types';

/**
 * Walks every saved coupon, finds selections whose outcome hasn't settled
 * yet, and fetches the bookmaker's odds-rates for the host fixture so we
 * can stamp `betWinning` once SportMonks publishes a verdict. Runs
 * silently in the background — no UI feedback unless the data lands.
 *
 * Mount this on any screen that displays saved coupons (CouponsScreen).
 * React Query caches by fixture so revisiting is free.
 */
export function useCouponSettlement(saved: Coupon[]) {
  // Group every still-pending selection by fixture. We only need to
  // fetch each fixture once even if it appears in multiple coupons.
  const pendingByFixture = useMemo(() => {
    const map = new Map<
      number,
      { bookmakerId: number; selections: { coupon: Coupon; selection: CouponSelection }[] }
    >();
    for (const coupon of saved) {
      for (const sel of coupon.selections) {
        if (sel.betWinning === true || sel.betWinning === false) continue;
        // Skip fixtures whose kickoff hasn't happened yet — settlement is
        // meaningless for them and the bookmaker's `winning` flag can leak
        // stale values from previous syncs.
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
    () => Array.from(pendingByFixture.keys()),
    [pendingByFixture],
  );

  const queries = useQueries({
    queries: fixtureIds.map((id) => {
      const bookmakerId = pendingByFixture.get(id)?.bookmakerId ?? 2;
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

  // When any fixture's odds-rates land, walk its pending selections and
  // try to match by (market, label, total, handicap, value). Only stamp
  // the store when we actually find a non-null winning flag.
  useEffect(() => {
    queries.forEach((q, idx) => {
      if (!q.data) return;
      const fixtureId = fixtureIds[idx];
      const bucket = pendingByFixture.get(fixtureId);
      if (!bucket) return;

      for (const { coupon, selection } of bucket.selections) {
        const market = q.data.find((m) => m.market_id === selection.marketId);
        if (!market) continue;
        const outcome = market.outcomes.find(
          (o) =>
            (o.label ?? '').toLowerCase() ===
              selection.outcomeLabel.toLowerCase() &&
            (o.total ?? null) === (selection.total ?? null) &&
            (o.handicap ?? null) === (selection.handicap ?? null) &&
            o.value != null &&
            Math.abs(o.value - selection.oddValue) < 0.0001,
        );
        const winning = outcome?.winning;
        if (winning === true || winning === false) {
          // updateSelectionWinning is a no-op when the value already matches,
          // so it's safe to call on every render — the toast only fires for
          // *new* settlements because we read the prev flag here first.
          const wasUnset =
            selection.betWinning !== true && selection.betWinning !== false;
          updateSelectionWinning(coupon.id, selection.id, winning);
          if (wasUnset) {
            notify({
              kind: winning ? 'win' : 'loss',
              title: winning
                ? `${selection.marketShort} tuttu!`
                : `${selection.marketShort} kaybetti`,
              body: `${selection.fixtureName} · ${selection.outcomeDisplay ?? selection.outcomeLabel}`,
            });
          }
        }
      }
    });
    // We deliberately depend on the fetched payloads + the pending map
    // shape rather than the queries array reference (which churns on
    // every render).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    queries.map((q) => q.dataUpdatedAt).join(','),
    pendingByFixture,
    fixtureIds.join(','),
  ]);
}

/** Subscribe + run settlement in one place. */
export function useAutoSettleSavedCoupons() {
  const saved = useCouponStore((s) => s.saved);
  useCouponSettlement(saved);
}
