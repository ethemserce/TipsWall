import Link from 'next/link';

import { ApiError, apiGet } from '@/lib/api';

import { RebuildButton } from './RebuildButton';

export const dynamic = 'force-dynamic';
export const revalidate = 0;

type Tab = 'postgres' | 'workers' | 'snapshot' | 'sportmonks';
const TABS: { id: Tab; label: string }[] = [
  { id: 'postgres', label: 'Postgres' },
  { id: 'workers', label: 'Workers' },
  { id: 'snapshot', label: 'Snapshot' },
  { id: 'sportmonks', label: 'SportMonks' },
];

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

interface SportMonksQuota {
  remaining: number | null;
  resets_at: string | null;
  last_seen_at: string | null;
  calls_last_hour: number;
  failure_rate_percent: number;
}

interface SportMonksError {
  started_at: string;
  endpoint: string;
  status_code: number | null;
  error: string | null;
}

async function loadOps(): Promise<{
  workers: WorkerStatus[] | { error: string };
  postgres: PostgresHealth | { error: string };
  nightly: NightlySnapshotRun[] | { error: string };
  quota: SportMonksQuota | { error: string };
  errors: SportMonksError[] | { error: string };
  fetchedAt: string;
}> {
  const [workers, postgres, nightly, quota, errors] = await Promise.all([
    apiGet<WorkerStatus[]>('/api/v3/admin/ops/workers').catch((e) =>
      e instanceof ApiError ? { error: e.detail } : { error: 'Bilinmeyen hata' },
    ),
    apiGet<PostgresHealth>('/api/v3/admin/ops/postgres').catch((e) =>
      e instanceof ApiError ? { error: e.detail } : { error: 'Bilinmeyen hata' },
    ),
    apiGet<NightlySnapshotRun[]>('/api/v3/admin/ops/nightly-snapshot/history?days=10').catch((e) =>
      e instanceof ApiError ? { error: e.detail } : { error: 'Bilinmeyen hata' },
    ),
    apiGet<SportMonksQuota>('/api/v3/admin/ops/sportmonks/quota').catch((e) =>
      e instanceof ApiError ? { error: e.detail } : { error: 'Bilinmeyen hata' },
    ),
    apiGet<SportMonksError[]>('/api/v3/admin/ops/sportmonks/errors?hours=24').catch((e) =>
      e instanceof ApiError ? { error: e.detail } : { error: 'Bilinmeyen hata' },
    ),
  ]);
  return { workers, postgres, nightly, quota, errors, fetchedAt: new Date().toISOString() };
}

