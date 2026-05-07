import { useTranslation } from 'react-i18next';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { TabEmpty, TabError, TabLoading } from '@/src/components/TabFeedback';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureStatistic } from '@/src/types/fixtureDetailExtras';

interface StatsTabProps {
  loading: boolean;
  error?: unknown;
  stats: FixtureStatistic[];
}

const PERCENT_TYPES = new Set(['BALL_POSSESSION', 'SUCCESSFUL_DRIBBLES_PERCENTAGE']);

export function StatsTab({ loading, error, stats }: StatsTabProps) {
  const c = useTheme();
  const { t } = useTranslation();

  if (error && stats.length === 0) return <TabError error={error} />;
  if (loading && stats.length === 0) return <TabLoading />;
  if (stats.length === 0) return <TabEmpty message={t('fixture.stats.notAvailable')} />;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <ThemedText style={[styles.title, { color: c.textMuted }]}>
        {t('fixture.stats.title').toUpperCase()}
      </ThemedText>
      {stats.map((s) => (
        <StatRow key={s.type_id} stat={s} />
      ))}
    </View>
  );
}

function StatRow({ stat }: { stat: FixtureStatistic }) {
  const c = useTheme();
  const isPercent = PERCENT_TYPES.has((stat.type_code ?? '').toUpperCase());
  const home = stat.home_value ?? 0;
  const away = stat.away_value ?? 0;
  const total = home + away;
  const homeRatio = total > 0 ? home / total : 0.5;
  const awayRatio = 1 - homeRatio;

  return (
    <View style={[styles.row, { borderTopColor: c.border }]}>
      <View style={styles.valueRow}>
        <ThemedText style={[styles.value, { color: c.text }]}>
          {formatValue(stat.home_value, isPercent)}
        </ThemedText>
        <ThemedText style={[styles.label, { color: c.textMuted }]}>
          {humanizeLabel(stat)}
        </ThemedText>
        <ThemedText style={[styles.value, { color: c.text }]}>
          {formatValue(stat.away_value, isPercent)}
        </ThemedText>
      </View>
      <View style={styles.barRow}>
        <View style={styles.barSide}>
          <View
            style={[
              styles.barFill,
              styles.barFillHome,
              {
                backgroundColor: c.brand,
                width: `${homeRatio * 100}%`,
              },
            ]}
          />
        </View>
        <View style={[styles.barDivider, { backgroundColor: c.border }]} />
        <View style={styles.barSide}>
          <View
            style={[
              styles.barFill,
              styles.barFillAway,
              {
                backgroundColor: c.brand,
                width: `${awayRatio * 100}%`,
              },
            ]}
          />
        </View>
      </View>
    </View>
  );
}

function formatValue(value: number | null, isPercent: boolean): string {
  if (value == null) return '-';
  if (isPercent) return `${Math.round(value)}%`;
  return Number.isInteger(value) ? String(value) : value.toFixed(1);
}

function humanizeLabel(stat: FixtureStatistic): string {
  if (stat.type_name) return stat.type_name;
  if (stat.type_code) {
    return stat.type_code
      .replace(/_/g, ' ')
      .toLowerCase()
      .replace(/\b\w/g, (m) => m.toUpperCase());
  }
  return `Stat #${stat.type_id}`;
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
  row: {
    paddingHorizontal: 14,
    paddingVertical: 12,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 6,
  },
  valueRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  value: {
    width: 48,
    fontSize: 14,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
    textAlign: 'center',
  },
  label: {
    flex: 1,
    fontSize: 12,
    textAlign: 'center',
  },
  barRow: {
    flexDirection: 'row',
    alignItems: 'center',
    height: 4,
  },
  barSide: {
    flex: 1,
    height: 4,
    overflow: 'hidden',
  },
  barFill: {
    height: 4,
    opacity: 0.7,
  },
  barFillHome: {
    alignSelf: 'flex-end',
  },
  barFillAway: {
    alignSelf: 'flex-start',
  },
  barDivider: {
    width: 1,
    height: 4,
  },
});
