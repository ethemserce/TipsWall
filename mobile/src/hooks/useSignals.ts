import { useInfiniteQuery, useQuery } from '@tanstack/react-query';

import { listSignals, type SignalQueryParams } from '@/src/api/signals';

/**
 * Single-page signals query. Kept for callers that explicitly want a
 * bounded list (e.g. fixture-detail "Top picks" widget).
 */
export function useSignals(params: SignalQueryParams = {}) {
  return useQuery({
    queryKey: ['signals', params],
    queryFn: () => listSignals(params),
    // The signal payload itself rarely changes (analytics runs nightly)
    // but each row carries fixture-level state — live_minute, scores —
    // that has to stay in sync with the home tab. 30s staleTime lets a
    // focus/return refetch, the 60s polling is the safety net for users
    // who keep the screen open during a live match, and useLiveTicker
    // invalidates this same key on SignalR pushes for near-instant
    // updates whenever a worker tick lands.
    staleTime: 30 * 1000,
    refetchInterval: 60 * 1000,
    refetchIntervalInBackground: false,
  });
}

const INFINITE_PAGE_SIZE = 40;

/**
 * Paginated signals query for the Analysis screen. Returns one page at
 * a time so the initial render only has to layout ~40 fixture cards;
 * FlatList onEndReached pulls the next page when the user scrolls near
 * the bottom. The query key omits page/perPage so the cache key stays
 * stable across pages.
 */
export function useInfiniteSignals(
  params: Omit<SignalQueryParams, 'page' | 'perPage'> = {},
) {
  return useInfiniteQuery({
    queryKey: ['signals-infinite', params],
    queryFn: ({ pageParam }) =>
      listSignals({ ...params, page: pageParam as number, perPage: INFINITE_PAGE_SIZE }),
    initialPageParam: 1,
    getNextPageParam: (last) => {
      const { page, total_pages } = last.pagination;
      return page < total_pages ? page + 1 : undefined;
    },
    staleTime: 30 * 1000,
    refetchInterval: 60 * 1000,
    refetchIntervalInBackground: false,
  });
}
