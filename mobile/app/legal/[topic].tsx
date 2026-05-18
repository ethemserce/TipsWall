import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { router, useLocalSearchParams } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { Pressable, ScrollView, StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { getLegalDoc, type LegalTopic } from '@/src/lib/legal/content';
import { useTheme } from '@/src/lib/useTheme';

// Single dynamic route serves all three legal docs (terms / privacy /
// kvkk). Long-form text lives in `src/lib/legal/content.ts` keyed by
// topic so this file stays presentational. Headings (lines starting
// with `#`) are emphasised; everything else is body paragraph.
export default function LegalPage() {
  const { topic } = useLocalSearchParams<{ topic: string }>();
  const { i18n } = useTranslation();
  const c = useTheme();

  const normalisedTopic = (topic === 'terms' || topic === 'privacy' || topic === 'kvkk'
    ? topic
    : 'terms') as LegalTopic;
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
      <ScrollView contentContainerStyle={styles.scroll}>
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
    paddingBottom: 40,
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
