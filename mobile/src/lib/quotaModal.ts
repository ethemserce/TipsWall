import { useEffect, useState } from 'react';

/**
 * Tiny module-level store for the "günde 2 tahmin doldu" modal. Same
 * pattern as the coupon / auth stores: shared snapshot + listener set,
 * no React context. Any code path that wants to surface the modal calls
 * openQuotaLimitModal(); the QuotaLimitModal component (mounted at the
 * root in app/_layout) subscribes via useQuotaModal().
 */

export interface QuotaModalState {
  open: boolean;
  /** How many picks the device has used today — drives the "2/2" line. */
  picksToday: number;
  limit: number;
}

type Listener = () => void;

let state: QuotaModalState = {
  open: false,
  picksToday: 0,
  limit: 2,
};
const listeners = new Set<Listener>();

function emit() {
  for (const l of listeners) l();
}

export function openQuotaLimitModal(opts: { picksToday: number; limit: number }) {
  state = { open: true, picksToday: opts.picksToday, limit: opts.limit };
  emit();
}

export function closeQuotaLimitModal() {
  if (!state.open) return;
  state = { ...state, open: false };
  emit();
}

export function useQuotaModal(): QuotaModalState {
  const [snapshot, setSnapshot] = useState<QuotaModalState>(state);
  useEffect(() => {
    const l = () => setSnapshot(state);
    listeners.add(l);
    return () => {
      listeners.delete(l);
    };
  }, []);
  return snapshot;
}
