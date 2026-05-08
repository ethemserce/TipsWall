import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';

import { getAuthSnapshot } from '@/src/lib/auth/authStore';
import { refreshAccessToken } from '@/src/lib/auth/refreshLock';
import { env } from '@/src/lib/env';
import type { ApiResponse, PagedResult } from '@/src/types/api';

export const apiClient = axios.create({
  baseURL: `${env.apiBaseUrl}/api/v3`,
  timeout: 15000,
  headers: { Accept: 'application/json' },
});

// Request interceptor: stamp Authorization header from in-memory auth state.
// Hot path — no async storage hit per request, the auth store hydrates once.
apiClient.interceptors.request.use((config) => {
  const { accessToken } = getAuthSnapshot();
  if (accessToken && !config.headers.has('Authorization')) {
    config.headers.set('Authorization', `Bearer ${accessToken}`);
  }
  return config;
});

// Response interceptor: 401 → single-flight refresh → retry original. We tag
// retries with `_authRetry` so a failed refresh doesn't loop forever.
interface RetryableConfig extends InternalAxiosRequestConfig {
  _authRetry?: boolean;
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const status = error.response?.status;
    const original = error.config as RetryableConfig | undefined;
    if (status !== 401 || !original || original._authRetry) {
      return Promise.reject(error);
    }
    // Don't try to refresh /auth/* endpoints — they're either the refresh
    // call itself (would loop) or login/signup (no token to refresh).
    if (original.url?.startsWith('/auth')) {
      return Promise.reject(error);
    }
    original._authRetry = true;
    const newAccess = await refreshAccessToken();
    if (!newAccess) return Promise.reject(error);
    original.headers.set('Authorization', `Bearer ${newAccess}`);
    return apiClient.request(original);
  },
);

export class ApiClientError extends Error {
  constructor(
    message: string,
    public readonly code: string,
    public readonly status?: number,
    public readonly url?: string,
  ) {
    super(message);
    this.name = 'ApiClientError';
  }
}

export async function getPaged<T>(
  path: string,
  params?: Record<string, string | number | boolean | undefined>,
): Promise<PagedResult<T>> {
  try {
    const response = await apiClient.get<ApiResponse<T[]>>(path, { params });
    const body = response.data;
    if (!body.success || !body.data) {
      throw new ApiClientError(
        body.error?.message ?? 'API returned unsuccessful response',
        body.error?.code ?? 'unknown_error',
        response.status,
        response.config.url,
      );
    }
    return {
      items: body.data,
      pagination: body.pagination ?? {
        page: 1,
        per_page: body.data.length,
        total: body.data.length,
        total_pages: 1,
      },
    };
  } catch (err) {
    if (err instanceof ApiClientError) throw err;
    const axiosErr = err as AxiosError<ApiResponse<unknown>>;
    const code = axiosErr.response?.data?.error?.code ?? axiosErr.code ?? 'network_error';
    const message =
      axiosErr.response?.data?.error?.message ??
      axiosErr.message ??
      'Failed to reach API';
    throw new ApiClientError(
      message,
      code,
      axiosErr.response?.status,
      axiosErr.config?.url,
    );
  }
}
