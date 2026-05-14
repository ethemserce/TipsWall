import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { analytics, consentStore } from '@/src/lib/analytics';
import { useTheme } from '@/src/lib/useTheme';

/**
 * KVKK / GDPR consent prompt for analytics. Renders only when the user
 * hasn't made a choice yet (`consentState === 'pending'`). Sits above
 * the tab bar at the bottom of the screen so it doesn't interrupt the
 * primary content but is clearly visible.
 *
 * After the user picks, the choice is persisted; this component never
 * shows again unless the user explicitly resets it from Settings.
 */
export function AnalyticsConsentBanner() {
  const c = useTheme();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const [state, setState] = useState(consentStore.getState());

  useEffect(() => {
    const unsubscribe = consentStore.subscribe(() => setState(consentStore.getState()));
    return unsubscribe;
  }, []);

  if (state !== 'pending') return null;

  return (
    <View
      pointerEvents="box-none"
      style={[styles.wrapper, { paddingBottom: Math.max(12, insets.bottom + 8) }]}>
      <View style={[styles.card, { backgroundColor: c.surface, borderColor: c.border }]}>
        <ThemedText style={[styles.title, { color: c.text }]}>
          {t('analytics.consent.title', { defaultValue: 'Kullanım verisi' })}
        </ThemedText>
        <ThemedText style={[styles.body, { color: c.textMuted }]}>
          {t('analytics.consent.body', {
            defaultValue:
              'TipsWall’ı geliştirmek için anonim kullanım verisi (hangi ekranlar açılıyor, hangi özellikler tıklanıyor) toplamak istiyoruz. Kişisel veri toplanmaz, istediğin zaman Ayarlar’dan kapatabilirsin.',
          })}
        </ThemedText>
        <View style={styles.actions}>
          <Pressable
            onPress={() => {
              void analytics.denyConsent();
            }}
            style={({ pressed }) => [
              styles.btn,
              styles.btnGhost,
              { borderColor: c.border },
              pressed && { opacity: 0.7 },
            ]}>
            <ThemedText style={[styles.btnText, { color: c.text }]}>
              {t('analytics.consent.deny', { defaultValue: 'Hayır, teşekkürler' })}
            </ThemedText>
          </Pressable>
          <Pressable
            onPress={() => {
              void analytics.grantConsent();
            }}
            style={({ pressed }) => [
              styles.btn,
              { backgroundColor: c.brand },
              pressed && { opacity: 0.85 },
            ]}>
            <ThemedText style={[styles.btnText, { color: c.textInverse }]}>
              {t('analytics.consent.accept', { defaultValue: 'Kabul ediyorum' })}
            </ThemedText>
          </Pressable>
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  wrapper: {
    position: 'absolute',
    left: 0,
    right: 0,
    bottom: 0,
    paddingHorizontal: 12,
  },
  card: {
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 14,
    padding: 14,
    gap: 8,
    shadowColor: '#000',
    shadowOpacity: 0.15,
    shadowRadius: 12,
    shadowOffset: { width: 0, height: 6 },
    elevation: 8,
  },
  title: {
    fontSize: 14,
    fontWeight: '800',
  },
  body: {
    fontSize: 12,
    lineHeight: 17,
  },
  actions: {
    flexDirection: 'row',
    gap: 8,
    marginTop: 4,
  },
  btn: {
    flex: 1,
    paddingVertical: 10,
    borderRadius: 10,
    alignItems: 'center',
    justifyContent: 'center',
  },
  btnGhost: {
    borderWidth: StyleSheet.hairlineWidth,
  },
  btnText: {
    fontSize: 13,
    fontWeight: '700',
  },
});