export default async function OpsPage({
  searchParams,
}: {
  searchParams: Promise<{ tab?: string }>;
}) {
  const { workers, postgres, nightly, quota, errors, fetchedAt } = await loadOps();
  const params = await searchParams;
  const active: Tab =
    params.tab === 'workers' || params.tab === 'snapshot' || params.tab === 'sportmonks'
      ? params.tab
      : 'postgres';

  return (
    <div className="space-y-6">
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

      {/* Tab nav — URL-based so browser back / share-link works. The
          server component re-renders the active tab's content; all
          three datasets were already fetched in parallel above so
          tab switches stay snappy. */}
      <nav
        className="flex gap-1 border-b border-border"
        aria-label="Ops sekmesi">
        {TABS.map((tab) => {
          const isActive = tab.id === active;
          return (
            <Link
              key={tab.id}
              href={`/ops?tab=${tab.id}`}
              prefetch={false}
              className={`px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors ${
                isActive
                  ? 'border-fg text-fg'
                  : 'border-transparent text-fg-muted hover:text-fg hover:border-border'
              }`}>
              {tab.label}
            </Link>
          );
        })}
      </nav>

      <div>
        {active === 'postgres' ? <PostgresCard postgres={postgres} /> : null}
        {active === 'workers' ? <WorkersTable workers={workers} /> : null}
        {active === 'snapshot' ? (
          <div className="space-y-6">
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
                Son 10 gün
              </h2>
              <NightlySnapshotGrid runs={nightly} />
            </section>
          </div>
        ) : null}
        {active === 'sportmonks' ? (
          <div className="space-y-6">
            <section>
              <h2 className="text-sm font-semibold uppercase tracking-wide text-fg-muted mb-3">
                Kota durumu
              </h2>
              <SportMonksQuotaCard quota={quota} />
            </section>
            <section>
              <h2 className="text-sm font-semibold uppercase tracking-wide text-fg-muted mb-3">
                Son 24 saat hataları
              </h2>
              <SportMonksErrorsTable errors={errors} />
            </section>
          </div>
        ) : null}
      </div>
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

function SportMonksQuotaCard({
  quota,
}: {
  quota: SportMonksQuota | { error: string };
}) {
  if ('error' in quota) {
    return <ErrorBanner message={quota.error} />;
  }
  const resetIn = quota.resets_at
    ? Math.max(0, Math.floor((new Date(quota.resets_at).getTime() - Date.now()) / 1000))
    : null;
  // Remaining headers from SportMonks are per-hour budget on the
  // Starter plan (~3000/hr) — anything under 300 is a warning,
  // under 100 is danger. Adjust thresholds if the subscription
  // tier changes.
  const remainingTone =
    quota.remaining == null ? 'neutral'
      : quota.remaining < 100 ? 'danger'
        : quota.remaining < 300 ? 'warning'
          : 'neutral';
  const failureTone =
    quota.failure_rate_percent >= 25 ? 'danger'
      : quota.failure_rate_percent >= 5 ? 'warning'
        : 'neutral';
  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
      <Stat
        label="Kalan kota"
        value={quota.remaining != null ? quota.remaining.toLocaleString('tr-TR') : '—'}
        tone={remainingTone}
      />
      <Stat
        label="Saatlik çağrı"
        value={quota.calls_last_hour.toLocaleString('tr-TR')}
        tone="neutral"
      />
      <Stat
        label="Hata oranı"
        value={`%${quota.failure_rate_percent.toFixed(1)}`}
        tone={failureTone}
      />
      <Stat
        label="Reset"
        value={resetIn != null ? formatDuration(resetIn) : '—'}
        tone="neutral"
      />
      {quota.last_seen_at ? (
        <p className="col-span-2 md:col-span-4 text-xs text-fg-subtle">
          Son çağrı: {relativeTime(quota.last_seen_at)}
        </p>
      ) : null}
    </div>
  );
}

function SportMonksErrorsTable({
  errors,
}: {
  errors: SportMonksError[] | { error: string };
}) {
  if ('error' in errors) {
    return <ErrorBanner message={errors.error} />;
  }
  if (errors.length === 0) {
    return (
      <p className="text-sm text-success px-4 py-3 bg-success/5 border border-success/20 rounded-md">
        Son 24 saatte SportMonks hatası kaydedilmedi.
      </p>
    );
  }
  return (
    <div className="bg-bg border border-border rounded-md overflow-hidden">
      <table className="w-full text-sm">
        <thead className="bg-bg-subtle border-b border-border text-left">
          <tr>
            <th className="px-4 py-2.5 font-medium text-fg-muted">Zaman (UTC)</th>
            <th className="px-4 py-2.5 font-medium text-fg-muted">Endpoint</th>
            <th className="px-4 py-2.5 font-medium text-fg-muted">Status</th>
            <th className="px-4 py-2.5 font-medium text-fg-muted">Hata</th>
          </tr>
        </thead>
        <tbody>
          {errors.map((e, idx) => (
            <tr key={idx} className="border-b border-border-subtle last:border-0">
              <td className="px-4 py-2.5 font-mono text-xs text-fg-muted">
                {e.started_at.slice(0, 19).replace('T', ' ')}
              </td>
              <td className="px-4 py-2.5 font-mono text-xs">{e.endpoint}</td>
              <td className="px-4 py-2.5">
                {e.status_code != null ? (
                  <Pill
                    tone={e.status_code >= 500 ? 'danger' : 'warning'}
                    label={String(e.status_code)}
                  />
                ) : (
                  <span className="text-fg-subtle text-xs">—</span>
                )}
              </td>
              <td className="px-4 py-2.5 text-xs text-danger">
                {e.error ?? '—'}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function Pill({ tone, label }: { tone: 'success' | 'danger' | 'neutral' | 'warning'; label: string }) {
  const cls = tone === 'success'
    ? 'bg-success/15 text-success'
    : tone === 'danger'
      ? 'bg-danger/15 text-danger'
      : tone === 'warning'
        ? 'bg-warning/15 text-warning'
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
