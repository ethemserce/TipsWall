import { ApiError, apiGet } from '@/lib/api';

import { RebuildButton } from './RebuildButton';

export const dynamic = 'force-dynamic';
export const revalidate = 0;

interface WorkerStatus {
  job_key: string;
  last_run_at: string | null;
  last_failure_at: string | null;
  last_error: string | null;
  run_count: number;
}

interface PostgresHealth {
  active_queries: number;
  total_connections: number;
  longest_query_seconds: number | null;
  longest_query_text: string | null;
  in_recovery: boolean;
  database_bytes: number;
}

interface NightlySnapshotRun {
  date: string;
  started_at: string;
  completed_at: string | null;
  duration_seconds: number | null;
  status: string;
  items_count: number | null;
  error_message: string | null;
}

async function loadOps(): Promise<{
  workers: WorkerStatus[] | { error: string };
  postgres: PostgresHealth | { error: string };
  nightly: NightlySnapshotRun[] | { error: string };
  fetchedAt: string;
}> {
  const [workers, postgres, nightly] = await Promise.all([
    apiGet<WorkerStatus[]>('/api/v3/admin/ops/workers').catch((e) =>
      e instanceof ApiError ? { error: e.detail } : { error: 'Bilinmeyen hata' },
    ),
    apiGet<PostgresHealth>('/api/v3/admin/ops/postgres').catch((e) =>
      e instanceof ApiError ? { error: e.detail } : { error: 'Bilinmeyen hata' },
    ),
    apiGet<NightlySnapshotRun[]>('/api/v3/admin/ops/nightly-snapshot/history?days=10').catch((e) =>
      e instanceof ApiError ? { error: e.detail } : { error: 'Bilinmeyen hata' },
    ),
  ]);
  return { workers, postgres, nightly, fetchedAt: new Date().toISOString() };
}

export default async function OpsPage() {
  const { workers, postgres, nightly, fetchedAt } = await loadOps();
  return (
    <div className="space-y-8">
      <header className="flex items-end justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Ops</h1>
          <p className="mt-1 text-sm text-fg-muted">
            Worker tier sağlığı ve Postgres canlı durumu.
          </p>
        </div>
        <p className="text-xs text-fg-subtle">
          Son güncelleme: {new Date(fetchedAt).toLocaleString('tr-TR')}
        </p>
      </header>

      <section>
        <h2 className="text-sm font-semibold uppercase tracking-wide text-fg-muted mb-3">
          Aksiyonlar
        </h2>
        <div className="bg-bg border border-border rounded-md px-4 py-3">
          <RebuildButton />
        </div>
      </section>

      <section>
        <h2 className="text-sm font-semibold uppercase tracking-wide text-fg-muted mb-3">
          NightlySnapshot — son 10 gün
        </h2>
        <NightlySnapshotGrid runs={nightly} />
      </section>

      <section>
        <h2 className="text-sm font-semibold uppercase tracking-wide text-fg-muted mb-3">
          Postgres
        </h2>
        <PostgresCard postgres={postgres} />
      </section>

      <section>
        <h2 className="text-sm font-semibold uppercase tracking-wide text-fg-muted mb-3">
          Worker tier'leri (son 24 saat)
        </h2>
        <WorkersTable workers={workers} />
      </section>
    </div>
  );
}

