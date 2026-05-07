import { addDays, format, isSameDay, startOfDay } from 'date-fns';
import { useMemo } from 'react';
import { Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';

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
  const days = useMemo(() => {
    const today = startOfDay(new Date());
    const items: Date[] = [];
    for (let i = -daysBack; i <= daysForward; i++) {
      items.push(addDays(today, i));
    }
    return items;
  }, [daysBack, daysForward]);

  return (
    <ScrollView
      horizontal
      showsHorizontalScrollIndicator={false}
      contentContainerStyle={styles.container}>
      {days.map((day) => {
        const active = isSameDay(day, selectedDate);
        return (
          <Pressable
            key={day.toISOString()}
            onPress={() => onSelect(day)}
            style={[styles.cell, active && styles.cellActive]}>
            <ThemedText style={[styles.weekday, active && styles.textActive]}>
              {format(day, 'EEE').toUpperCase()}
            </ThemedText>
            <ThemedText style={[styles.day, active && styles.textActive]}>
              {format(day, 'd MMM')}
            </ThemedText>
          </Pressable>
        );
      })}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
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
  cellActive: {
    backgroundColor: '#2563eb',
  },
  weekday: {
    fontSize: 11,
    opacity: 0.6,
    letterSpacing: 0.5,
  },
  day: {
    fontSize: 14,
    fontWeight: '600',
    marginTop: 2,
  },
  textActive: {
    color: '#fff',
    opacity: 1,
  },
});
