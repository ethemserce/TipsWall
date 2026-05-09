import { useTranslation } from 'react-i18next';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureScore } from '@/src/types/fixtureDetail';

interface ScoreBreakdownProps {
  scores: FixtureScore[];
  homeName?: string | null;
  awayName?: string | null;
}

const SECTION_ORDER = ['1ST_HALF', '2ND_HALF', 'EXTRA_TIME', 'PENALTIES', 'NORMALTIME'];

const SECTION_I18N: Record<string, string> = {
  '1ST_HALF': 'fixture.score.firstHalf',
  '2ND_HALF': 'fixture.score.secondHalf',
  EXTRA_TIME: 'fixture.score.extraTime',
  PENALTIES: 'fixture.score.penalties',
  NORMALTIME: 'fixture.score.fullTime',
};

export function ScoreBreakdown({ scores, homeName, awayName }: ScoreBreakdownProps) {
  const c = useTheme();
  const { t } = useTranslation();

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
        label: SECTION_I18N[description] ? t(SECTION_I18N[description]) : description,
        home: home?.goals ?? null,
        away: away?.goals ?? null,
      },
    ];
  });

  if (rows.length === 0) return null;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <ThemedText style={[styles.title, { color: c.textMuted }]}>
        {t('fixture.score.title').toUpperCase()}
      </ThemedText>

      <View style={styles.headerRow}>
        <ThemedText style={[styles.headerLabel, { color: c.textMuted }]} />
        <View style={styles.scoreCells}>
          <ThemedText
            style={[styles.headerCell, { color: c.textMuted }]}
            numberOfLines={1}>
            {abbreviate(homeName) ?? 'H'}
          </ThemedText>
          <ThemedText
            style={[styles.headerCell, { color: c.textMuted }]}
            numberOfLines={1}>
            {abbreviate(awayName) ?? 'A'}
          </ThemedText>
        </View>
      </View>

      {rows.map((row) => (
        <View
          key={row.key}
          style={[styles.row, { borderTopColor: c.border }]}>
          <ThemedText style={[styles.label, { color: c.text }]}>
            {row.label}
          </ThemedText>
          <View style={styles.scoreCells}>
            <ThemedText style={[styles.cell, { color: c.text }]}>
              {row.home ?? '-'}
            </ThemedText>
            <ThemedText style={[styles.cell, { color: c.text }]}>
              {row.away ?? '-'}
            </ThemedText>
          </View>
        </View>
      ))}
    </View>
  );
}

function abbreviate(name: string | null | undefined): string | null {
  if (!name) return null;
  return name.length > 3 ? name.slice(0, 3).toUpperCase() : name.toUpperCase();
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    overflow: 'hidden',
  },
  title: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.5,
    paddingHorizontal: 14,
    paddingTop: 12,
    paddingBottom: 6,
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 14,
    paddingBottom: 6,
  },
  headerLabel: {
    flex: 1,
  },
  headerCell: {
    width: 32,
    textAlign: 'center',
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  label: {
    flex: 1,
    fontSize: 13,
    fontWeight: '500',
  },
  scoreCells: {
    flexDirection: 'row',
    gap: 16,
  },
  cell: {
    width: 32,
    textAlign: 'center',
    fontSize: 14,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
  },
});