function PostgresCard({ postgres }: { postgres: PostgresHealth | { error: string } }) {
  if ('error' in postgres) {
    return <ErrorBanner message={postgres.error} />;
  }
  const sizeGb = (postgres.database_bytes / (1024 ** 3)).toFixed(2);
  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
      <Stat
        label="Active query"
        value={String(postgres.active_queries)}
        tone={postgres.active_queries > 8 ? 'danger' : postgres.active_queries > 3 ? 'warning' : 'neutral'}
      />
      <Stat
        label="Toplam bağlantı"
        value={String(postgres.total_connections)}
        tone="neutral"
      />
      <Stat
        label="En uzun query"
        value={postgres.longest_query_seconds != null
          ? `${postgres.longest_query_seconds.toFixed(1)}s`
          : '—'}
        tone={postgres.longest_query_seconds != null && postgres.longest_query_seconds > 60 ? 'danger'
          : postgres.longest_query_seconds != null && postgres.longest_query_seconds > 10 ? 'warning'
          : 'neutral'}
      />
      <Stat
        label="DB boyut"
        value={`${sizeGb} GB`}
        tone="neutral"
      />
      {postgres.in_recovery ? (
        <div className="col-span-2 md:col-span-4 px-4 py-3 rounded-md bg-danger/10 border border-danger/30 text-danger text-sm font-medium">
          ⚠ Postgres recovery modunda. Worker tick'leri 57P03 alıyor olabilir.
        </div>
      ) : null}
      {postgres.longest_query_text ? (
        <details className="col-span-2 md:col-span-4 px-4 py-3 rounded-md bg-bg border border-border">
          <summary className="text-xs text-fg-muted cursor-pointer">
            En uzun query metni
          </summary>
          <pre className="mt-2 text-xs font-mono whitespace-pre-wrap break-all text-fg">
            {postgres.longest_query_text}
          </pre>
        </details>
      ) : null}
    </div>
  );
}

