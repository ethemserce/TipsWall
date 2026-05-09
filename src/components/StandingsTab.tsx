import { Image } from 'expo-image';
import { useTranslation } from 'react-i18next';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { TabEmpty, TabError, TabLoading } from '@/src/components/TabFeedback';
import { useTheme } from '@/src/lib/useTheme';
import type { LeagueTableRow } from '@/src/types/standings';

interface StandingsTabProps {
  loading: boolean;
  error?: unknown;
  rows: LeagueTableRow[];
  // The fixture's two team ids — we highlight their rows so the user can
  // spot them at a glance without scrolling the table.
  highlightTeamIds: ReadonlyArray<number | null | undefined>;
}

export function StandingsTab({
  loading,
  error,
  rows,
  highlightTeamIds,
}: StandingsTabProps) {
  const c = useTheme();
  const { t } = useTranslation();

  if (error && rows.length === 0) return <TabError error={error} />;
  if (loading && rows.length === 0) return <TabLoading />;
  if (rows.length === 0)
    return <TabEmpty icon="podium" message={t('fixture.standings.notAvailable')} />;

  const highlightSet = new Set(
    highlightTeamIds.filter((id): id is number => id != null),
  );

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <View style={[styles.headerRow, { borderBottomColor: c.border }]}>
        <ThemedText style={[styles.headerCell, styles.cellPos, { color: c.textMuted }]}>
          #
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellTeam, { color: c.textMuted }]}>
          {t('fixture.standings.team')}
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNum, { color: c.textMuted }]}>
          O
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNum, { color: c.textMuted }]}>
          G
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNum, { color: c.textMuted }]}>
          B
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNum, { color: c.textMuted }]}>
          M
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNumWide, { color: c.textMuted }]}>
          AV
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNum, { color: c.textMuted }]}>
          P
        </ThemedText>
      </View>

      {rows.map((row) => {
        const highlighted = row.team_id != null && highlightSet.has(row.team_id);
        return (
          <View
            key={row.team_id ?? row.position ?? Math.random()}
            style={[
              styles.row,
              { borderTopColor: c.border },
              highlighted && { backgroundColor: c.bg },
            ]}>
            <ThemedText
              style={[
                styles.cellPos,
                styles.cellText,
                { color: highlighted ? c.brand : c.textMuted, fontWeight: '700' },
              ]}>
              {row.position ?? '–'}
            </ThemedText>
            <View style={[styles.cellTeam, styles.teamCell]}>
              {row.team_image_path ? (
                <Image
                  source={{ uri: row.team_image_path }}
                  style={styles.logo}
                  contentFit="contain"
                />
              ) : (
                <View style={[styles.logoPlaceholder, { backgroundColor: c.border }]} />
              )}
              <ThemedText
                style={[
                  styles.teamName,
                  { color: highlighted ? c.text : c.text, fontWeight: highlighted ? '700' : '500' },
                ]}
                numberOfLines={1}>
                {row.team_name ?? '—'}
              </ThemedText>
            </View>
            <Cell value={row.played} c={c} />
            <Cell value={row.wins} c={c} />
            <Cell value={row.draws} c={c} />
            <Cell value={row.losses} c={c} />
            <Cell
              value={row.goal_difference > 0
                ? `+${row.goal_difference}`
                : `${row.goal_difference}`}
              c={c}
              wide
            />
            <ThemedText
              style={[
                styles.cellNum,
                styles.cellText,
                styles.numberValue,
                { color: c.text, fontWeight: '700' },
              ]}>
              {row.points}
            </ThemedText>
          </View>
        );
      })}

      <View style={[styles.legend, { borderTopColor: c.border }]}>
        <ThemedText style={[styles.legendText, { color: c.textMuted }]}>
          {t('fixture.standings.legend')}
        </ThemedText>
      </View>
    </View>
  );
}

function Cell({
  value,
  c,
  wide,
}: {
  value: number | string;
  c: ReturnType<typeof useTheme>;
  wide?: boolean;
}) {
  return (
    <ThemedText
      style={[
        wide ? styles.cellNumWide : styles.cellNum,
        styles.cellText,
        styles.numberValue,
        { color: c.textMuted },
      ]}>
      {value}
    </ThemedText>
  );
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    overflow: 'hidden',
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  headerCell: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.4,
    textAlign: 'center',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  cellPos: {
    width: 26,
    textAlign: 'center',
  },
  cellTeam: {
    flex: 1,
    paddingHorizontal: 4,
  },
  cellNum: {
    width: 26,
    textAlign: 'center',
  },
  cellNumWide: {
    width: 36,
    textAlign: 'center',
  },
  cellText: {
    fontSize: 12,
  },
  numberValue: {
    fontVariant: ['tabular-nums'],
  },
  teamCell: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  logo: {
    width: 18,
    height: 18,
  },
  logoPlaceholder: {
    width: 18,
    height: 18,
    borderRadius: 3,
  },
  teamName: {
    fontSize: 12,
    flexShrink: 1,
  },
  legend: {
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  legendText: {
    fontSize: 10,
    letterSpacing: 0.3,
  },
});
