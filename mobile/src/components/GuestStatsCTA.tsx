import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { router } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

/**
 * Sits in CouponStatsCard's slot for guest users — the actual stats
 * (HIT rate, calibration, market breakdown, streak, risk profile) need
 * a stored history that only registered users have. Shows the
 * value proposition + Üye Ol / Giriş Yap pair.
 *
 * Mirrors CouponStatsCard's outer card shape (margin, radius, shadow)
 * so the layout doesn't jump when a guest signs up and the real card
 * takes over.
 */
export function GuestStatsCTA() {
  const c = useTheme();
  const { t } = useTranslation();

  return (
    <View
      style={[
        styles.card,
        c.shadowCard,
        { backgroundColor: c.surfaceElevated, borderColor: c.brand },
      ]}>
      <View style={[styles.iconWrap, { backgroundColor: c.brandSoft }]}>
        <MaterialCommunityIcons name="chart-box-plus-outline" size={22} color={c.brand} />
      </View>
      <ThemedText style={[styles.title, { color: c.text }]}>
        {t('coupons.guestStats.title')}
      </ThemedText>
      <ThemedText style={[styles.body, { color: c.textMuted }]}>
        {t('coupons.guestStats.body')}
      </ThemedText>

      <View style={styles.bulletRow}>
        <Bullet text={t('coupons.guestStats.bulletHit')} c={c} />
        <Bullet text={t('coupons.guestStats.bulletCalibration')} c={c} />
        <Bullet text={t('coupons.guestStats.bulletStreak')} c={c} />
      </View>

      <View style={styles.actions}>
        <Pressable
          onPress={() => router.push('/auth/signup' as never)}
          style={({ pressed }) => [
            styles.primaryBtn,
            { backgroundColor: c.brand, opacity: pressed ? 0.85 : 1 },
          ]}>
          <ThemedText style={[styles.primaryText, { color: c.textInverse }]}>
            {t('coupons.guestStats.signupBtn')}
          </ThemedText>
        </Pressable>
        <Pressable
          onPress={() => router.push('/auth/login' as never)}
          style={({ pressed }) => [
            styles.secondaryBtn,
            { borderColor: c.brand, opacity: pressed ? 0.7 : 1 },
          ]}>
          <ThemedText style={[styles.secondaryText, { color: c.brand }]}>
            {t('coupons.guestStats.loginBtn')}
          </ThemedText>
        </Pressable>
      </View>
    </View>
  );
}

function Bullet({ text, c }: { text: string; c: ReturnType<typeof useTheme> }) {
  return (
    <View style={styles.bullet}>
      <MaterialCommunityIcons name="check-circle" size={14} color={c.brand} />
      <ThemedText style={[styles.bulletText, { color: c.text }]}>
        {text}
      </ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 12,
    marginBottom: 14,
    paddingHorizontal: 16,
    paddingVertical: 16,
    borderRadius: 14,
    borderWidth: 1.5,
    overflow: 'hidden',
    gap: 8,
  },
  iconWrap: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 2,
  },
  title: {
    fontSize: 16,
    fontWeight: '800',
    letterSpacing: 0.2,
  },
  body: {
    fontSize: 13,
    lineHeight: 18,
  },
  bulletRow: {
    gap: 4,
    paddingTop: 4,
    paddingBottom: 6,
  },
  bullet: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  bulletText: {
    fontSize: 12,
    fontWeight: '500',
  },
  actions: {
    flexDirection: 'row',
    gap: 8,
    paddingTop: 4,
  },
  primaryBtn: {
    flex: 1,
    paddingVertical: 11,
    borderRadius: 10,
    alignItems: 'center',
  },
  primaryText: {
    fontSize: 13,
    fontWeight: '800',
    letterSpacing: 0.5,
  },
  secondaryBtn: {
    flex: 1,
    paddingVertical: 10,
    borderRadius: 10,
    borderWidth: 1,
    alignItems: 'center',
  },
  secondaryText: {
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
});
