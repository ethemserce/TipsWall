'use server';

import { ApiError, apiPost } from '@/lib/api';

export interface KillQueryResult {
  ok: boolean;
  message: string;
}

/**
 * Server action that posts pg_terminate_backend(@pid) via the admin
 * endpoint. Validates pid client-side then forwards; backend re-
 * checks the pid is still active before issuing terminate.
 */
export async function killPostgresQuery(pid: number): Promise<KillQueryResult> {
  if (!Number.isFinite(pid) || pid <= 0) {
    return { ok: false, message: 'pid geçersiz.' };
  }
  try {
    const data = await apiPost<{ terminated: boolean; pid: number }>(
      `/api/v3/admin/ops/postgres/kill-query?pid=${pid}`,
      {},
    );
    return {
      ok: data.terminated === true,
      message: data.terminated
        ? `pid ${data.pid} terminate edildi.`
        : 'Terminate başarısız.',
    };
  } catch (e) {
    return {
      ok: false,
      message: e instanceof ApiError ? e.detail : 'İstek gönderilemedi.',
    };
  }
}
