import { useTranslation } from 'react-i18next';
import { Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

export type DetailTab = 'details' | 'odds' | 'stats' | 'lineups' | 'h2h' | 'standings';

interface DetailTabBarProps {
  selected: DetailTab;
  onSelect: (tab: DetailTab) => void;
}

const ORDER: { key: DetailTab; i18nKey: string }[] = [
  { key: 'details', i18nKey: 'fixture.tabs.details' },
  { key: 'odds', i18nKey: 'fixture.tabs.odds' },
  { key: 'stats', i18nKey: 'fixture.tabs.stats' },
  { key: 'lineups', i18nKey: 'fixture.tabs.lineups' },
  { key: 'h2h', i18nKey: 'fixture.tabs.h2h' },
  { key: 'standings', i18nKey: 'fixture.tabs.standings' },
];

export function DetailTabBar({ selected, onSelect }: DetailTabBarProps) {
  const c = useTheme();
  const { t } = useTranslation();

  return (
    <View style={[styles.bar, { backgroundColor: c.bg, borderBottomColor: c.border }]}>
      <ScrollView
        horizontal
        showsHorizontalScrollIndicator={false}
        contentContainerStyle={styles.row}>
        {ORDER.map(({ key, i18nKey }) => {
          const label = t(i18nKey);
          const active = key === selected;
          return (
            <Pressable
              key={key}
              onPress={() => onSelect(key)}
              style={[styles.tab, active && { borderBottomColor: c.brand }]}>
              <ThemedText
                style={[
                  styles.tabText,
                  { color: active ? c.text : c.textMuted },
                ]}>
                {label}
              </ThemedText>
            </Pressable>
          );
        })}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  bar: {
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  row: {
    paddingHorizontal: 8,
  },
  tab: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 2,
    borderBottomColor: 'transparent',
  },
  tabText: {
    fontSize: 14,
    fontWeight: '600',
  },
});
