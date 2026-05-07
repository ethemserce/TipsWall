import { addDays, format, isSameDay, startOfDay } from 'date-fns';
import { useMemo } from 'react';
import { Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

interface DateBarProps {
  selectedDate: Date;
  onSelect: (date: Date) => void;
  daysBack?: number;
  daysForward?: number;
}

export function DateBar({
  selectedDate,
  onSelect,
  daysBack = 3,
  daysForward = 7,
}: DateBarProps) {
  const c = useTheme();
  const days = useMemo(() => {
    const today = startOfDay(new Date());
    const items: Date[] = [];
    for (let i = -daysBack; i <= daysForward; i++) {
      items.push(addDays(today, i));
    }
    return items;
  }, [daysBack, daysForward]);

  return (
    <View style={[styles.container, { borderBottomColor: c.border }]}>
      <ScrollView
        horizontal
        showsHorizontalScrollIndicator={false}
        contentContainerStyle={styles.row}>
        {days.map((day) => {
          const active = isSameDay(day, selectedDate);
          const isToday = isSameDay(day, new Date());
          return (
            <Pressable
              key={day.toISOString()}
              onPress={() => onSelect(day)}
              style={[
                styles.cell,
                active && { backgroundColor: c.brand },
              ]}>
              <ThemedText
                style={[
                  styles.weekday,
                  { color: active ? c.textInverse : c.textMuted },
                ]}>
                {isToday ? 'TODAY' : format(day, 'EEE').toUpperCase()}
              </ThemedText>
              <ThemedText
                style={[
                  styles.day,
                  { color: active ? c.textInverse : c.text },
                ]}>
                {format(day, 'd MMM')}
              </ThemedText>
            </Pressable>
          );
        })}
      </ScrollView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  row: {
    paddingHorizontal: 12,
    paddingVertical: 8,
    gap: 8,
  },
  cell: {
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 10,
    minWidth: 64,
    alignItems: 'center',
    backgroundColor: 'transparent',
  },
  weekday: {
    fontSize: 10,
    letterSpacing: 0.5,
    fontWeight: '600',
  },
  day: {
    fontSize: 14,
    fontWeight: '600',
    marginTop: 2,
  },
});
