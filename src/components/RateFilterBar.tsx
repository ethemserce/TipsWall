import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

export type WindowCode = '1m' | '3m' | '6m' | '1y' | 'all';

const WINDOWS: { key: WindowCode; label: string }[] = [
  { key: '1m', label: '1 Ay' },
  { key: '3m', label: '3 Ay' },
  { key: '6m', label: '6 Ay' },
  { key: '1y', label: '1 Yıl' },
  { key: 'all', label: 'Tümü' },
];

export interface RateFilters {
  window: WindowCode;
  minRate: number | null;
  notStartedOnly: boolean;
}

interface RateFilterBarProps {
  filters: RateFilters;
  onChange: (next: RateFilters) => void;
}

const MIN_RATES: (number | null)[] = [null, 1.3, 1.5, 1.8, 2.0, 2.5, 3.0];

export function RateFilterBar({ filters, onChange }: RateFilterBarProps) {
  const c = useTheme();

  return (
    <View style={[styles.bar, { borderBottomColor: c.border }]}>
      <ScrollView
        horizontal
        showsHorizontalScrollIndicator={false}
        contentContainerStyle={styles.row}>
        {/* Window picker */}
        <View style={styles.group}>
          <ThemedText style={[styles.groupLabel, { color: c.textMuted }]}>PERİYOT</ThemedText>
          <View style={[styles.pillGroup, { borderColor: c.border }]}>
            {WINDOWS.map(({ key, label }) => {
              const active = filters.window === key;
              return (
                <Pressable
                  key={key}
                  onPress={() => onChange({ ...filters, window: key })}
                  style={[
                    styles.pill,
                    active && { backgroundColor: c.brand },
                  ]}>
                  <ThemedText
                    style={[
                      styles.pillText,
                      { color: active ? c.textInverse : c.text },
                    ]}>
                    {label}
                  </ThemedText>
                </Pressable>
              );
            })}
          </View>
        </View>

        {/* Min odd picker */}
        <View style={styles.group}>
          <ThemedText style={[styles.groupLabel, { color: c.textMuted }]}>MİN ORAN</ThemedText>
          <View style={[styles.pillGroup, { borderColor: c.border }]}>
            {MIN_RATES.map((value) => {
              const active = filters.minRate === value;
              return (
                <Pressable
                  key={value ?? 'any'}
                  onPress={() => onChange({ ...filters, minRate: value })}
                  style={[
                    styles.pill,
                    active && { backgroundColor: c.brand },
                  ]}>
                  <ThemedText
                    style={[
                      styles.pillText,
                      { color: active ? c.textInverse : c.text },
                    ]}>
                    {value == null ? 'Tümü' : value.toFixed(2)}
                  </ThemedText>
                </Pressable>
              );
            })}
          </View>
        </View>

        {/* Not-started toggle */}
        <View style={styles.group}>
          <ThemedText style={[styles.groupLabel, { color: c.textMuted }]}>DURUM</ThemedText>
          <Pressable
            onPress={() =>
              onChange({ ...filters, notStartedOnly: !filters.notStartedOnly })
            }
            style={[
              styles.pillGroup,
              styles.toggleSingle,
              {
                borderColor: c.border,
                backgroundColor: filters.notStartedOnly ? c.brand : 'transparent',
              },
            ]}>
            <MaterialCommunityIcons
              name={filters.notStartedOnly ? 'check-circle' : 'circle-outline'}
              size={14}
              color={filters.notStartedOnly ? c.textInverse : c.textMuted}
            />
            <ThemedText
              style={[
                styles.pillText,
                { color: filters.notStartedOnly ? c.textInverse : c.text },
              ]}>
              Sadece Başlamayan
            </ThemedText>
          </Pressable>
        </View>
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  bar: {
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  row: {
    paddingHorizontal: 12,
    paddingVertical: 10,
    gap: 16,
  },
  group: {
    gap: 4,
  },
  groupLabel: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.4,
    paddingLeft: 4,
  },
  pillGroup: {
    flexDirection: 'row',
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 999,
    padding: 2,
    gap: 2,
  },
  pill: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 999,
  },
  pillText: {
    fontSize: 12,
    fontWeight: '600',
  },
  toggleSingle: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10,
    gap: 6,
  },
});
