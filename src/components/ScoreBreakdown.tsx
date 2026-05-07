import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureScore } from '@/src/types/fixtureDetail';

interface ScoreBreakdownProps {
  scores: FixtureScore[];
}

const SECTION_ORDER = ['1ST_HALF', '2ND_HALF', 'EXTRA_TIME', 'PENALTIES', 'NORMALTIME'];

const SECTION_LABEL: Record<string, string> = {
  '1ST_HALF': '1st Half',
  '2ND_HALF': '2nd Half',
  EXTRA_TIME: 'Extra Time',
  PENALTIES: 'Penalties',
  NORMALTIME: 'Full Time',
};

export function ScoreBreakdown({ scores }: ScoreBreakdownProps) {
  const c = useTheme();

  const rows = SECTION_ORDER.flatMap((description) => {
    const home = scores.find(
      (s) => s.description === description && s.participant_location === 'home',
    );
    const away = scores.find(
      (s) => s.description === description && s.participant_location === 'away',
    );
    if (!home && !away) return [];
    return [
      {
        key: description,
        label: SECTION_LABEL[description] ?? description,
        home: home?.goals ?? null,
        away: away?.goals ?? null,
      },
    ];
  });

  if (rows.length === 0) return null;

  return (
    <View style={[styles.container, { backgroundColor: c.bg, borderColor: c.border }]}>
      <ThemedText style={[styles.title, { color: c.textMuted }]}>
        SCORE BREAKDOWN
      </ThemedText>
      {rows.map((row) => (
        <View key={row.key} style={[styles.row, { borderTopColor: c.border }]}>
          <ThemedText style={[styles.label, { color: c.textMuted }]}>
            {row.label}
          </ThemedText>
          <ThemedText style={[styles.value, { color: c.text }]}>
            {row.home ?? '-'}
            <ThemedText style={[styles.dash, { color: c.textMuted }]}>{' : '}</ThemedText>
            {row.away ?? '-'}
          </ThemedText>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 10,
    overflow: 'hidden',
  },
  title: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.5,
    paddingHorizontal: 14,
    paddingTop: 12,
    paddingBottom: 8,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  label: {
    fontSize: 13,
    fontWeight: '500',
  },
  value: {
    fontSize: 14,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
  },
  dash: {
    fontWeight: '400',
  },
});
