import type { AxiosError } from 'axios';

import { apiClient, ApiClientError } from '@/src/api/client';
import { clearTokens, setTokens, getAuthSnapshot } from '@/src/lib/auth/authStore';
import type { ApiResponse } from '@/src/types/api';

/**
 * Auth API client. Wraps every /auth/* endpoint, normalises error
 * shapes, and updates the auth store on success so the rest of the app
 * (interceptors, useAuth, useTier) sees the new state in one tick.
 */

export interface UserProfile {
  id: string;
  username: string | null;
  email: string | null;
  display_name: string | null;
  role: string;
  tier: 'free' | 'premium';
  tier_expires_at: string | null;
}

interface AuthResponse {
  user: UserProfile;
  access_token: string;
  refresh_token: string;
  token_type: string;
  expires_in: number;
}

interface TokenResponse {
  access_token: string;
  refresh_token: string;
  token_type: string;
  expires_in: number;
}

interface SignupPayload {
  username: string;
  email: string;
  password: string;
  display_name?: string | null;
}

/** Logs in with a username-or-email + password. Stores tokens on success. */
export async function login(
  usernameOrEmail: string,
  password: string,
): Promise<TokenResponse> {
  const body = await postAuth<TokenResponse>('/auth/token', {
    username: usernameOrEmail,
    password,
  });
  await setTokens(body.access_token, body.refresh_token);
  return body;
}

/** Creates a new account. Server hands back tokens — user is signed in. */
export async function signup(payload: SignupPayload): Promise<AuthResponse> {
  const body = await postAuth<AuthResponse>('/auth/signup', payload);
  await setTokens(body.access_token, body.refresh_token);
  return body;
}

/**
 * Revokes the current refresh token on the server and clears local
 * tokens. Safe to call when there's no refresh token (no-op).
 */
export async function logout(): Promise<void> {
  const { refreshToken } = getAuthSnapshot();
  if (refreshToken) {
    try {
      await apiClient.post('/auth/logout', { refresh_token: refreshToken });
    } catch {
      // Swallow — even if the server reject fails, we still want the
      // local tokens cleared so the user's logged out from their POV.
    }
  }
  await clearTokens();
}

/**
 * Triggers a password-reset email. Server always returns 200 — we never
 * leak whether the account exists. The dev token only comes back in
 * Development environments (handy for e2e tests).
 */
export async function requestPasswordReset(
  emailOrUsername: string,
): Promise<{ sent: true; dev_token?: string }> {
  return postAuth<{ sent: true; dev_token?: string }>(
    '/auth/forgot-password',
    { email_or_username: emailOrUsername },
  );
}

/**
 * Deletes the calling user's account. Soft-delete server-side (scrubs
 * PII, marks status='deleted'), revokes all refresh tokens, then we
 * clear local tokens. App should drop the user back to the guest view.
 *
 * Apple/Google store policy requires this entry point — without it the
 * app is rejected from the store.
 */
export async function deleteAccount(reason?: string): Promise<void> {
  try {
    await apiClient.delete('/auth/me', {
      data: { reason: reason ?? null },
    });
  } finally {
    await clearTokens();
  }
}

/**
 * Returns the JWT-derived `/auth/me` projection. Cheap server call —
 * useful after signup/login when we want the canonical tier/email
 * (rather than trusting the just-decoded JWT alone).
 */
export async function fetchMe(): Promise<{
  username: string | null;
  uid: string;
  email: string | null;
  tier: string;
}> {
  const res = await apiClient.get<
    ApiResponse<{ username: string | null; uid: string; email: string | null; tier: string }>
  >('/auth/me');
  if (!res.data.success || !res.data.data) {
    throw new ApiClientError(
      res.data.error?.message ?? 'fetchMe failed',
      res.data.error?.code ?? 'unknown_error',
      res.status,
    );
  }
  return res.data.data;
}

// Shared post-with-error-normalisation helper. /auth/* endpoints return
// the standard ApiResponse envelope; mapping the failure into
// ApiClientError keeps screen code that catches errors simple.
async function postAuth<T>(path: string, body: unknown): Promise<T> {
  try {
    const res = await apiClient.post<ApiResponse<T>>(path, body);
    if (!res.data.success || !res.data.data) {
      throw new ApiClientError(
        res.data.error?.message ?? 'Request failed',
        res.data.error?.code ?? 'unknown_error',
        res.status,
        path,
      );
    }
    return res.data.data;
  } catch (err) {
    if (err instanceof ApiClientError) throw err;
    const axiosErr = err as AxiosError<ApiResponse<unknown>>;
    const code =
      axiosErr.response?.data?.error?.code ?? axiosErr.code ?? 'network_error';
    const message =
      axiosErr.response?.data?.error?.message ??
      axiosErr.message ??
      'Failed to reach API';
    throw new ApiClientError(
      message,
      code,
      axiosErr.response?.status,
      path,
    );
  }
}
