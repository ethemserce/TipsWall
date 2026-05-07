import { useMemo } from 'react';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
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
        <ThemedText style={[styles.headerCell, styles.cellNumber, { color: c.textMuted }]}>
          VBET
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNumber, { color: c.textMuted }]}>
          DSO
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNumber, { color: c.textMuted }]}>
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
        const vbetColor = colorForSigned(outcome.earning_percent, c);
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
            <ThemedText
              style={[styles.cell, styles.cellNumber, styles.numberValue, { color: vbetColor }]}>
              {hasSample ? formatSigned(outcome.earning_percent) : '-'}
            </ThemedText>
            <ThemedText
              style={[styles.cell, styles.cellNumber, styles.numberValue, { color: c.text }]}>
              {hasSample ? formatPercent(outcome.winning_percent) : '-'}
            </ThemedText>
            <ThemedText
              style={[styles.cell, styles.cellNumber, styles.numberValue, { color: c.text }]}>
              {iko != null ? formatPercent(iko) : '-'}
            </ThemedText>
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

function colorForSigned(
  value: number | null | undefined,
  c: ReturnType<typeof useTheme>,
): string {
  if (value == null) return c.textMuted;
  if (value > 0) return c.brand;
  if (value < 0) return c.live;
  return c.text;
}

function formatLabel(o: FixtureOddOutcome): string {
  const suffix = o.total ?? o.handicap ?? null;
  return suffix ? `${o.label} ${suffix}` : o.label;
}

function formatOdd(value: number | null | undefined): string {
  if (value == null) return '-';
  return value.toFixed(2);
}

function formatPercent(value: number | null | undefined): string {
  if (value == null) return '-';
  return `${value.toFixed(1)}`;
}

function formatSigned(value: number | null | undefined): string {
  if (value == null) return '-';
  const sign = value > 0 ? '+' : '';
  return `${sign}${value.toFixed(1)}`;
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
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  headerCell: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  cell: {
    fontSize: 13,
  },
  cellLabel: {
    flex: 1.6,
    fontWeight: '500',
  },
  cellNumber: {
    flex: 1,
    textAlign: 'right',
  },
  cellNarrow: {
    flex: 0.6,
    textAlign: 'right',
  },
  numberValue: {
    fontVariant: ['tabular-nums'],
    fontWeight: '600',
  },
});
