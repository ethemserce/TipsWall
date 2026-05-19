'use client';

import { useState, useTransition } from 'react';
import { killPostgresQuery } from './kill-query-action';

/**
 * "Kill" button next to a slow query row. Two-step confirm so a
 * misclick doesn't terminate an in-flight batch job; status banner
 * appears below the button after the action returns.
 */
export function KillQueryButton({ pid, durationSeconds }: { pid: number; durationSeconds: number }) {
  const [pending, startTransition] = useTransition();
  const [status, setStatus] = useState<null | { kind: 'ok' | 'err'; message: string }>(null);
  const [confirming, setConfirming] = useState(false);

  const handleConfirm = () => {
    setConfirming(false);
    startTransition(async () => {
      const result = await killPostgresQuery(pid);
      setStatus({ kind: result.ok ? 'ok' : 'err', message: result.message });
      setTimeout(() => setStatus(null), 10_000);
    });
  };

  return (
    <div className="inline-flex flex-col items-end gap-1">
      {confirming ? (
        <div className="flex gap-1.5">
          <button
            type="button"
            onClick={handleConfirm}
            disabled={pending}
            className="px-2 py-0.5 text-xs font-semibold rounded-md bg-danger text-bg disabled:opacity-50">
            {pending ? '…' : 'Onayla'}
          </button>
          <button
            type="button"
            onClick={() => setConfirming(false)}
            disabled={pending}
            className="px-2 py-0.5 text-xs font-medium rounded-md border border-border hover:bg-bg-subtle">
            İptal
          </button>
        </div>
      ) : (
        <button
          type="button"
          onClick={() => setConfirming(true)}
          disabled={pending}
          title={`pg_terminate_backend(${pid}) — ${durationSeconds.toFixed(0)}s sürmekte`}
          className="px-2 py-0.5 text-xs font-semibold rounded-md border border-danger/40 text-danger hover:bg-danger/10 disabled:opacity-50">
          Kill
        </button>
      )}
      {status ? (
        <p className={`text-xs ${status.kind === 'ok' ? 'text-success' : 'text-danger'}`}>
          {status.message}
        </p>
      ) : null}
    </div>
  );
}
