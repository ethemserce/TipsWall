import type { AxiosError } from 'axios';

import { apiClient, ApiClientError } from '@/src/api/client';
import { getDeviceId } from '@/src/lib/auth/deviceId';
import type { ApiResponse } from '@/src/types/api';

export interface GuestQuotaStatus {
  limit: number;
  picks_today: number;
  remaining: number;
}

export interface GuestQuotaClaim extends GuestQuotaStatus {
  granted: boolean;
}

/**
 * Reads today's guest quota counter for this device. Doesn't mutate
 * server state — safe to call as often as the UI wants to refresh
 * a "X / N remaining" badge.
 */
export async function getGuestQuotaStatus(): Promise<GuestQuotaStatus> {
  const deviceId = await getDeviceId();
  try {
    const res = await apiClient.get<ApiResponse<GuestQuotaStatus>>(
      '/guest-quota',
      { params: { device_id: deviceId } },
    );
    if (!res.data.success || !res.data.data) {
      throw new ApiClientError(
        res.data.error?.message ?? 'quota lookup failed',
        res.data.error?.code ?? 'unknown_error',
        res.status,
      );
    }
    return res.data.data;
  } catch (err) {
    return mapError(err, 'getGuestQuotaStatus');
  }
}

/**
 * Attempts to claim one quota slot for today. Server returns 200 with
 * granted=true when there's room, or 429 + granted=false when the
 * device has already used today's allowance. Caller treats granted=false
 * as "deny the add, open the upgrade modal".
 */
export async function claimGuestQuotaSlot(): Promise<GuestQuotaClaim> {
  const deviceId = await getDeviceId();
  try {
    const res = await apiClient.post<ApiResponse<GuestQuotaClaim>>(
      '/guest-quota/claim',
      { device_id: deviceId },
      {
        // Treat 429 as "expected business response" rather than an error;
        // axios would throw by default. We want to read .data either way.
        validateStatus: (s) => s === 200 || s === 429,
      },
    );
    if (!res.data.success || !res.data.data) {
      throw new ApiClientError(
        res.data.error?.message ?? 'quota claim failed',
        res.data.error?.code ?? 'unknown_error',
        res.status,
      );
    }
    return res.data.data;
  } catch (err) {
    return mapError(err, 'claimGuestQuotaSlot');
  }
}

function mapError(err: unknown, path: string): never {
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
