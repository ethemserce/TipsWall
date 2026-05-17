import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import {
  computeHitStats,
  getOutcomeWinning,
} from '@/src/lib/signals/hitStats';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureOddsMarket } from '@/src/types/fixtureOdds';

interface FixtureHitStatsCardProps {
  markets: readonly FixtureOddsMarket[];
  /** Only renders when the host fixture is in the `finished` state bucket. */
  finished: boolean;
}

/**
 * Per-fixture retrospective accuracy: of all the odds outcomes shown
 * for this match, how many of the ones that already settled came true.
 * Renders nothing until the fixture is finished (state bucket = finished)
 * because mid-match `winning` flags can flap with the live score.
 */
export function FixtureHitStatsCard({ markets, finished }: FixtureHitStatsCardProps) {
  const c = useTheme();
  const { t } = useTranslation();

  const stats = useMemo(() => {
    const allOutcomes = markets.flatMap((m) => m.outcomes);
    return computeHitStats(allOutcomes, getOutcomeWinning);
  }, [markets]);

  if (!finished || stats.finished === 0) return null;

  const positive = stats.hitRate >= 50;
  const accentColor = positive ? c.success : c.danger;
  const accentSoft = positive
    ? (c.successSoft ?? c.surface)
    : (c.dangerSoft ?? c.surface);

  return (
    <View
      style={[
        styles.card,
        c.shadowCard,
        {
          backgroundColor: c.surfaceElevated,
          borderColor: c.borderSoft,
        },
      ]}
      accessibilityRole="summary">
      <View style={[styles.iconWrap, { backgroundColor: accentSoft }]}>
        <MaterialCommunityIcons
          name={positive ? 'check-circle' : 'close-circle'}
          size={20}
          color={accentColor}
        />
      </View>
      <View style={styles.meta}>
        <ThemedText style={[styles.title, { color: c.text }]}>
          {t('fixture.hitStats.title')}
        </ThemedText>
        <ThemedText style={[styles.subtitle, { color: c.textMuted }]}>
          {t('fixture.hitStats.subtitle', {
            won: stats.won,
            finished: stats.finished,
            rate: Math.round(stats.hitRate),
          })}
        </ThemedText>
      </View>
      <View style={[styles.rateChip, { borderColor: accentColor }]}>
        <ThemedText style={[styles.rateText, { color: accentColor }]}>
          %{Math.round(stats.hitRate)}
        </ThemedText>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    marginHorizontal: 16,
    marginTop: 12,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderRadius: 12,
    borderWidth: StyleSheet.hairlineWidth,
  },
  iconWrap: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  meta: {
    flex: 1,
    gap: 2,
  },
  title: {
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0.2,
  },
  subtitle: {
    fontSize: 11,
    fontWeight: '500',
  },
  rateChip: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 999,
    borderWidth: 1,
  },
  rateText: {
    fontSize: 13,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
});
