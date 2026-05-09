import { useTranslation } from 'react-i18next';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import {
  formatOddValue,
  useOddsHidden,
} from '@/src/lib/settings/settingsStore';
import { useTheme } from '@/src/lib/useTheme';
import type { RateSummary } from '@/src/types/rateResult';

interface RateSummaryCardProps {
  summary: RateSummary;
  asOfDate: string | null;
}

export function RateSummaryCard({ summary, asOfDate }: RateSummaryCardProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const oddsHidden = useOddsHidden();
  const winRate =
    summary.bet_total > 0
      ? (summary.success_count / summary.bet_total) * 100
      : null;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <View style={styles.row}>
        <Stat
          label={t('rate.summary.signals').toUpperCase()}
          value={String(summary.total_signals)}
          color={c.text}
        />
        <Stat
          label={t('rate.summary.avgOdd').toUpperCase()}
          value={formatOddValue(summary.avg_odd_value, oddsHidden)}
          color={c.text}
        />
        <Stat
          label={t('rate.summary.avgWin').toUpperCase()}
          value={
            summary.avg_winning_percent != null
              ? `${summary.avg_winning_percent.toFixed(1)}%`
              : '-'
          }
          color="#22c55e"
        />
        <Stat
          label={t('rate.summary.avgRoi').toUpperCase()}
          value={
            summary.avg_earning_percent != null
              ? `${summary.avg_earning_percent.toFixed(1)}%`
              : '-'
          }
          color="#f59e0b"
        />
      </View>

      {summary.bet_total > 0 ? (
        <View style={[styles.verifyRow, { borderTopColor: c.border }]}>
          <Stat
            label={t('rate.summary.verification').toUpperCase()}
            value={`${summary.success_count} / ${summary.bet_total}`}
            color={c.text}
          />
          <Stat
            label={t('rate.summary.actualWin').toUpperCase()}
            value={winRate != null ? `${winRate.toFixed(1)}%` : '-'}
            color={c.brand}
          />
          <Stat
            label={t('rate.summary.earnings').toUpperCase()}
            value={
              summary.earning_total != null
                ? summary.earning_total.toFixed(2)
                : '-'
            }
            color={c.text}
          />
        </View>
      ) : null}

      {asOfDate ? (
        <ThemedText style={[styles.asOf, { color: c.textMuted }]}>
          {t('rate.summary.lastRefresh', { date: asOfDate })}
        </ThemedText>
      ) : null}
    </View>
  );
}

function Stat({
  label,
  value,
  color,
}: {
  label: string;
  value: string;
  color: string;
}) {
  const c = useTheme();
  return (
    <View style={styles.stat}>
      <ThemedText style={[styles.statLabel, { color: c.textMuted }]}>{label}</ThemedText>
      <ThemedText style={[styles.statValue, { color }]}>{value}</ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 12,
    marginTop: 12,
    paddingVertical: 12,
    paddingHorizontal: 4,
    borderRadius: 12,
    borderWidth: StyleSheet.hairlineWidth,
  },
  row: {
    flexDirection: 'row',
  },
  verifyRow: {
    flexDirection: 'row',
    marginTop: 10,
    paddingTop: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  stat: {
    flex: 1,
    alignItems: 'center',
    gap: 2,
  },
  statLabel: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.4,
    textAlign: 'center',
  },
  statValue: {
    fontSize: 16,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  asOf: {
    marginTop: 10,
    paddingHorizontal: 12,
    fontSize: 10,
    textAlign: 'center',
  },
});
