import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useTranslation } from 'react-i18next';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { TeamSeasonStats } from '@/src/types/team';

interface TeamStatsCardProps {
  stats: TeamSeasonStats | null | undefined;
}

/**
 * Headline numbers for the active league + season: O/G/B/M tile,
 * AG/YG tile, points/goal-difference, form pill (last 5 outcomes).
 * Rendered above any extra detail (e.g. table position) so the
 * "how is this team doing?" question lands in one glance.
 */
export function TeamStatsCard({ stats }: TeamStatsCardProps) {
  const c = useTheme();
  const { t } = useTranslation();

  if (!stats) return null;

  const played = stats.matches_played ?? 0;
  const won = stats.matches_won ?? 0;
  const drawn = stats.matches_drawn ?? 0;
  const lost = stats.matches_lost ?? 0;
  const goalsFor = stats.goals_for ?? 0;
  const goalsAgainst = stats.goals_against ?? 0;
  const goalDiff = stats.goal_difference ?? goalsFor - goalsAgainst;
  const points = stats.points;
  const avgFor = stats.average_goals_for;
  const avgAgainst = stats.average_goals_against;
  const cleanSheets = stats.clean_sheets;
  const bothScored = stats.both_teams_scored;

  return (
    <View style={[styles.card, { backgroundColor: c.surface, borderColor: c.border }]}>
      <View style={styles.header}>
        <ThemedText style={[styles.title, { color: c.textMuted }]}>
          {t('team.stats.title', { defaultValue: 'SEZON İSTATİSTİKLERİ' })}
        </ThemedText>
        {points != null ? (
          <View style={[styles.pointsPill, { backgroundColor: c.brandSoft }]}>
            <ThemedText style={[styles.pointsText, { color: c.brand }]}>
              {points} {t('team.stats.pointsShort', { defaultValue: 'P' })}
            </ThemedText>
          </View>
        ) : null}
      </View>

      <View style={styles.recordRow}>
        <RecordCell value={played} label={t('team.stats.played', { defaultValue: 'O' })} c={c} />
        <RecordCell value={won} label={t('team.stats.won', { defaultValue: 'G' })} c={c} color={c.success} />
        <RecordCell value={drawn} label={t('team.stats.drawn', { defaultValue: 'B' })} c={c} color={c.textMuted} />
        <RecordCell value={lost} label={t('team.stats.lost', { defaultValue: 'M' })} c={c} color={c.danger} />
      </View>

      <View style={[styles.divider, { backgroundColor: c.borderSoft }]} />

      <View style={styles.goalsRow}>
        <View style={styles.goalsBlock}>
          <ThemedText style={[styles.goalsLabel, { color: c.textMuted }]}>
            {t('team.stats.goalsFor', { defaultValue: 'Attılan' })}
          </ThemedText>
          <ThemedText style={[styles.goalsValue, { color: c.text }]}>
            {goalsFor}
          </ThemedText>
          {avgFor != null ? (
            <ThemedText style={[styles.goalsAvg, { color: c.textMuted }]}>
              {Number(avgFor).toFixed(2)} / m
            </ThemedText>
          ) : null}
        </View>
        <View style={[styles.goalDiffBlock, { backgroundColor: c.bg, borderColor: c.border }]}>
          <ThemedText style={[styles.goalDiffLabel, { color: c.textMuted }]}>
            {t('team.stats.goalDiff', { defaultValue: 'Av.' })}
          </ThemedText>
          <ThemedText
            style={[
              styles.goalDiffValue,
              {
                color: goalDiff > 0 ? c.success : goalDiff < 0 ? c.danger : c.text,
              },
            ]}>
            {goalDiff > 0 ? '+' : ''}{goalDiff}
          </ThemedText>
        </View>
        <View style={styles.goalsBlock}>
          <ThemedText style={[styles.goalsLabel, { color: c.textMuted }]}>
            {t('team.stats.goalsAgainst', { defaultValue: 'Yenilen' })}
          </ThemedText>
          <ThemedText style={[styles.goalsValue, { color: c.text }]}>
            {goalsAgainst}
          </ThemedText>
          {avgAgainst != null ? (
            <ThemedText style={[styles.goalsAvg, { color: c.textMuted }]}>
              {Number(avgAgainst).toFixed(2)} / m
            </ThemedText>
          ) : null}
        </View>
      </View>

      {cleanSheets != null || bothScored != null ? (
        <>
          <View style={[styles.divider, { backgroundColor: c.borderSoft }]} />
          <View style={styles.chipsRow}>
            {cleanSheets != null ? (
              <Chip
                icon="shield-check"
                label={t('team.stats.cleanSheets', { defaultValue: 'Gol yemediği' })}
                value={`${cleanSheets}`}
                c={c}
              />
            ) : null}
            {bothScored != null ? (
              <Chip
                icon="soccer"
                label={t('team.stats.btts', { defaultValue: 'KG Var' })}
                value={`${bothScored}`}
                c={c}
              />
            ) : null}
          </View>
        </>
      ) : null}

      {stats.form ? (
        <>
          <View style={[styles.divider, { backgroundColor: c.borderSoft }]} />
          <View style={styles.formRow}>
            <ThemedText style={[styles.formLabel, { color: c.textMuted }]}>
              {t('team.stats.form', { defaultValue: 'SON 5' })}
            </ThemedText>
            <View style={styles.formChips}>
              {parseForm(stats.form).map((r, i) => (
                <View
                  key={i}
                  style={[
                    styles.formChip,
                    {
                      backgroundColor:
                        r === 'W'
                          ? c.success
                          : r === 'L'
                            ? c.danger
                            : c.textMuted,
                    },
                  ]}>
                  <ThemedText style={[styles.formChipText, { color: c.textInverse }]}>
                    {formCharLabel(r)}
                  </ThemedText>
                </View>
              ))}
            </View>
          </View>
        </>
      ) : null}
    </View>
  );
}

