import { useEffect, useState } from 'react';

import { marketPreferencesStore } from '@/src/lib/marketPreferences/store';

/**
 * Hook subscribing to the market preferences store. Returns the
 * current selection + cap and re-renders on every change.
 */
export function useMarketPreferences() {
  const [snapshot, setSnapshot] = useState(marketPreferencesStore.getState());
  useEffect(() => {
    const unsubscribe = marketPreferencesStore.subscribe(() =>
      setSnapshot(marketPreferencesStore.getState()),
    );
    return unsubscribe;
  }, []);
  return snapshot;
}
