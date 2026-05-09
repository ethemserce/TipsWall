import { Pressable, StyleSheet, View } from 'react-native';
import { useTranslation } from 'react-i18next';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

export type FixtureFilter = 'all' | 'live' | 'upcoming' | 'finished';

interface StateFilterBarProps {
  selected: FixtureFilter;
  onSelect: (filter: FixtureFilter) => void;
  counts: Record<FixtureFilter, number>;
}

const ORDER: { key: FixtureFilter; i18nKey: string }[] = [
  { key: 'all', i18nKey: 'common.all' },
  { key: 'live', i18nKey: 'common.live' },
  { key: 'upcoming', i18nKey: 'common.upcoming' },
  { key: 'finished', i18nKey: 'common.finished' },
];

export function StateFilterBar({ selected, onSelect, counts }: StateFilterBarProps) {
  const c = useTheme();
  const { t } = useTranslation();

  return (
    <View style={[styles.bar, { borderBottomColor: c.border }]}>
      {ORDER.map(({ key, i18nKey }) => {
        const label = t(i18nKey);
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
