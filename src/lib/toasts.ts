/**
 * Tiny module-level toast bus. Anything in the app can call `notify(...)`
 * and the global ToastHost (mounted at the root layout) will surface it as
 * a banner. No native deps — the only things this gives up vs. real push
 * notifications are out-of-app delivery and OS notification center.
 */

export interface Toast {
  id: string;
  title: string;
  body?: string;
  kind: 'win' | 'loss' | 'info';
}

type Listener = (t: Toast) => void;
const listeners = new Set<Listener>();

export function notify(input: Omit<Toast, 'id'>): void {
  const toast: Toast = {
    ...input,
    id: `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`,
  };
  for (const l of listeners) l(toast);
}

export function subscribeToasts(l: Listener): () => void {
  listeners.add(l);
  return () => {
    listeners.delete(l);
  };
}
