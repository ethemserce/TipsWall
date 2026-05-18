import { useEffect, useState } from 'react';
import { StyleSheet, View } from 'react-native';
import { BannerAd, BannerAdSize } from 'react-native-google-mobile-ads';

import { useTier } from '@/src/lib/auth/authStore';
import { env } from '@/src/lib/env';

/**
 * AdMob banner ad with tier-gated rendering.
 *
 * - Premium users never see ads.
 * - Guests and free users see an adaptive banner sized to the device.
 * - The unit id falls back to Google's test banner when the env vars
 *   are blank, so dev builds render an ad slot instead of an empty
 *   gap, but never serve real inventory.
 *
 * The banner self-collapses to an empty View while the ad is still
 * loading so a half-laid-out blank rectangle doesn't push content
 * around when the impression eventually fires.
 *
 * Content filtering — TipsWall's Turkish positioning makes gambling
 * / betting / alcohol ads unsuitable. The global request configuration
 * in _layout.tsx sets maxAdContentRating='G' and tagForChildDirected
 * + tagForUnderAge = false so we get the broadest non-adult inventory.
 */
export function AdBanner() {
  const tier = useTier();
  const [loaded, setLoaded] = useState(false);

  // Premium suppression — paying users see no ads. Anything else
  // (guest, free) renders the banner.
  if (tier === 'premium') return null;

  return (
    <View style={[styles.wrap, !loaded && styles.hidden]} pointerEvents="box-none">
      <BannerAd
        unitId={env.admobBannerUnitId}
        size={BannerAdSize.ANCHORED_ADAPTIVE_BANNER}
        requestOptions={{
          requestNonPersonalizedAdsOnly: false,
        }}
        onAdLoaded={() => setLoaded(true)}
        onAdFailedToLoad={() => setLoaded(false)}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    alignItems: 'center',
    paddingVertical: 4,
  },
  // Keep the slot in the tree but invisible while loading so the
  // layout shift on first impression is bounded by paddingVertical
  // only, not the full banner height.
  hidden: {
    opacity: 0,
  },
});
