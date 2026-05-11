import AsyncStorage from '@react-native-async-storage/async-storage';
import { useEffect, useRef, useState } from 'react';

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
    name: 'Yeni Tahmin',
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

/**
 * Migrates a hydrated coupon to the current shape. Each historical schema
 * change adds a step here so users keep their saved coupons forever. The
 * v1 keys (`preodds.coupons.*.v1`) are still the storage path; we don't
 * bump the version on disk — instead we evolve in place so we don't have
 * to flush every device. If a future change is genuinely incompatible,
 * bump to v2 keys + write a one-shot v1→v2 reader here.
 */
function migrateCoupon(raw: unknown): Coupon | null {
  if (!raw || typeof raw !== 'object') return null;
  const c = raw as Partial<Coupon>;
  if (typeof c.id !== 'string' || !Array.isArray(c.selections)) return null;
  return {
    id: c.id,
    name: migrateCouponName(typeof c.name === 'string' ? c.name : undefined),
    createdAt: typeof c.createdAt === 'string' ? c.createdAt : new Date().toISOString(),
    updatedAt: typeof c.updatedAt === 'string' ? c.updatedAt : new Date().toISOString(),
    status: c.status ?? 'saved',
    selections: c.selections.map(migrateSelection).filter((s): s is CouponSelection => s != null),
  };
}

/**
 * Cleans betting-adjacent words out of legacy stored list names. Saved
 * lists named "9 Mayıs Kuponu" / "9 Mayıs Listesi" / "Cumartesi Kuponum"
 * etc. get rewritten on load so the on-device vocabulary stays consistent
 * with the rest of the UI. Trailing "Kuponu" / "Listesi" on date-style
 * names is stripped entirely — "9 Mayıs Listesi" → "9 Mayıs".
 */
function migrateCouponName(name: string | undefined): string {
  if (name == null || name.trim().length === 0) return 'Yeni Tahmin';
  return name
    .replace(/\s*Kuponum$/g, '')
    .replace(/\s*Kuponun$/g, '')
    .replace(/\s*Kuponu$/g, '')
    .replace(/\s*Listesi$/g, '')
    .replace(/Kuponum/g, 'Tahmin')
    .replace(/Kuponun/g, 'Tahmin')
    .replace(/Kuponu/g, 'Tahmin')
    .replace(/Kupon/g, 'Tahmin')
    .replace(/Listesi/g, 'Tahmin')
    .replace(/Listem/g, 'Tahmin')
    .replace(/Liste/g, 'Tahmin')
    .replace(/\s+$/g, '')
    .trim() || 'Yeni Tahmin';
}

function migrateSelection(raw: unknown): CouponSelection | null {
  if (!raw || typeof raw !== 'object') return null;
  const s = raw as Partial<CouponSelection>;
  if (typeof s.id !== 'string' || typeof s.fixtureId !== 'number') return null;
  if (typeof s.outcomeLabel !== 'string' || typeof s.oddValue !== 'number') return null;
  return {
    id: s.id,
    fixtureId: s.fixtureId,
    fixtureName: s.fixtureName ?? `Maç #${s.fixtureId}`,
    startingAt: s.startingAt ?? null,
    bookmakerId: s.bookmakerId ?? 2,
    marketId: s.marketId ?? 0,
    marketShort: s.marketShort ?? `M${s.marketId ?? 0}`,
    outcomeLabel: s.outcomeLabel,
    // outcomeDisplay is newer than the original schema — older coupons fall
    // back to outcomeLabel via the consumer's nullish coalescing.
    outcomeDisplay: s.outcomeDisplay,
    total: s.total ?? null,
    handicap: s.handicap ?? null,
    oddValue: s.oddValue,
    dso: s.dso ?? null,
    vbet: s.vbet ?? null,
    iko: s.iko ?? null,
    sampleCount: s.sampleCount ?? null,
    betWinning: s.betWinning,
  };
}

function tryParseJson<T>(raw: string | null): T | null {
  if (raw == null) return null;
  try {
    return JSON.parse(raw) as T;
  } catch {
    return null;
  }
}

async function hydrate() {
  if (state.hydrated) return;
  try {
    const [draftRaw, savedRaw] = await Promise.all([
      AsyncStorage.getItem(DRAFT_KEY),
      AsyncStorage.getItem(SAVED_KEY),
    ]);
    const parsedDraft = tryParseJson<unknown>(draftRaw);
    const parsedSaved = tryParseJson<unknown[]>(savedRaw);
    const draft = parsedDraft ? migrateCoupon(parsedDraft) ?? emptyDraft() : emptyDraft();
    const saved = Array.isArray(parsedSaved)
      ? parsedSaved
          .map(migrateCoupon)
          .filter((c): c is Coupon => c != null)
      : [];
    state = { draft, saved, hydrated: true };
    // Persist the migrated shape so the Kupon→Liste rename is durable
    // even before the user touches anything.
    persist();
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

export function subscribe(l: Listener): () => void {
  listeners.add(l);
  return () => {
    listeners.delete(l);
  };
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

/**
 * Wipes every saved + draft pick from the device. Called from the
 * auth lifecycle when tokens are cleared (logout / account deletion /
 * forced sign-out on refresh failure) so the *previous* logged-in
 * user's picks don't leak to whoever opens the app next on the same
 * device. Signup-time migration intentionally does NOT call this —
 * a fresh signup keeps the guest's local picks as the seed of their
 * new account.
 *
 * No-op when the store hasn't hydrated yet: clearing before the disk
 * read settles would race the hydrate completion and overwrite the
 * disk with empty state. The auth lifecycle only calls this from
 * user-driven paths, so by then hydration has long since finished.
 */
export function clearAllCoupons() {
  if (!state.hydrated) return;
  state = { draft: emptyDraft(), saved: [], hydrated: true };
  persist();
  emit();
}

/**
 * Snapshot pick counts — used by signup to detect "the guest had N
 * picks they're about to bring into their new account" and surface a
 * confirmation toast.
 */
export function getCouponCounts(): { draft: number; saved: number } {
  return {
    draft: state.draft.selections.length,
    saved: state.saved.length,
  };
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
  // "9 Mayıs" — just the date, no trailing noun.
  const months = [
    'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
    'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık',
  ];
  return `${d.getDate()} ${months[d.getMonth()]}`;
}

export function totalOdd(coupon: Coupon): number {
  return coupon.selections.reduce((acc, s) => acc * (s.oddValue || 1), 1);
}

/**
 * React subscription to the coupon state. Re-renders any consumer when the
 * selector's projection of state changes.
 *
 * Implementation note: the selector is held in a ref that's refreshed on
 * every render, so a parent passing a new selector (e.g. one that closes
 * over a prop) sees consistent values. This sidesteps the classic stale-
 * closure bug where the original selector would be called against new
 * state forever.
 */
export function useCouponStore<T>(selector: (s: State) => T): T {
  const selectorRef = useRef(selector);
  selectorRef.current = selector;
  const [snapshot, setSnapshot] = useState<T>(() => selector(state));
  useEffect(() => {
    const compute = () => {
      const next = selectorRef.current(state);
      setSnapshot((prev) => (Object.is(prev, next) ? prev : next));
    };
    const unsubscribe = subscribe(compute);
    // Sync immediately in case state changed between render and subscribe.
    compute();
    return unsubscribe;
  }, []);
  return snapshot;
}
