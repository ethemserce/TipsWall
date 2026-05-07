import { Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

export type DetailTab = 'details' | 'stats' | 'lineups' | 'h2h';

interface DetailTabBarProps {
  selected: DetailTab;
  onSelect: (tab: DetailTab) => void;
}

const ORDER: { key: DetailTab; label: string }[] = [
  { key: 'details', label: 'Details' },
  { key: 'stats', label: 'Stats' },
  { key: 'lineups', label: 'Lineups' },
  { key: 'h2h', label: 'H2H' },
];

export function DetailTabBar({ selected, onSelect }: DetailTabBarProps) {
  const c = useTheme();

  return (
    <View style={[styles.bar, { backgroundColor: c.bg, borderBottomColor: c.border }]}>
      <ScrollView
        horizontal
        showsHorizontalScrollIndicator={false}
        contentContainerStyle={styles.row}>
        {ORDER.map(({ key, label }) => {
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
