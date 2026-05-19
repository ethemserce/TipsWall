'use server';

import { ApiError, apiPost } from '@/lib/api';

export interface RebuildResult {
  ok: boolean;
  message: string;
}

/**
 * Server action that posts to /api/v3/admin/ops/analytics/rebuild on
 * behalf of the admin. The backend returns 202 immediately and runs the
 * 5-10-minute job in a fire-and-forget background task; the client
 * watches the postgres health card afterwards to see the long-running
 * query appear and clear. We never await the actual finish here.
 */
export async function triggerAnalyticsRebuild(): Promise<RebuildResult> {
  try {
    await apiPost<{ success: boolean }>('/api/v3/admin/ops/analytics/rebuild', {});
    return {
      ok: true,
      message: 'Rebuild başlatıldı. Postgres kartında uzun query 5-10 dk sürer.',
    };
  } catch (e) {
    return {
      ok: false,
      message:
        e instanceof ApiError ? e.detail : 'İstek gönderilemedi. Loglara bak.',
    };
  }
}
