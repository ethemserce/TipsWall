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
  if (stats.length === 0) return <TabEmpty icon="chart-bar" message={t('fixture.stats.notAvailable')} />;

  // Pull ball-possession out of the list and surface it as a hero stat
  // up top. It's the single most-read metric in a match and the regular
  // value-and-bar row buries it among 15+ other rows. Hero treatment:
  // big percentages, a thick split bar with brand vs muted accents.
  const possessionStat = stats.find(
    (s) => (s.type_code ?? '').toUpperCase() === 'BALL_POSSESSION',
  );
  const otherStats = stats.filter(
    (s) => (s.type_code ?? '').toUpperCase() !== 'BALL_POSSESSION',
  );

  return (
    <>
      {possessionStat ? <PossessionHero stat={possessionStat} /> : null}
      <View
        style={[
          styles.card,
          { backgroundColor: c.surface, borderColor: c.border },
        ]}>
        {otherStats.map((s) => (
          <StatRow key={s.type_id} stat={s} />
        ))}
      </View>
    </>
  );
}

function PossessionHero({ stat }: { stat: FixtureStatistic }) {
  const c = useTheme();
  const { t, i18n } = useTranslation();
  const home = stat.home_value ?? 0;
  const away = stat.away_value ?? 0;
  const total = home + away;
  // SportMonks usually ships percentages already summing to ~100 — but
  // some feeds emit fractions (0.56/0.44) or raw counts. Normalise to
  // a 0-100 share each side so the bar always reads correctly.
  const homePct = total > 0 ? Math.round((home / total) * 100) : 50;
  const awayPct = 100 - homePct;
  // %56 in TR, 56% in EN — keeps the typographic convention right.
  const isTr = i18n.language?.toLowerCase().startsWith('tr');
  const fmt = (p: number) => (isTr ? `%${p}` : `${p}%`);

  return (
    <View
      style={[
        styles.possessionCard,
        {
          backgroundColor: c.surfaceElevated,
          borderColor: c.brand,
        },
      ]}>
      <ThemedText style={[styles.possessionLabel, { color: c.brand }]}>
        {t('fixture.stats.possessionTitle', { defaultValue: isTr ? 'TOPLA OYNAMA' : 'BALL POSSESSION' })}
      </ThemedText>
      <View style={styles.possessionValues}>
        <ThemedText style={[styles.possessionPct, { color: c.text }]}>
          {fmt(homePct)}
        </ThemedText>
        <ThemedText style={[styles.possessionPct, { color: c.text }]}>
          {fmt(awayPct)}
        </ThemedText>
      </View>
      <View style={[styles.possessionBar, { backgroundColor: c.border }]}>
        <View
          style={[
            styles.possessionFillHome,
            {
              backgroundColor: c.brand,
              width: `${homePct}%`,
            },
          ]}
        />
        <View
          style={[
            styles.possessionFillAway,
            {
              backgroundColor: c.brandSoft,
              width: `${awayPct}%`,
            },
          ]}
        />
      </View>
    </View>
  );
}

function StatRow({ stat }: { stat: FixtureStatistic }) {
  const c = useTheme();
  const { t } = useTranslation();
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
          {humanizeLabel(stat, t)}
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

// SportMonks type_code → user-facing translation. Falls back to the
// title-cased English type_code (or the upstream type_name) so unmapped
// stats still render legibly. The i18n key is "fixture.stats.types.<code>".
function humanizeLabel(
  stat: FixtureStatistic,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  const code = (stat.type_code ?? '').toUpperCase();
  if (code) {
    const translated = t(`fixture.stats.types.${code}`, {
      defaultValue: '',
    });
    if (translated) return translated;
  }
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
  possessionCard: {
    marginHorizontal: 16,
    marginTop: 16,
    paddingHorizontal: 18,
    paddingTop: 14,
    paddingBottom: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    gap: 8,
  },
  possessionLabel: {
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 0.6,
    textAlign: 'center',
  },
  possessionValues: {
    flexDirection: 'row',
    alignItems: 'baseline',
    justifyContent: 'space-between',
    paddingHorizontal: 4,
  },
  possessionPct: {
    fontSize: 28,
    fontWeight: '900',
    fontVariant: ['tabular-nums'],
    letterSpacing: -0.5,
  },
  possessionBar: {
    height: 10,
    borderRadius: 5,
    flexDirection: 'row',
    overflow: 'hidden',
  },
  possessionFillHome: {
    height: 10,
  },
  possessionFillAway: {
    height: 10,
  },
});
