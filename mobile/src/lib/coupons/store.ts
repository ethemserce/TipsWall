import AsyncStorage from '@react-native-async-storage/async-storage';
import { useEffect, useRef, useState } from 'react';

import { selectionKey, type Coupon, type CouponSelection } from '@/src/lib/coupons/types';
import { marketShort } from '@/src/lib/marketShort';

// Per-user storage namespaces. Each logged-in user keeps their picks
// under their own key prefix so a logout / re-login round-trip on the
// same device doesn't drop the draft. Guest users (no JWT) share the
// 'guest' bucket. Legacy unsuffixed keys are migrated into whichever
// namespace activates first — users updating from <2026-05-19 keep
// their data without a manual flush.
const GUEST_NS = 'guest';
const LEGACY_DRAFT_KEY = 'preodds.coupons.draft.v1';
const LEGACY_SAVED_KEY = 'preodds.coupons.saved.v1';

function draftKey(ns: string): string {
  return `preodds.coupons.draft.v1.${ns}`;
}

function savedKey(ns: string): string {
  return `preodds.coupons.saved.v1.${ns}`;
}

interface State {
  draft: Coupon;
  saved: Coupon[];
  hydrated: boolean;
  /** Active storage namespace — 'guest' or the JWT uid claim. */
  namespace: string;
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
  namespace: GUEST_NS,
};
const listeners = new Set<Listener>();

function emit() {
  for (const l of listeners) l();
}

