import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

export type FixtureFilter = 'all' | 'live' | 'upcoming' | 'finished';

interface StateFilterBarProps {
  selected: FixtureFilter;
  onSelect: (filter: FixtureFilter) => void;
  counts: Record<FixtureFilter, number>;
}

const ORDER: { key: FixtureFilter; label: string }[] = [
  { key: 'all', label: 'All' },
  { key: 'live', label: 'Live' },
  { key: 'upcoming', label: 'Upcoming' },
  { key: 'finished', label: 'Finished' },
];

export function StateFilterBar({ selected, onSelect, counts }: StateFilterBarProps) {
  const c = useTheme();

  return (
    <View style={[styles.bar, { borderBottomColor: c.border }]}>
      {ORDER.map(({ key, label }) => {
        const active = key === selected;
        const showLiveDot = key === 'live' && counts.live > 0;
        return (
          <Pressable
            key={key}
            onPress={() => onSelect(key)}
            style={[
              styles.tab,
              active && { borderBottomColor: c.brand },
            ]}>
            <View style={styles.tabRow}>
              {showLiveDot ? (
                <View style={[styles.liveDot, { backgroundColor: c.live }]} />
              ) : null}
              <ThemedText
                style={[
                  styles.tabText,
                  { color: active ? c.text : c.textMuted },
                ]}>
                {label}
              </ThemedText>
              <View style={[styles.count, { backgroundColor: active ? c.brand : c.surface }]}>
                <ThemedText
                  style={[
                    styles.countText,
                    { color: active ? c.textInverse : c.textMuted },
                  ]}>
                  {counts[key]}
                </ThemedText>
              </View>
            </View>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  bar: {
    flexDirection: 'row',
    paddingHorizontal: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  tab: {
    flex: 1,
    paddingVertical: 12,
    paddingHorizontal: 8,
    alignItems: 'center',
    borderBottomWidth: 2,
    borderBottomColor: 'transparent',
  },
  tabRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  tabText: {
    fontSize: 13,
    fontWeight: '600',
  },
  count: {
    paddingHorizontal: 6,
    paddingVertical: 1,
    borderRadius: 999,
    minWidth: 20,
    alignItems: 'center',
  },
  countText: {
    fontSize: 10,
    fontWeight: '700',
  },
  liveDot: {
    width: 6,
    height: 6,
    borderRadius: 3,
  },
});
