import { useTranslation } from 'react-i18next';
import { Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

// User-visible windows. '1y' and 'all' are kept in the union as
// deprecated codes so persisted state from older app versions still
// type-checks; the backend continues to produce snapshots for them
// (additive rollback safety). New UI never offers them.
export type WindowCode =
  | '1m'
  | '3m'
  | '6m'
  | '1y'
  | 'all'
  | 'season_current'
  | 'season_2y';

const WINDOWS: WindowCode[] = [
  '1m',
  '3m',
  '6m',
  'season_current',
  'season_2y',
];

export type RateBound = 'min' | 'max';

export interface RateFilters {
  window: WindowCode;
  rateValue: number | null;
  rateBound: RateBound;
}

interface RateFilterBarProps {
  filters: RateFilters;
  onChange: (next: RateFilters) => void;
}

const RATE_VALUES: number[] = [1.5, 1.8, 2.5, 4.0, 10.0];

// Compact labels keyed by WindowCode. Pulled from i18n so they read as
// "1m / 3m / 6m / 1y / All" in English and "1 Ay / 3 Ay / ... / Tümü"
// when the device is set to Turkish.
const WINDOW_LABEL_KEY: Record<WindowCode, string> = {
  '1m': 'rate.filters.windowsShort.1m',
  '3m': 'rate.filters.windowsShort.3m',
  '6m': 'rate.filters.windowsShort.6m',
  '1y': 'rate.filters.windowsShort.1y',
  all: 'rate.filters.windowsShort.all',
  season_current: 'rate.filters.windowsShort.season_current',
  season_2y: 'rate.filters.windowsShort.season_2y',
};

export function RateFilterBar({ filters, onChange }: RateFilterBarProps) {
  const c = useTheme();
  const { t } = useTranslation();

  const toggleBound = () =>
    onChange({
      ...filters,
      rateBound: filters.rateBound === 'min' ? 'max' : 'min',
    });

  return (
    <View
      style={[
        styles.bar,
        { backgroundColor: c.surface, borderBottomColor: c.border },
      ]}>
      {/* Period — full-width segmented control */}
      <View style={[styles.segmented, { backgroundColor: c.bg, borderColor: c.border }]}>
        {WINDOWS.map((key) => {
          const active = filters.window === key;
          return (
            <Pressable
              key={key}
              onPress={() => onChange({ ...filters, window: key })}
              style={[
                styles.segment,
                active && { backgroundColor: c.brand },
              ]}>
              <ThemedText
                style={[
                  styles.segmentText,
                  { color: active ? c.textInverse : c.text },
                ]}>
                {t(WINDOW_LABEL_KEY[key])}
              </ThemedText>
            </Pressable>
          );
        })}
      </View>

      {/* Rate row: Min/Max toggle button + scrollable rate pills */}
      <View style={styles.rateRow}>
        <Pressable
          onPress={toggleBound}
          style={[
            styles.boundToggle,
            { backgroundColor: c.brand, borderColor: c.brand },
          ]}>
          <ThemedText style={[styles.boundSymbol, { color: c.textInverse }]}>
            {filters.rateBound === 'min' ? '≥' : '≤'}
          </ThemedText>
          <ThemedText style={[styles.boundLabel, { color: c.textInverse }]}>
            {filters.rateBound === 'min'
              ? t('rate.filters.bound.min')
              : t('rate.filters.bound.max')}
          </ThemedText>
        </Pressable>

        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.ratePillsContent}>
          <Pressable
            onPress={() => onChange({ ...filters, rateValue: null })}
            style={[
              styles.ratePill,
              { borderColor: c.border },
              filters.rateValue == null && {
                backgroundColor: c.brand,
                borderColor: c.brand,
              },
            ]}>
            <ThemedText
              style={[
                styles.ratePillText,
                {
                  color:
                    filters.rateValue == null ? c.textInverse : c.textMuted,
                },
              ]}>
              {t('common.all')}
            </ThemedText>
          </Pressable>
          {RATE_VALUES.map((value) => {
            const active = filters.rateValue === value;
            return (
              <Pressable
                key={value}
                onPress={() => onChange({ ...filters, rateValue: value })}
                style={[
                  styles.ratePill,
                  { borderColor: c.border },
                  active && {
                    backgroundColor: c.brand,
                    borderColor: c.brand,
                  },
                ]}>
                <ThemedText
                  style={[
                    styles.ratePillText,
                    { color: active ? c.textInverse : c.text },
                  ]}>
                  {value.toFixed(2)}
                </ThemedText>
              </Pressable>
            );
          })}
        </ScrollView>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  bar: {
    paddingHorizontal: 12,
    paddingVertical: 10,
    gap: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  segmented: {
    flexDirection: 'row',
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 3,
    gap: 2,
  },
  segment: {
    flex: 1,
    paddingVertical: 7,
    borderRadius: 7,
    alignItems: 'center',
    justifyContent: 'center',
  },
  segmentText: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
  rateRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  boundToggle: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10,
    paddingVertical: 8,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
    gap: 4,
  },
  boundSymbol: {
    fontSize: 14,
    fontWeight: '800',
    lineHeight: 14,
  },
  boundLabel: {
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
  ratePillsContent: {
    paddingRight: 4,
    gap: 6,
    alignItems: 'center',
  },
  ratePill: {
    paddingHorizontal: 12,
    paddingVertical: 7,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
    minWidth: 56,
    alignItems: 'center',
  },
  ratePillText: {
    fontSize: 12,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
    letterSpacing: 0.3,
  },
});