function persist() {
  const ns = state.namespace;
  // Fire-and-forget — UI doesn't block on storage round-trip.
  AsyncStorage.setItem(draftKey(ns), JSON.stringify(state.draft)).catch(() => {});
  AsyncStorage.setItem(savedKey(ns), JSON.stringify(state.saved)).catch(() => {});
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
    // Earlier coupons stored marketShort verbatim ('M80', 'M81') when the
    // canonical catalogue didn't cover the id. Re-resolve every load
    // through the canonical helper so legacy/incomplete saves and
    // post-locale-switch renders both come out with the correct short
    // code in the user's current language ("A/Ü" in TR, "O/U" in EN).
    // If the saved short looks like a sensible non-"M{id}" string and
    // the canonical helper still can't resolve the id, fall back to
    // the stored value so we never make displays *worse* on rehydrate.
    marketShort: (() => {
      const canonical = marketShort(s.marketId ?? 0, s.marketShort ?? null);
      if (canonical.startsWith('M') && /^M\d+$/.test(canonical) && s.marketShort) {
        return s.marketShort;
      }
      return canonical;
    })(),
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

/**
 * Reads the on-disk draft + saved buckets for a given namespace.
 * Returns empty coupons on any error so the UI keeps working.
 *
 * Also migrates legacy unsuffixed keys (`preodds.coupons.{draft,saved}.v1`)
 * into the active namespace on first load — users updating from a
 * pre-2026-05-19 build keep their picks instead of seeing an empty
 * draft after the namespace refactor. Legacy keys are deleted after
 * the migration so we don't re-migrate on every load.
 */
async function loadNamespace(ns: string): Promise<{ draft: Coupon; saved: Coupon[] }> {
  try {
    const [draftRaw, savedRaw] = await Promise.all([
      AsyncStorage.getItem(draftKey(ns)),
      AsyncStorage.getItem(savedKey(ns)),
    ]);
    let parsedDraft = tryParseJson<unknown>(draftRaw);
    let parsedSaved = tryParseJson<unknown[]>(savedRaw);
    if (parsedDraft == null && parsedSaved == null) {
      const [legacyDraft, legacySaved] = await Promise.all([
        AsyncStorage.getItem(LEGACY_DRAFT_KEY),
        AsyncStorage.getItem(LEGACY_SAVED_KEY),
      ]);
      if (legacyDraft != null || legacySaved != null) {
        parsedDraft = tryParseJson<unknown>(legacyDraft);
        parsedSaved = tryParseJson<unknown[]>(legacySaved);
        await Promise.all([
          AsyncStorage.removeItem(LEGACY_DRAFT_KEY),
          AsyncStorage.removeItem(LEGACY_SAVED_KEY),
        ]).catch(() => {});
      }
    }
    const draft = parsedDraft ? migrateCoupon(parsedDraft) ?? emptyDraft() : emptyDraft();
    const saved = Array.isArray(parsedSaved)
      ? parsedSaved.map(migrateCoupon).filter((c): c is Coupon => c != null)
      : [];
    return { draft, saved };
  } catch {
    return { draft: emptyDraft(), saved: [] };
  }
}

// Serialize namespace swaps so concurrent setActiveUser() calls don't
// race — e.g. the module-load guest activation + auth's user activation
// arriving at the same time during app launch. Each call awaits the
// previous one's completion before running.
let activeUserChain: Promise<void> = Promise.resolve();

/**
 * Switches the coupon store to the given user's storage namespace.
 * Pass null for the guest bucket. Flushes the current bucket under
 * its existing namespace before swapping so picks added right before
 * a login/logout transition don't get dropped.
 *
 * Driven by the auth lifecycle: once at startup after auth hydrates,
 * once on login (setTokens), once on logout (clearTokens). Safe to
 * call concurrently — calls are serialized via activeUserChain.
 */
export function setActiveUser(uid: string | null): Promise<void> {
  const newNs = uid ?? GUEST_NS;
  const work = async () => {
    if (state.hydrated && state.namespace === newNs) return;
    if (state.hydrated) {
      // Flush the previous bucket — don't go through persist() because
      // it reads state.namespace, which we're about to overwrite.
      await Promise.all([
        AsyncStorage.setItem(draftKey(state.namespace), JSON.stringify(state.draft)),
        AsyncStorage.setItem(savedKey(state.namespace), JSON.stringify(state.saved)),
      ]).catch(() => {});
    }
    const { draft, saved } = await loadNamespace(newNs);
    state = { draft, saved, hydrated: true, namespace: newNs };
    // Persist the migrated shape under the new namespace so the
    // Kupon→Liste rename + legacy-key migration are durable.
    persist();
    emit();
  };
  activeUserChain = activeUserChain.then(work, work);
  return activeUserChain;
}

// Kick off an initial guest hydrate at module load. Auth store
// upgrades this to a user namespace once it reads tokens off disk.
void setActiveUser(null);

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

// Hard cap on a single coupon. Parlays past 5 legs are mathematically
// unhittable and the UI compresses badly. User-facing cap so the toast
// can stay generic; useTryAddSelection translates this into a quota
// modal.
export const MAX_COUPON_SELECTIONS = 5;

export function toggleSelection(
  selection: Omit<CouponSelection, 'id'>,
): { ok: boolean; reason?: 'max-reached' } {
  const k = selectionKey(selection);
  const exists = state.draft.selections.find((s) => selectionKey(s) === k);
  if (exists) {
    state.draft = {
      ...state.draft,
      selections: state.draft.selections.filter((s) => selectionKey(s) !== k),
      updatedAt: new Date().toISOString(),
    };
    persist();
    emit();
    return { ok: true };
  }
  // Adding — block at the cap. Removing is always allowed (above).
  if (state.draft.selections.length >= MAX_COUPON_SELECTIONS) {
    return { ok: false, reason: 'max-reached' };
  }
  state.draft = {
    ...state.draft,
    selections: [
      ...state.draft.selections,
      { ...selection, id: cryptoRandom() },
    ],
    updatedAt: new Date().toISOString(),
  };
  persist();
  emit();
  // Fire-and-forget — consent gate inside the wrapper makes this a no-op
  // until the user opts in. Imported lazily to avoid a require cycle
  // between store.ts and the analytics module.
  void import('@/src/lib/analytics').then(({ analytics }) =>
    analytics.track('add_to_tip_list', {
      fixture_id: selection.fixtureId,
      market_id: selection.marketId,
      outcome_label: selection.outcomeLabel,
      draft_size: state.draft.selections.length,
    }),
  );
  return { ok: true };
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
 * Wipes a specific user's bucket from disk. Called from the account-
 * deletion path so the deleted account's picks don't sit orphaned
 * across re-installs. Use setActiveUser(null) for the logout path —
 * that swaps the in-memory state to the guest bucket without touching
 * the user's data on disk, so re-login restores it.
 */
export async function wipeUserCoupons(uid: string): Promise<void> {
  await Promise.all([
    AsyncStorage.removeItem(draftKey(uid)),
    AsyncStorage.removeItem(savedKey(uid)),
  ]).catch(() => {});
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
