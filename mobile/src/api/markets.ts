import { getPaged } from '@/src/api/client';
import type { Market } from '@/src/types/market';

export function listMarkets(params: {
  active?: boolean;
  search?: string;
  page?: number;
  perPage?: number;
} = {}) {
  return getPaged<Market>('/markets', {
    active: params.active,
    search: params.search,
    page: params.page,
    per_page: params.perPage,
  });
}
