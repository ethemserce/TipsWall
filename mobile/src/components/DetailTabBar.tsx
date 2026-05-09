import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import {
  type LayoutChangeEvent,
  Pressable,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';

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
  const scrollRef = useRef<ScrollView | null>(null);
  const tabLayoutsRef = useRef<Partial<Record<DetailTab, { x: number; width: number }>>>({});
  const containerWidthRef = useRef(0);

  // Centre the active tab whenever selection changes — without this the
  // user can swipe to a tab whose label is clipped off-screen and lose
  // the visual cue for what they're now viewing.
  const scrollToSelected = (animated: boolean) => {
    const layout = tabLayoutsRef.current[selected];
    const cw = containerWidthRef.current;
    if (!layout || !scrollRef.current || cw === 0) return;
    const target = Math.max(0, layout.x + layout.width / 2 - cw / 2);
    scrollRef.current.scrollTo({ x: target, animated });
  };

  useEffect(() => {
    scrollToSelected(true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selected]);

  return (
    <View
      style={[styles.bar, { backgroundColor: c.bg, borderBottomColor: c.border }]}
      onLayout={(e) => {
        containerWidthRef.current = e.nativeEvent.layout.width;
        // Initial layout: the selected tab may already be off-centre on
        // mount (e.g., the user landed on a tab past the first one).
        scrollToSelected(false);
      }}>
      <ScrollView
        ref={scrollRef}
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
              onLayout={(e: LayoutChangeEvent) => {
                tabLayoutsRef.current[key] = {
                  x: e.nativeEvent.layout.x,
                  width: e.nativeEvent.layout.width,
                };
                if (key === selected) scrollToSelected(false);
              }}
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
