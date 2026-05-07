import { apiClient, ApiClientError } from '@/src/api/client';
import type { ApiResponse } from '@/src/types/api';
import type { Country } from '@/src/types/country';

export async function getCountry(id: number): Promise<Country> {
  const response = await apiClient.get<ApiResponse<Country>>(`/countries/${id}`);
  const body = response.data;
  if (!body.success || !body.data) {
    throw new ApiClientError(
      body.error?.message ?? 'Country not found',
      body.error?.code ?? 'unknown_error',
      response.status,
    );
  }
  return body.data;
}
