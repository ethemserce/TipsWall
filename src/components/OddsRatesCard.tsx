import { useMemo } from 'react';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { CircularGauge } from '@/src/components/CircularGauge';
import { useTheme } from '@/src/lib/useTheme';
import type {
  FixtureOddOutcome,
  FixtureOddsMarket,
} from '@/src/types/fixtureOdds';

interface OddsRatesCardProps {
  market: FixtureOddsMarket;
}

interface DerivedRow {
  outcome: FixtureOddOutcome;
  iko: number | null;
}

const VBET_COLOR = '#f59e0b'; // amber
const DSO_COLOR = '#22c55e'; // green
const IKO_COLOR = '#3b82f6'; // blue

export function OddsRatesCard({ market }: OddsRatesCardProps) {
  const c = useTheme();

  const rows = useMemo<DerivedRow[]>(() => {
    const totalImplied = market.outcomes.reduce((acc, o) => {
      if (o.value && o.value > 0) return acc + 1 / o.value;
      return acc;
    }, 0);
    return market.outcomes.map((o) => ({
      outcome: o,
      iko:
        o.value && o.value > 0 && totalImplied > 0
          ? (1 / o.value / totalImplied) * 100
          : null,
    }));
  }, [market.outcomes]);

  if (rows.length === 0) return null;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <ThemedText style={[styles.title, { color: c.textMuted }]}>
        {(market.market_name ?? `MARKET #${market.market_id}`).toUpperCase()}
      </ThemedText>

      <View style={[styles.headerRow, { borderTopColor: c.border }]}>
        <ThemedText style={[styles.headerCell, styles.cellLabel, { color: c.textMuted }]}>
          TİP
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNumber, { color: c.textMuted }]}>
          ORAN
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellGauge, { color: c.textMuted }]}>
          VBET
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellGauge, { color: c.textMuted }]}>
          DSO
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellGauge, { color: c.textMuted }]}>
          İKO
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNarrow, { color: c.textMuted }]}>
          KZ
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNarrow, { color: c.textMuted }]}>
          KY
        </ThemedText>
      </View>

      {rows.map(({ outcome, iko }) => {
        const sample = outcome.win_count + outcome.lost_count;
        const hasSample = sample > 0;
        return (
          <View
            key={`${outcome.label}-${outcome.total ?? ''}-${outcome.handicap ?? ''}-${outcome.value ?? ''}`}
            style={[styles.row, { borderTopColor: c.border }]}>
            <ThemedText
              style={[styles.cell, styles.cellLabel, { color: c.text }]}
              numberOfLines={1}>
              {formatLabel(outcome)}
            </ThemedText>
            <ThemedText
              style={[styles.cell, styles.cellNumber, styles.numberValue, { color: c.text }]}>
              {formatOdd(outcome.value)}
            </ThemedText>
            <View style={styles.cellGauge}>
              <CircularGauge
                value={hasSample ? outcome.earning_percent : null}
                color={VBET_COLOR}
              />
            </View>
            <View style={styles.cellGauge}>
              <CircularGauge
                value={hasSample ? outcome.winning_percent : null}
                color={DSO_COLOR}
              />
            </View>
            <View style={styles.cellGauge}>
              <CircularGauge value={iko} color={IKO_COLOR} />
            </View>
            <ThemedText
              style={[styles.cell, styles.cellNarrow, styles.numberValue, { color: c.textMuted }]}>
              {hasSample ? outcome.win_count : '-'}
            </ThemedText>
            <ThemedText
              style={[styles.cell, styles.cellNarrow, styles.numberValue, { color: c.textMuted }]}>
              {hasSample ? outcome.lost_count : '-'}
            </ThemedText>
          </View>
        );
      })}
    </View>
  );
}

function formatLabel(o: FixtureOddOutcome): string {
  const suffix = o.total ?? o.handicap ?? null;
  return suffix ? `${o.label} ${suffix}` : o.label;
}

function formatOdd(value: number | null | undefined): string {
  if (value == null) return '-';
  return value.toFixed(2);
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
    paddingBottom: 8,
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 6,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  headerCell: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.4,
    textAlign: 'center',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 8,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 2,
  },
  cell: {
    fontSize: 12,
  },
  cellLabel: {
    flex: 1.2,
    fontWeight: '500',
    paddingLeft: 6,
  },
  cellNumber: {
    flex: 0.7,
    textAlign: 'center',
  },
  cellGauge: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  cellNarrow: {
    flex: 0.5,
    textAlign: 'center',
  },
  numberValue: {
    fontVariant: ['tabular-nums'],
    fontWeight: '600',
  },
});
