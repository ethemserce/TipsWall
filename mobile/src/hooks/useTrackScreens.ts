import { usePathname } from 'expo-router';
import { useEffect } from 'react';

import { analytics } from '@/src/lib/analytics';

/**
 * Fires Firebase Analytics `screen_view` whenever expo-router's path
 * changes. Path is used as both `screen_name` and `screen_class` so
 * dashboards group on the file-based route (e.g. `/fixture/19429486`
 * → `screen_name="/fixture/[id]"` is not what we get here; we ship the
 * resolved path so query filtering is straightforward). Aggregation
 * can normalise dynamic segments downstream if needed.
 */
export function useTrackScreens(): void {
  const pathname = usePathname();
  useEffect(() => {
    if (!pathname) return;
    void analytics.screen(pathname);
  }, [pathname]);
}
