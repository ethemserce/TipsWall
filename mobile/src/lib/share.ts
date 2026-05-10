import * as Linking from 'expo-linking';
import { Share } from 'react-native';

import type { Coupon } from '@/src/lib/coupons/types';

/**
 * Builds a deeplink that opens the app on a specific fixture. Uses
 * expo-linking's createURL so the right scheme is picked at runtime
 * (preoddsmobile:// in standalone, exp://... in dev client). Web fallback
 * via expo-router's `web` config will land users on the marketing site
 * once that exists.
 */
export function fixtureDeepLink(fixtureId: number | string): string {
  return Linking.createURL(`/fixture/${fixtureId}`);
}

/**
 * Native share sheet for a fixture. Title + body are TR-tuned because the
 * current audience is TR-only; once i18n is wired through these will pull
 * from the translation bundle.
 */
export async function shareFixture(
  fixtureId: number,
  fixtureName: string,
): Promise<void> {
  const url = fixtureDeepLink(fixtureId);
  await Share.share({
    title: fixtureName,
    // RN's Share concats title and message on iOS but only message on
    // Android, so put everything we need in `message`.
    message: `${fixtureName}\n${url}\n\nTipsWall'da incele.`,
    url, // honoured on iOS for the URL field of the share intent.
  });
}

/**
 * Share-friendly summary of a saved prediction list. Each pick on its own
 * line. No deep-link to a specific list yet (server-side shared lists are
 * a future feature) — just the tips for now.
 */
export async function shareCoupon(coupon: Coupon): Promise<void> {
  const lines = coupon.selections.map((s) => {
    const tip = `${s.marketShort} ${s.outcomeDisplay ?? s.outcomeLabel}`;
    return `• ${s.fixtureName} — ${tip}`;
  });
  await Share.share({
    title: coupon.name,
    message: [
      coupon.name,
      ...lines,
      `Toplam ${coupon.selections.length} tahmin`,
      '',
      'TipsWall ile takip ediyorum.',
    ].join('\n'),
  });
}