function WorkersTable({ workers }: { workers: WorkerStatus[] | { error: string } }) {
  if ('error' in workers) {
    return <ErrorBanner message={workers.error} />;
  }
  if (workers.length === 0) {
    return (
      <p className="text-sm text-fg-muted px-4 py-3 bg-bg border border-border rounded-md">
        Son 24 saatte hiç worker tick'i kaydedilmemiş — worker container'ı down olabilir.
      </p>
    );
  }
  return (
    <div className="bg-bg border border-border rounded-md overflow-hidden">
      <table className="w-full text-sm">
        <thead className="bg-bg-subtle border-b border-border text-left">
          <tr>
            <th className="px-4 py-2.5 font-medium text-fg-muted">Job</th>
            <th className="px-4 py-2.5 font-medium text-fg-muted">Son çalışma</th>
            <th className="px-4 py-2.5 font-medium text-fg-muted">Son hata</th>
            <th className="px-4 py-2.5 font-medium text-fg-muted text-right">Çalışma</th>
          </tr>
        </thead>
        <tbody>
          {workers.map((w) => {
            const failed = w.last_failure_at != null
              && (w.last_run_at == null || w.last_failure_at >= w.last_run_at);
            return (
              <tr key={w.job_key} className="border-b border-border-subtle last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs">{w.job_key}</td>
                <td className="px-4 py-2.5 text-fg-muted">
                  {w.last_run_at ? relativeTime(w.last_run_at) : '—'}
                </td>
                <td className="px-4 py-2.5">
                  {failed && w.last_error ? (
                    <span className="text-danger text-xs">{w.last_error}</span>
                  ) : (
                    <span className="text-success text-xs">ok</span>
                  )}
                </td>
                <td className="px-4 py-2.5 text-right font-mono text-xs text-fg-muted">
                  {w.run_count.toLocaleString('tr-TR')}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function NightlySnapshotGrid({
  runs,
}: {
  runs: NightlySnapshotRun[] | { error: string };
}) {
  if ('error' in runs) {
    return <ErrorBanner message={runs.error} />;
  }
  // Build a 10-day calendar window (today → 9 days back, UTC date)
  // and overlay any runs that landed in each slot. Slots with no
  // matching run get a red "yok" pill so missed nights are visible
  // at a glance — that was the whole point of the grid.
  const today = new Date();
  const slots: { date: string; run: NightlySnapshotRun | null }[] = [];
  for (let i = 0; i < 10; i++) {
    const d = new Date(Date.UTC(
      today.getUTCFullYear(),
      today.getUTCMonth(),
      today.getUTCDate() - i,
    ));
    const dateStr = d.toISOString().slice(0, 10);
    const run = runs.find((r) => r.date === dateStr) ?? null;
    slots.push({ date: dateStr, run });
  }
  return (
    <div className="bg-bg border border-border rounded-md overflow-hidden">
      <table className="w-full text-sm">
        <thead className="bg-bg-subtle border-b border-border text-left">
          <tr>
            <th className="px-4 py-2.5 font-medium text-fg-muted">UTC tarih</th>
            <th className="px-4 py-2.5 font-medium text-fg-muted">Başlangıç (UTC)</th>
            <th className="px-4 py-2.5 font-medium text-fg-muted">Süre</th>
            <th className="px-4 py-2.5 font-medium text-fg-muted">Durum</th>
            <th className="px-4 py-2.5 font-medium text-fg-muted">Satır</th>
            <th className="px-4 py-2.5 font-medium text-fg-muted">Hata</th>
          </tr>
        </thead>
        <tbody>
          {slots.map((slot) => {
            const r = slot.run;
            const isFailure = r != null && r.status !== 'success';
            const missed = r == null;
            return (
              <tr key={slot.date} className="border-b border-border-subtle last:border-0">
                <td className="px-4 py-2.5 font-mono text-xs">{slot.date}</td>
                <td className="px-4 py-2.5 text-fg-muted text-xs">
                  {r?.started_at ? new Date(r.started_at).toISOString().slice(11, 19) : '—'}
                </td>
                <td className="px-4 py-2.5 text-fg-muted text-xs tabular-nums">
                  {r?.duration_seconds != null
                    ? formatDuration(r.duration_seconds)
                    : '—'}
                </td>
                <td className="px-4 py-2.5">
                  {missed ? (
                    <Pill tone="danger" label="yok" />
                  ) : isFailure ? (
                    <Pill tone="danger" label={r!.status} />
                  ) : (
                    <Pill tone="success" label="ok" />
                  )}
                </td>
                <td className="px-4 py-2.5 text-xs tabular-nums text-fg-muted">
                  {r?.items_count?.toLocaleString('tr-TR') ?? '—'}
                </td>
                <td className="px-4 py-2.5 text-xs">
                  {r?.error_message ? (
                    <span className="text-danger">{r.error_message}</span>
                  ) : (
                    <span className="text-fg-subtle">—</span>
                  )}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function Pill({ tone, label }: { tone: 'success' | 'danger' | 'neutral'; label: string }) {
  const cls = tone === 'success'
    ? 'bg-success/15 text-success'
    : tone === 'danger'
      ? 'bg-danger/15 text-danger'
      : 'bg-bg-subtle text-fg-muted';
  return (
    <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-semibold ${cls}`}>
      {label}
    </span>
  );
}

function formatDuration(seconds: number): string {
  if (seconds < 60) return `${seconds.toFixed(0)}s`;
  const m = Math.floor(seconds / 60);
  const s = Math.round(seconds - m * 60);
  return `${m}m ${s}s`;
}

function Stat({
  label,
  value,
  tone,
}: {
  label: string;
  value: string;
  tone: 'neutral' | 'warning' | 'danger';
}) {
  const toneClass = tone === 'danger'
    ? 'text-danger'
    : tone === 'warning'
      ? 'text-warning'
      : 'text-fg';
  return (
    <div className="bg-bg border border-border rounded-md px-4 py-3">
      <p className="text-xs text-fg-muted uppercase tracking-wide">{label}</p>
      <p className={`mt-1 text-2xl font-semibold tabular-nums ${toneClass}`}>
        {value}
      </p>
    </div>
  );
}

function ErrorBanner({ message }: { message: string }) {
  return (
    <div className="px-4 py-3 rounded-md bg-danger/10 border border-danger/30 text-danger text-sm">
      {message}
    </div>
  );
}

function relativeTime(iso: string): string {
  const t = new Date(iso).getTime();
  const diff = (Date.now() - t) / 1000;
  if (diff < 60) return `${Math.floor(diff)}sn önce`;
  if (diff < 3600) return `${Math.floor(diff / 60)}dk önce`;
  if (diff < 86400) return `${Math.floor(diff / 3600)}sa önce`;
  return new Date(iso).toLocaleString('tr-TR');
}
