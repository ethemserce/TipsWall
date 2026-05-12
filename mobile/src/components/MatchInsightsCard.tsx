import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureMatchFact } from '@/src/types/fixtureDetailExtras';

interface MatchInsightsCardProps {
  facts: FixtureMatchFact[] | undefined;
  collapsedCount?: number;
}

// SportMonks bundles 700+ pre-match factoids per fixture. Even after we
// drop rows without natural_language, the list runs long, so the card
// shows a digestible slice and reveals the rest behind an inline toggle.
export function MatchInsightsCard({
  facts,
  collapsedCount = 5,
}: MatchInsightsCardProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const [expanded, setExpanded] = useState(false);

  const visible = useMemo(() => {
    if (!facts || facts.length === 0) return [];
    if (expanded) return facts;
    return facts.slice(0, collapsedCount);
  }, [facts, expanded, collapsedCount]);

  if (!facts || facts.length === 0) return null;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <View style={styles.headerRow}>
        <MaterialCommunityIcons
          name="lightbulb-outline"
          size={16}
          color={c.textMuted}
        />
        <ThemedText style={[styles.title, { color: c.textMuted }]}>
          {t('fixture.insights.title').toUpperCase()}
        </ThemedText>
      </View>
      <View style={styles.list}>
        {visible.map((f, i) => (
          <View
            key={f.id}
            style={[
              styles.row,
              i > 0 && {
                borderTopWidth: StyleSheet.hairlineWidth,
                borderTopColor: c.border,
              },
            ]}>
            <View style={[styles.bullet, { backgroundColor: c.brand }]} />
            <ThemedText style={[styles.text, { color: c.text }]}>
              {f.natural_language}
            </ThemedText>
          </View>
        ))}
      </View>
      {facts.length > collapsedCount ? (
        <Pressable
          onPress={() => setExpanded((v) => !v)}
          style={styles.toggle}
          hitSlop={8}>
          <ThemedText style={[styles.toggleText, { color: c.brand }]}>
            {expanded
              ? t('fixture.insights.showLess')
              : t('fixture.insights.showMore', {
                  count: facts.length - collapsedCount,
                })}
          </ThemedText>
        </Pressable>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    marginBottom: 8,
  },
  title: {
    fontSize: 11,
    fontWeight: '600',
    letterSpacing: 0.6,
  },
  list: {},
  row: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 8,
    paddingVertical: 10,
  },
  bullet: {
    width: 5,
    height: 5,
    borderRadius: 2.5,
    marginTop: 7,
  },
  text: {
    flex: 1,
    fontSize: 13,
    lineHeight: 18,
  },
  toggle: {
    paddingTop: 10,
    alignSelf: 'flex-start',
  },
  toggleText: {
    fontSize: 12,
    fontWeight: '600',
  },
});
