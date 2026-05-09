import axios from 'axios';

import { env } from '@/src/lib/env';
import {
  clearTokens,
  getAuthSnapshot,
  setTokens,
} from '@/src/lib/auth/authStore';

/**
 * Single-flight refresh: when several requests fail with 401 simultaneously,
 * the first one triggers a /auth/refresh round-trip and the rest await the
 * same promise. Without this we'd fire N concurrent refreshes — Postgres
 * rotates each one, only the latest survives, every request after the first
 * bounces with `token already rotated` and the user gets logged out.
 */

let pendingRefresh: Promise<string | null> | null = null;

export async function refreshAccessToken(): Promise<string | null> {
  if (pendingRefresh) return pendingRefresh;

  const { refreshToken } = getAuthSnapshot();
  if (!refreshToken) return null;

  pendingRefresh = (async () => {
    try {
      // Use a bare axios instance — going through the main client would loop
      // through this same interceptor.
      const response = await axios.post<RefreshResponseEnvelope>(
        `${env.apiBaseUrl}/api/v3/auth/refresh`,
        { refresh_token: refreshToken },
        {
          headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
          timeout: 10000,
        },
      );
      const body = response.data;
      if (!body?.success || !body.data?.access_token || !body.data?.refresh_token) {
        await clearTokens();
        return null;
      }
      await setTokens(body.data.access_token, body.data.refresh_token);
      return body.data.access_token;
    } catch {
      await clearTokens();
      return null;
    } finally {
      pendingRefresh = null;
    }
  })();

  return pendingRefresh;
}

interface RefreshResponseEnvelope {
  success: boolean;
  data?: {
    access_token: string;
    refresh_token: string;
    token_type?: string;
    expires_in?: number;
  };
  error?: { code: string; message: string };
}
