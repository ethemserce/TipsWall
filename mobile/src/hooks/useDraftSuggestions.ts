import { format } from 'date-fns';
import { useMemo } from 'react';

import { useFixtureLookup } from '@/src/hooks/useFixtureLookup';
import { useSignals } from '@/src/hooks/useSignals';
import { MARKET_CATALOG, marketShort, shortenOutcome } from '@/src/lib/marketShort';
import type { CouponSelection } from '@/src/lib/coupons/types';
import type { RateResult } from '@/src/types/rateResult';

// Markets we have a curated short/long label for. Used to filter
// suggestions whose market_id isn't in MARKET_CATALOG — those would
// render as "M94 1" (raw fallback) and look broken. Adding a market
// to MARKET_CATALOG automatically unblocks it here.
const KNOWN_MARKET_IDS = new Set(MARKET_CATALOG.map((m) => m.id));

const BOOKMAKER_ID = 2;
const SUGGESTION_LIMIT = 5;

export interface DraftSuggestion {
  signal: RateResult;
  fixtureId: number;
  fixtureName: string;
  startingAt: string | null;
  marketShort: string;
  outcomeLabel: string; // raw — for the store / matching
  outcomeDisplay: string; // shortened — for UI
}

/**
 * Picks a small set of "complete-the-coupon" signals from today's matches:
 * confidence-sorted, value-only (DSO > İKO), top-1 per fixture, ≥5 sample.
 * Drops any signal already in the draft.
 *
 * Skips work entirely when the draft is empty (no intent yet) or full
 * (≥6 selections feels cluttered).
 */
export function useDraftSuggestions(
  draftSelections: CouponSelection[],
): DraftSuggestion[] {
  const enabled =
    draftSelections.length > 0 && draftSelections.length < 6;

  const today = format(new Date(), 'yyyy-MM-dd');
  const { data } = useSignals(
    enabled
      ? {
          bookmakerId: BOOKMAKER_ID,
          fixtureDate: today,
          window: 'all',
          sort: 'confidence',
          minSampleCount: 5,
          valueOnly: true,
          topPerFixture: 1,
          perPage: 25,
        }
      : { bookmakerId: BOOKMAKER_ID, perPage: 0 },
  );

  // Pre-pick the candidate signals — one suggestion per fixture, and any
  // fixture already represented in the draft is dropped entirely (one
  // pick per fixture rule). Removing a draft pick re-opens that fixture.
  const candidates = useMemo<RateResult[]>(() => {
    if (!enabled || !data?.data?.items) return [];
    const draftFixtureIds = new Set(draftSelections.map((sel) => sel.fixtureId));
    const seen = new Set<number>();
    const out: RateResult[] = [];
    for (const s of data.data.items) {
      if (seen.has(s.fixture_id)) continue;
      if (draftFixtureIds.has(s.fixture_id)) continue;
      // Skip markets we don't have a short label for — they'd render
      // as "M{id} {label}" which looks like a bug to the user. Add
      // the missing market to MARKET_CATALOG to re-enable.
      if (!KNOWN_MARKET_IDS.has(s.market_id)) continue;
      seen.add(s.fixture_id);
      out.push(s);
      if (out.length >= SUGGESTION_LIMIT) break;
    }
    return out;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data?.data?.items, draftSelections, enabled]);

  // Fetch fixture details for display names.
  const fixtureIds = useMemo(
    () => candidates.map((c) => c.fixture_id),
    [candidates],
  );
  const { lookup } = useFixtureLookup(fixtureIds);

  return useMemo(() => {
    return candidates.map((s) => {
      const fx = lookup.get(s.fixture_id)?.fixture;
      const home = fx?.home_team_name ?? '';
      const away = fx?.away_team_name ?? '';
      const fixtureName =
        home || away ? `${home} - ${away}` : `Maç #${s.fixture_id}`;
      return {
        signal: s,
        fixtureId: s.fixture_id,
        fixtureName,
        startingAt: fx?.starting_at ?? null,
        marketShort: marketShort(s.market_id),
        outcomeLabel: s.label,
        outcomeDisplay: shortenOutcome(s.label, s.market_id),
      };
    });
  }, [candidates, lookup]);
}
