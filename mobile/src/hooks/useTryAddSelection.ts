import { useCallback } from 'react';

import { claimGuestQuotaSlot } from '@/src/api/guestQuota';
import { useTier } from '@/src/lib/auth/authStore';
import {
  isInDraft,
  toggleSelection as toggleSelectionRaw,
} from '@/src/lib/coupons/store';
import type { CouponSelection } from '@/src/lib/coupons/types';
import { openQuotaLimitModal } from '@/src/lib/quotaModal';

/**
 * Single entry point every card uses to add or remove a pick. Wraps the
 * raw coupon-store toggle with the guest daily-quota check:
 *
 *  - Removing an existing pick is always allowed (no quota change).
 *  - Adding a pick for guests goes through claimGuestQuotaSlot(). When
 *    the server says "limit reached" the modal opens and no local state
 *    is touched.
 *  - Adding a pick for free / premium passes through unchecked.
 *
 * Falls open on any quota-endpoint error — better to let the user
 * pick than to lock them out because of a transient network blip.
 */
export function useTryAddSelection() {
  const tier = useTier();

  return useCallback(
    async (
      selection: Omit<CouponSelection, 'id'>,
    ): Promise<{ ok: boolean; reason?: 'quota' | 'error' }> => {
      const alreadyInDraft = isInDraft(selection);
      if (alreadyInDraft) {
        toggleSelectionRaw(selection);
        return { ok: true };
      }

      if (tier === 'guest') {
        try {
          const claim = await claimGuestQuotaSlot();
          if (!claim.granted) {
            openQuotaLimitModal({
              picksToday: claim.picks_today,
              limit: claim.limit,
            });
            return { ok: false, reason: 'quota' };
          }
        } catch {
          // Network / server hiccup — fall through and let the add
          // succeed. The server-side counter is the source of truth
          // for tomorrow; today's pick survives locally regardless.
        }
      }

      toggleSelectionRaw(selection);
      return { ok: true };
    },
    [tier],
  );
}
