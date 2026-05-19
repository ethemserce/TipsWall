import { env } from '@/lib/env';
import { getSession } from '@/lib/session';

export class ApiError extends Error {
  constructor(public status: number, public detail: string) {
    super(detail);
  }
}

interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: { code?: string; message?: string };
}

/**
 * Server-side fetch helper. Reads the access token from the httpOnly
 * cookie and attaches it as Bearer. The backend's ApiResponse<T>
 * envelope is unwrapped here so call sites just see the payload.
 *
 * Errors surface as ApiError with status + message — pages can render
 * a "session expired / not authorised" state for 401 / 403, or a
 * generic failure card for everything else.
 */
export async function apiGet<T>(path: string): Promise<T> {
  const { accessToken } = await getSession();
  if (!accessToken) {
    throw new ApiError(401, 'Not signed in');
  }
  const res = await fetch(`${env.apiBaseUrl}${path}`, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: 'application/json',
    },
    // Admin dashboard is real-time-ish; never cache server-side.
    cache: 'no-store',
  });
  const body = (await res.json()) as ApiResponse<T>;
  if (!res.ok || !body.success || body.data === undefined) {
    throw new ApiError(
      res.status,
      body.error?.message ?? `Request failed: ${res.status}`,
    );
  }
  return body.data;
}

/**
 * POST counterpart for apiGet. The backend's 202 Accepted responses
 * (fire-and-forget jobs like the analytics rebuild) still ship the
 * ApiResponse<T> envelope, so the unwrap logic mirrors apiGet — the
 * only difference is the HTTP method + an optional JSON body.
 */
export async function apiPost<T>(
  path: string,
  body: unknown = {},
): Promise<T> {
  const { accessToken } = await getSession();
  if (!accessToken) {
    throw new ApiError(401, 'Not signed in');
  }
  const res = await fetch(`${env.apiBaseUrl}${path}`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(body),
    cache: 'no-store',
  });
  const json = (await res.json()) as ApiResponse<T>;
  if (!res.ok || !json.success) {
    throw new ApiError(
      res.status,
      json.error?.message ?? `Request failed: ${res.status}`,
    );
  }
  return (json.data ?? ({} as T));
}
