import { useRouter } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { DISCLAIMER_SHORT_EN, DISCLAIMER_SHORT_TR } from '@/src/lib/legal/content';
import { useTheme } from '@/src/lib/useTheme';

/**
 * Tiny app-wide footer line reminding the user that everything in the
 * app is informational. Shown at the bottom of high-traffic screens
 * (analysis, home, fixture detail). Tapping routes to the full
 * Sorumluluk Reddi page for the long-form text.
 */
export function AppDisclaimerFooter() {
  const c = useTheme();
  const router = useRouter();
  const { i18n } = useTranslation();
  const tr = (i18n.language ?? '').toLowerCase().startsWith('tr');
  const text = tr ? DISCLAIMER_SHORT_TR : DISCLAIMER_SHORT_EN;

  return (
    <Pressable
      onPress={() => router.push('/legal/disclaimer' as never)}
      style={({ pressed }) => [
        styles.wrap,
        { backgroundColor: pressed ? c.brandSoft : 'transparent' },
      ]}
      accessibilityRole="button">
      <ThemedText
        style={[styles.text, { color: c.textMuted }]}
        numberOfLines={3}>
        {text}
      </ThemedText>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  wrap: {
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  text: {
    fontSize: 11,
    lineHeight: 15,
    textAlign: 'center',
    letterSpacing: 0.1,
  },
});
