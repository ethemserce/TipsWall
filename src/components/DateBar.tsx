import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { addDays, format, isSameDay, isToday, startOfMonth } from 'date-fns';
import { enUS, tr as trLocale } from 'date-fns/locale';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal, Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

function dfLocale(lang: string) {
  return lang.startsWith('tr') ? trLocale : enUS;
}

interface DateBarProps {
  selectedDate: Date;
  onSelect: (date: Date) => void;
}

export function DateBar({ selectedDate, onSelect }: DateBarProps) {
  const c = useTheme();
  const { t, i18n } = useTranslation();
  const [calendarOpen, setCalendarOpen] = useState(false);
  const locale = dfLocale(i18n.language);

  const pillLabel = isToday(selectedDate)
    ? t('common.today')
    : format(selectedDate, 'd MMM', { locale });

  return (
    <View style={styles.bar}>
      <View style={[styles.pillGroup, { backgroundColor: c.surface, borderColor: c.border }]}>
        <Pressable
          onPress={() => onSelect(addDays(selectedDate, -1))}
          style={styles.arrowBtn}>
          <MaterialCommunityIcons name="chevron-left" size={20} color={c.text} />
        </Pressable>
        <Pressable onPress={() => setCalendarOpen(true)} style={styles.pillCenter}>
          <ThemedText style={[styles.pillLabel, { color: c.text }]}>
            {pillLabel}
          </ThemedText>
        </Pressable>
        <Pressable
          onPress={() => onSelect(addDays(selectedDate, 1))}
          style={styles.arrowBtn}>
          <MaterialCommunityIcons name="chevron-right" size={20} color={c.text} />
        </Pressable>
      </View>

      <CalendarModal
        visible={calendarOpen}
        onClose={() => setCalendarOpen(false)}
        selectedDate={selectedDate}
        onSelect={(d) => {
          onSelect(d);
          setCalendarOpen(false);
        }}
      />
    </View>
  );
}

function CalendarModal({
  visible,
  onClose,
  selectedDate,
  onSelect,
}: {
  visible: boolean;
  onClose: () => void;
  selectedDate: Date;
  onSelect: (d: Date) => void;
}) {
  const c = useTheme();
  const { t, i18n } = useTranslation();
  const locale = dfLocale(i18n.language);
  const [cursor, setCursor] = useState(() => startOfMonth(selectedDate));

  const days = buildMonthGrid(cursor);

  return (
    <Modal
      visible={visible}
      transparent
      animationType="slide"
      onRequestClose={onClose}>
      <Pressable style={styles.backdrop} onPress={onClose} />
      <View style={[styles.sheet, { backgroundColor: c.bg, borderColor: c.border }]}>
        <View style={styles.sheetHandle}>
          <View style={[styles.handleBar, { backgroundColor: c.border }]} />
        </View>

        <View style={styles.monthRow}>
          <Pressable
            onPress={() =>
              setCursor((d) => addDays(startOfMonth(d), -1))
            }
            style={styles.arrowBtn}>
            <MaterialCommunityIcons name="chevron-left" size={22} color={c.brand} />
          </Pressable>
          <ThemedText style={[styles.monthLabel, { color: c.text }]}>
            {format(cursor, 'MMMM yyyy', { locale })}
          </ThemedText>
          <Pressable
            onPress={() =>
              setCursor((d) =>
                startOfMonth(addDays(startOfMonth(d), 32)),
              )
            }
            style={styles.arrowBtn}>
            <MaterialCommunityIcons name="chevron-right" size={22} color={c.brand} />
          </Pressable>
        </View>

        <View style={styles.weekRow}>
          {weekLabels(locale).map((w) => (
            <ThemedText key={w} style={[styles.weekLabel, { color: c.textMuted }]}>
              {w}
            </ThemedText>
          ))}
        </View>

        <View style={styles.grid}>
          {days.map((cell, i) => {
            if (!cell) {
              return <View key={`empty-${i}`} style={styles.cell} />;
            }
            const active = isSameDay(cell, selectedDate);
            return (
              <Pressable
                key={cell.toISOString()}
                onPress={() => onSelect(cell)}
                style={[
                  styles.cell,
                  active && { backgroundColor: c.brand, borderRadius: 999 },
                ]}>
                <ThemedText
                  style={[
                    styles.cellText,
                    { color: active ? c.textInverse : c.text },
                  ]}>
                  {format(cell, 'd')}
                </ThemedText>
              </Pressable>
            );
          })}
        </View>

        <Pressable
          onPress={() => onSelect(new Date())}
          style={[styles.todayBtn, { backgroundColor: c.brand }]}>
          <ThemedText style={[styles.todayBtnText, { color: c.textInverse }]}>
            {t('common.today').toUpperCase()}
          </ThemedText>
        </Pressable>
      </View>
    </Modal>
  );
}

function weekLabels(locale: typeof enUS): string[] {
  const monday = new Date(2024, 0, 1);
  return Array.from({ length: 7 }, (_, i) =>
    format(addDays(monday, i), 'EEE', { locale }),
  );
}

function buildMonthGrid(month: Date): (Date | null)[] {
  const first = startOfMonth(month);
  // Monday-based: 0=Mon, 6=Sun
  const offset = (first.getDay() + 6) % 7;
  const cells: (Date | null)[] = [];
  for (let i = 0; i < offset; i++) cells.push(null);
  let cur = first;
  while (cur.getMonth() === first.getMonth()) {
    cells.push(cur);
    cur = addDays(cur, 1);
  }
  while (cells.length % 7 !== 0) cells.push(null);
  return cells;
}

const styles = StyleSheet.create({
  bar: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 8,
    gap: 12,
  },
  pillGroup: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    height: 36,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
    paddingHorizontal: 4,
  },
  arrowBtn: {
    width: 32,
    height: 32,
    alignItems: 'center',
    justifyContent: 'center',
  },
  pillCenter: {
    flex: 1,
    alignItems: 'center',
  },
  pillLabel: {
    fontSize: 14,
    fontWeight: '600',
  },
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
  },
  sheet: {
    paddingTop: 8,
    paddingBottom: 24,
    paddingHorizontal: 16,
    borderTopLeftRadius: 24,
    borderTopRightRadius: 24,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  sheetHandle: {
    alignItems: 'center',
    marginBottom: 8,
  },
  handleBar: {
    width: 36,
    height: 4,
    borderRadius: 2,
  },
  closeBtn: {
    position: 'absolute',
    top: 12,
    right: 16,
    padding: 6,
  },
  monthRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingTop: 8,
    paddingBottom: 12,
  },
  monthLabel: {
    fontSize: 16,
    fontWeight: '600',
  },
  weekRow: {
    flexDirection: 'row',
    paddingHorizontal: 4,
    paddingBottom: 6,
  },
  weekLabel: {
    flex: 1,
    textAlign: 'center',
    fontSize: 11,
    fontWeight: '600',
    letterSpacing: 0.3,
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    paddingHorizontal: 4,
  },
  cell: {
    width: `${100 / 7}%`,
    aspectRatio: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  cellText: {
    fontSize: 14,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
  },
  todayBtn: {
    marginTop: 16,
    height: 44,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  todayBtnText: {
    fontSize: 14,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
});