function RecordCell({
  value,
  label,
  c,
  color,
}: {
  value: number;
  label: string;
  c: ReturnType<typeof useTheme>;
  color?: string;
}) {
  return (
    <View style={styles.recordCell}>
      <ThemedText style={[styles.recordValue, { color: color ?? c.text }]}>
        {value}
      </ThemedText>
      <ThemedText style={[styles.recordLabel, { color: c.textMuted }]}>
        {label}
      </ThemedText>
    </View>
  );
}

function Chip({
  icon,
  label,
  value,
  c,
}: {
  icon: keyof typeof MaterialCommunityIcons.glyphMap;
  label: string;
  value: string;
  c: ReturnType<typeof useTheme>;
}) {
  return (
    <View style={[styles.chip, { backgroundColor: c.bg, borderColor: c.border }]}>
      <MaterialCommunityIcons name={icon} size={14} color={c.brand} />
      <ThemedText style={[styles.chipLabel, { color: c.textMuted }]}>
        {label}
      </ThemedText>
      <ThemedText style={[styles.chipValue, { color: c.text }]}>
        {value}
      </ThemedText>
    </View>
  );
}

function parseForm(form: string): string[] {
  // SportMonks sometimes ships the form string with separators ("W-W-D-L-W"),
  // sometimes packed ("WWDLW"). Strip non-letters and take the last 5.
  const clean = form.replace(/[^A-Za-z]/g, '').toUpperCase();
  return clean.slice(-5).split('');
}

function formCharLabel(ch: string): string {
  // English W/D/L is already a single character; matches the chip width.
  if (ch === 'W') return 'G';
  if (ch === 'D') return 'B';
  if (ch === 'L') return 'M';
  return ch;
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    overflow: 'hidden',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 14,
    paddingTop: 12,
    paddingBottom: 8,
  },
  title: {
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
  pointsPill: {
    paddingHorizontal: 10,
    paddingVertical: 3,
    borderRadius: 999,
  },
  pointsText: {
    fontSize: 12,
    fontWeight: '900',
    letterSpacing: 0.5,
  },
  recordRow: {
    flexDirection: 'row',
    paddingHorizontal: 14,
    paddingBottom: 12,
  },
  recordCell: {
    flex: 1,
    alignItems: 'center',
    gap: 2,
  },
  recordValue: {
    fontSize: 22,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  recordLabel: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.6,
  },
  divider: {
    height: StyleSheet.hairlineWidth,
    marginHorizontal: 14,
  },
  goalsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 14,
    paddingVertical: 12,
    gap: 12,
  },
  goalsBlock: {
    flex: 1,
    alignItems: 'center',
    gap: 2,
  },
  goalsLabel: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
  goalsValue: {
    fontSize: 20,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  goalsAvg: {
    fontSize: 10,
    fontVariant: ['tabular-nums'],
  },
  goalDiffBlock: {
    minWidth: 70,
    paddingVertical: 8,
    paddingHorizontal: 10,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 10,
    alignItems: 'center',
    gap: 2,
  },
  goalDiffLabel: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.6,
  },
  goalDiffValue: {
    fontSize: 18,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  chipsRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
    paddingHorizontal: 14,
    paddingVertical: 10,
  },
  chip: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 10,
    paddingVertical: 5,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
  },
  chipLabel: {
    fontSize: 11,
  },
  chipValue: {
    fontSize: 12,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  formRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  formLabel: {
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
  formChips: {
    flexDirection: 'row',
    gap: 4,
  },
  formChip: {
    width: 22,
    height: 22,
    borderRadius: 11,
    alignItems: 'center',
    justifyContent: 'center',
  },
  formChipText: {
    fontSize: 10,
    fontWeight: '800',
  },
});
