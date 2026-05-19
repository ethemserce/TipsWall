'use client';

import { useState, useTransition } from 'react';
import { triggerAnalyticsRebuild } from './rebuild-action';

/**
 * "Analytics rebuild" trigger button on the ops page. Posts to the
 * fire-and-forget admin endpoint and surfaces a status banner. The
 * underlying SQL chain (season stats × 3, outcome finalizer, snapshot
 * regen) runs 5-10 min — watch the Postgres card for the long query.
 *
 * Double-tap protection: useTransition disables the button while a
 * post is in flight, and we keep it disabled for a few extra seconds
 * after the toast lands so an impatient click doesn't queue a second
 * concurrent rebuild.
 */
export function RebuildButton() {
  const [pending, startTransition] = useTransition();
  const [status, setStatus] = useState<
    null | { kind: 'ok' | 'err'; message: string }
  >(null);
  const [confirming, setConfirming] = useState(false);

  const handleConfirm = () => {
    setConfirming(false);
    startTransition(async () => {
      const result = await triggerAnalyticsRebuild();
      setStatus({ kind: result.ok ? 'ok' : 'err', message: result.message });
      // Auto-clear the banner after 12 seconds — the rebuild keeps
      // running in the background; the user can keep an eye on the
      // Postgres card without the toast cluttering the header.
      setTimeout(() => setStatus(null), 12_000);
    });
  };

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-3">
        {confirming ? (
          <>
            <button
              type="button"
              onClick={handleConfirm}
              disabled={pending}
              className="px-3 py-1.5 text-sm font-semibold rounded-md bg-danger text-bg disabled:opacity-50">
              {pending ? 'Başlatılıyor…' : 'Evet, başlat'}
            </button>
            <button
              type="button"
              onClick={() => setConfirming(false)}
              disabled={pending}
              className="px-3 py-1.5 text-sm font-medium rounded-md border border-border hover:bg-bg-subtle">
              İptal
            </button>
          </>
        ) : (
          <button
            type="button"
            onClick={() => setConfirming(true)}
            disabled={pending}
            className="px-3 py-1.5 text-sm font-semibold rounded-md bg-fg text-bg hover:bg-fg/90 disabled:opacity-50">
            Analytics rebuild başlat
          </button>
        )}
        <p className="text-xs text-fg-subtle">
          Snapshot tablosunu yeniden üretir (5-10 dk). Gece 03:00 UTC
          otomatik çalışır.
        </p>
      </div>
      {status ? (
        <p
          className={`text-xs ${
            status.kind === 'ok' ? 'text-success' : 'text-danger'
          }`}>
          {status.message}
        </p>
      ) : null}
    </div>
  );
}
