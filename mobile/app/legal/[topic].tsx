import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { router, useLocalSearchParams } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { Pressable, ScrollView, StyleSheet, View } from 'react-native';
import { SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { getLegalDoc, type LegalTopic } from '@/src/lib/legal/content';
import { useTheme } from '@/src/lib/useTheme';

// Single dynamic route serves every legal doc (terms / privacy / kvkk /
// disclaimer / imprint / contact / advertising). Long-form text lives
// in `src/lib/legal/content.ts` keyed by topic so this file stays
// presentational. Headings (lines starting with `#`) are emphasised;
// everything else is body paragraph.
const VALID_TOPICS: ReadonlySet<LegalTopic> = new Set([
  'terms',
  'privacy',
  'kvkk',
  'disclaimer',
  'imprint',
  'contact',
  'advertising',
]);

export default function LegalPage() {
  const { topic } = useLocalSearchParams<{ topic: string }>();
  const { i18n } = useTranslation();
  const c = useTheme();
  const insets = useSafeAreaInsets();

  const normalisedTopic: LegalTopic = VALID_TOPICS.has(topic as LegalTopic)
    ? (topic as LegalTopic)
    : 'terms';
  const doc = getLegalDoc(normalisedTopic, i18n.language);

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={[styles.headerRow, { borderBottomColor: c.borderSoft }]}>
        <Pressable
          onPress={() => router.back()}
          hitSlop={12}
          style={({ pressed }) => [
            styles.backBtn,
            { backgroundColor: pressed ? c.brandSoft : 'transparent' },
          ]}>
          <MaterialCommunityIcons name="chevron-left" size={24} color={c.text} />
        </Pressable>
        <ThemedText style={[styles.headerTitle, { color: c.text }]} numberOfLines={1}>
          {doc.title}
        </ThemedText>
        <View style={styles.backBtn} />
      </View>
      <ScrollView
        contentContainerStyle={[
          styles.scroll,
          // insets.bottom covers the gesture bar / home indicator height;
          // adding 24px buffer on top of it keeps the last paragraph
          // visible above the navigation surface on every device. The
          // SafeAreaView only consumes the top edge so the rest of the
          // screen still scrolls behind any translucent nav bar.
          { paddingBottom: insets.bottom + 24 },
        ]}>
        <ThemedText style={[styles.updated, { color: c.textMuted }]}>
          {doc.lastUpdated}
        </ThemedText>
        {doc.paragraphs.map((p, idx) => {
          if (p.startsWith('# ')) {
            return (
              <ThemedText
                key={idx}
                style={[styles.heading, { color: c.text }]}>
                {p.slice(2)}
              </ThemedText>
            );
          }
          return (
            <ThemedText
              key={idx}
              style={[styles.body, { color: c.textMuted }]}>
              {p}
            </ThemedText>
          );
        })}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 8,
    paddingVertical: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  backBtn: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerTitle: {
    flex: 1,
    textAlign: 'center',
    fontSize: 16,
    fontWeight: '800',
  },
  scroll: {
    paddingHorizontal: 20,
    paddingTop: 12,
    gap: 6,
  },
  updated: {
    fontSize: 11,
    marginBottom: 12,
  },
  heading: {
    fontSize: 14,
    fontWeight: '800',
    marginTop: 14,
    marginBottom: 2,
    letterSpacing: 0.2,
  },
  body: {
    fontSize: 13,
    lineHeight: 19,
  },
});
