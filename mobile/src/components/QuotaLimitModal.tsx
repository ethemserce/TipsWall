import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { router } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { Modal, Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { closeQuotaLimitModal, useQuotaModal } from '@/src/lib/quotaModal';
import { useTheme } from '@/src/lib/useTheme';

/**
 * Pops when a guest tries to add their (limit+1)th pick of the day.
 * Mounted at the root so it can interrupt any screen. Two paths out:
 *   - "Üye Ol" → /auth/signup, unlimited from there
 *   - "Tamam" → close, user keeps their first 2 picks
 *
 * Apple guidance: the upgrade prompt should be informative, not
 * deceptive. We name what they're getting ("sınırsız tahmin") rather
 * than dark-pattern wording.
 */
export function QuotaLimitModal() {
  const c = useTheme();
  const { t } = useTranslation();
  const { open, picksToday, limit } = useQuotaModal();

  const handleSignup = () => {
    closeQuotaLimitModal();
    router.push('/auth/signup' as never);
  };

  return (
    <Modal
      visible={open}
      transparent
      animationType="fade"
      onRequestClose={closeQuotaLimitModal}>
      <Pressable style={styles.backdrop} onPress={closeQuotaLimitModal}>
        <Pressable
          onPress={(e) => e.stopPropagation()}
          style={[
            styles.sheet,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <View style={[styles.iconWrap, { backgroundColor: c.brandSoft }]}>
            <MaterialCommunityIcons name="lock-clock" size={28} color={c.brand} />
          </View>
          <ThemedText style={[styles.title, { color: c.text }]}>
            {t('quotaLimit.title')}
          </ThemedText>
          <ThemedText style={[styles.body, { color: c.textMuted }]}>
            {t('quotaLimit.body', { count: picksToday, limit })}
          </ThemedText>

          <View style={styles.bullets}>
            <Bullet text={t('quotaLimit.bulletUnlimited')} c={c} />
            <Bullet text={t('quotaLimit.bulletHistory')} c={c} />
            <Bullet text={t('quotaLimit.bulletFavorites')} c={c} />
          </View>

          <Pressable
            onPress={handleSignup}
            style={({ pressed }) => [
              styles.primaryBtn,
              { backgroundColor: c.brand, opacity: pressed ? 0.85 : 1 },
            ]}>
            <ThemedText style={[styles.primaryText, { color: c.textInverse }]}>
              {t('quotaLimit.signupBtn')}
            </ThemedText>
          </Pressable>
          <Pressable
            onPress={closeQuotaLimitModal}
            style={({ pressed }) => [
              styles.secondaryBtn,
              { opacity: pressed ? 0.6 : 1 },
            ]}>
            <ThemedText style={[styles.secondaryText, { color: c.textMuted }]}>
              {t('quotaLimit.dismiss')}
            </ThemedText>
          </Pressable>
        </Pressable>
      </Pressable>
    </Modal>
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
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.55)',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  sheet: {
    width: '100%',
    maxWidth: 360,
    borderRadius: 16,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 20,
    gap: 10,
  },
  iconWrap: {
    width: 56,
    height: 56,
    borderRadius: 28,
    alignItems: 'center',
    justifyContent: 'center',
    alignSelf: 'center',
    marginBottom: 4,
  },
  title: {
    fontSize: 17,
    fontWeight: '800',
    textAlign: 'center',
  },
  body: {
    fontSize: 13,
    lineHeight: 18,
    textAlign: 'center',
  },
  bullets: {
    gap: 6,
    paddingVertical: 6,
  },
  bullet: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  bulletText: {
    fontSize: 12,
    fontWeight: '500',
    flex: 1,
  },
  primaryBtn: {
    paddingVertical: 13,
    borderRadius: 10,
    alignItems: 'center',
    marginTop: 4,
  },
  primaryText: {
    fontSize: 14,
    fontWeight: '800',
    letterSpacing: 0.5,
  },
  secondaryBtn: {
    paddingVertical: 10,
    alignItems: 'center',
  },
  secondaryText: {
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0.4,
  },
});
