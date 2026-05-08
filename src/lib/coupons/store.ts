import AsyncStorage from '@react-native-async-storage/async-storage';
import { useEffect, useState } from 'react';

import { selectionKey, type Coupon, type CouponSelection } from '@/src/lib/coupons/types';

const DRAFT_KEY = 'preodds.coupons.draft.v1';
const SAVED_KEY = 'preodds.coupons.saved.v1';

interface State {
  draft: Coupon;
  saved: Coupon[];
  hydrated: boolean;
}

type Listener = () => void;

function emptyDraft(): Coupon {
  const now = new Date().toISOString();
  return {
    id: cryptoRandom(),
    name: 'Yeni Kupon',
    createdAt: now,
    updatedAt: now,
    status: 'draft',
    selections: [],
  };
}

function cryptoRandom(): string {
  // Lightweight uuid alternative — module-local, no native deps.
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}

let state: State = {
  draft: emptyDraft(),
  saved: [],
  hydrated: false,
};
const listeners = new Set<Listener>();

function emit() {
  for (const l of listeners) l();
}

function persist() {
  // Fire-and-forget — UI doesn't block on storage round-trip.
  AsyncStorage.setItem(DRAFT_KEY, JSON.stringify(state.draft)).catch(() => {});
  AsyncStorage.setItem(SAVED_KEY, JSON.stringify(state.saved)).catch(() => {});
}

async function hydrate() {
  if (state.hydrated) return;
  try {
    const [draftRaw, savedRaw] = await Promise.all([
      AsyncStorage.getItem(DRAFT_KEY),
      AsyncStorage.getItem(SAVED_KEY),
    ]);
    state = {
      draft: draftRaw ? (JSON.parse(draftRaw) as Coupon) : emptyDraft(),
      saved: savedRaw ? (JSON.parse(savedRaw) as Coupon[]) : [],
      hydrated: true,
    };
  } catch {
    state = { draft: emptyDraft(), saved: [], hydrated: true };
  }
  emit();
}

// Kick off hydration once at module load.
hydrate();

export function getState(): State {
  return state;
}

export function subscribe(l: Listener) {
  listeners.add(l);
  return () => listeners.delete(l);
}

export function isInDraft(s: Pick<CouponSelection, 'fixtureId' | 'marketId' | 'outcomeLabel' | 'total' | 'handicap' | 'oddValue'>): boolean {
  const k = selectionKey(s);
  return state.draft.selections.some((x) => selectionKey(x) === k);
}

export function toggleSelection(selection: Omit<CouponSelection, 'id'>) {
  const k = selectionKey(selection);
  const exists = state.draft.selections.find((s) => selectionKey(s) === k);
  if (exists) {
    state.draft = {
      ...state.draft,
      selections: state.draft.selections.filter((s) => selectionKey(s) !== k),
      updatedAt: new Date().toISOString(),
    };
  } else {
    state.draft = {
      ...state.draft,
      selections: [
        ...state.draft.selections,
        { ...selection, id: cryptoRandom() },
      ],
      updatedAt: new Date().toISOString(),
    };
  }
  persist();
  emit();
}

export function removeSelection(id: string) {
  state.draft = {
    ...state.draft,
    selections: state.draft.selections.filter((s) => s.id !== id),
    updatedAt: new Date().toISOString(),
  };
  persist();
  emit();
}

export function clearDraft() {
  state.draft = emptyDraft();
  persist();
  emit();
}

export function saveDraft(name?: string): Coupon | null {
  if (state.draft.selections.length === 0) return null;
  const now = new Date().toISOString();
  const finalName = (name ?? '').trim() || defaultCouponName();
  const saved: Coupon = {
    ...state.draft,
    name: finalName,
    status: 'saved',
    updatedAt: now,
  };
  state.saved = [saved, ...state.saved];
  state.draft = emptyDraft();
  persist();
  emit();
  return saved;
}

export function deleteSavedCoupon(id: string) {
  state.saved = state.saved.filter((c) => c.id !== id);
  persist();
  emit();
}

/**
 * Stamp a selection's settled outcome and, when every selection in the
 * coupon has a verdict, flip the coupon's status from 'saved' to 'settled'.
 * No-op if the value matches what's already stored — protects React Query
 * batches from looping forever on stable data.
 */
export function updateSelectionWinning(
  couponId: string,
  selectionId: string,
  winning: boolean | null,
) {
  let changed = false;
  state.saved = state.saved.map((coupon) => {
    if (coupon.id !== couponId) return coupon;
    const selections = coupon.selections.map((sel) => {
      if (sel.id !== selectionId) return sel;
      const prev = sel.betWinning ?? null;
      if (prev === winning) return sel;
      changed = true;
      return { ...sel, betWinning: winning };
    });
    if (!changed) return coupon;
    const allSettled =
      selections.length > 0 &&
      selections.every((s) => s.betWinning === true || s.betWinning === false);
    return {
      ...coupon,
      selections,
      status: allSettled ? 'settled' : coupon.status,
      updatedAt: new Date().toISOString(),
    };
  });
  if (changed) {
    persist();
    emit();
  }
}

/**
 * True if the fixture's kickoff has passed (or kickoff time is unknown,
 * which we conservatively treat as started). Used to keep stale
 * `betWinning` flags from leaking into UI for matches that haven't kicked
 * off yet — null kickoff means we can't prove the flag is fresh, so we
 * suppress it.
 */
export function selectionStarted(s: { startingAt: string | null }): boolean {
  if (!s.startingAt) return true;
  const ts = Date.parse(s.startingAt);
  if (Number.isNaN(ts)) return true;
  return ts <= Date.now();
}

export type CouponOutcome = 'pending' | 'won' | 'lost';

/**
 * Pure helper for the UI: a coupon counts as `lost` as soon as any
 * settled selection has missed (parlay logic — one miss kills the slip,
 * remaining legs no longer matter). `won` only when every selection has
 * settled to true. Otherwise `pending` (still waiting on at least one).
 */
export function couponOutcome(coupon: Coupon): { state: CouponOutcome; settled: number; won: number } {
  let settled = 0;
  let won = 0;
  let anyLost = false;
  for (const s of coupon.selections) {
    // Stale `winning` flags can creep in for fixtures that haven't kicked
    // off yet — ignore until kickoff has actually passed.
    if (!selectionStarted(s)) continue;
    if (s.betWinning === true) {
      settled++;
      won++;
    } else if (s.betWinning === false) {
      settled++;
      anyLost = true;
    }
  }
  if (anyLost) return { state: 'lost', settled, won };
  if (settled < coupon.selections.length) return { state: 'pending', settled, won };
  return { state: 'won', settled, won };
}

function defaultCouponName(): string {
  const d = new Date();
  // "9 Mayıs Kuponu"
  const months = [
    'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
    'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık',
  ];
  return `${d.getDate()} ${months[d.getMonth()]} Kuponu`;
}

export function totalOdd(coupon: Coupon): number {
  return coupon.selections.reduce((acc, s) => acc * (s.oddValue || 1), 1);
}

/**
 * React subscription to the coupon state. Re-renders any consumer when
 * draft / saved / hydration changes.
 */
export function useCouponStore<T>(selector: (s: State) => T): T {
  const [snapshot, setSnapshot] = useState<T>(() => selector(state));
  useEffect(() => {
    const unsubscribe = subscribe(() => setSnapshot(selector(state)));
    // Sync immediately in case state changed between render and subscribe.
    setSnapshot(selector(state));
    return () => {
      unsubscribe();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
  return snapshot;
}
