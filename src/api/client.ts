import axios, { AxiosError } from 'axios';
import { env } from '@/src/lib/env';
import type { ApiResponse, PagedResult } from '@/src/types/api';

export const apiClient = axios.create({
  baseURL: `${env.apiBaseUrl}/api/v3`,
  timeout: 15000,
  headers: { Accept: 'application/json' },
});

export class ApiClientError extends Error {
  constructor(
    message: string,
    public readonly code: string,
    public readonly status?: number,
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
    throw new ApiClientError(message, code, axiosErr.response?.status);
  }
}
