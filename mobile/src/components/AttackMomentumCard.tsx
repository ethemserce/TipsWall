import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Fragment, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { LayoutChangeEvent, StyleSheet, View } from 'react-native';
import Svg, { Line, Rect } from 'react-native-svg';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureTrend } from '@/src/types/fixtureDetailExtras';

interface AttackMomentumCardProps {
  trends: FixtureTrend[] | undefined;
  homeName?: string | null;
  awayName?: string | null;
}

// Sofascore-style attack momentum. SportMonks trends arrive as cumulative
// counts per side per minute; we diff successive entries to get per-minute
// "attacks in this minute" and plot home above the zero line, away below.
// "dangerous-attacks" is the preferred signal because it ignores midfield
// possession noise; falls back to "attacks" when that's missing (often
// before the trial bundle landed, or for lower-tier matches).
const PREFERRED_CODES = ['dangerous-attacks', 'attacks'] as const;
const CHART_HEIGHT = 110;
const CHART_VPAD = 4;
const HALF_HEIGHT = (CHART_HEIGHT - CHART_VPAD * 2) / 2;
const MIN_MINUTES = 90;

function pickTrend(trends: FixtureTrend[]): FixtureTrend | null {
  for (const code of PREFERRED_CODES) {
    const hit = trends.find((tr) => tr.type_code === code);
    if (hit && hit.points.length > 0) return hit;
  }
  return null;
}

interface MinuteCell {
  minute: number;
  homeDelta: number;
  awayDelta: number;
}

function buildMinuteSeries(trend: FixtureTrend): {
  cells: MinuteCell[];
  maxDelta: number;
} {
  // First pass: bucket cumulative values per side per minute. SportMonks
  // emits one point each time the count changes; later points within the
  // same minute should win (the latest cumulative is the most accurate).
  const homeAt = new Map<number, number>();
  const awayAt = new Map<number, number>();
  let maxMinute = 0;
  for (const p of trend.points) {
    const m = p.minute ?? 0;
    const v = p.value ?? 0;
    if (p.side === 'home') homeAt.set(m, Math.max(homeAt.get(m) ?? 0, v));
    else if (p.side === 'away') awayAt.set(m, Math.max(awayAt.get(m) ?? 0, v));
    if (m > maxMinute) maxMinute = m;
  }
  const totalMinutes = Math.max(MIN_MINUTES, maxMinute);

  const cells: MinuteCell[] = [];
  let lastHome = 0;
  let lastAway = 0;
  let maxDelta = 0;
  for (let m = 1; m <= totalMinutes; m++) {
    const h = homeAt.get(m) ?? lastHome;
    const a = awayAt.get(m) ?? lastAway;
    const dh = Math.max(0, h - lastHome);
    const da = Math.max(0, a - lastAway);
    cells.push({ minute: m, homeDelta: dh, awayDelta: da });
    if (dh > maxDelta) maxDelta = dh;
    if (da > maxDelta) maxDelta = da;
    lastHome = h;
    lastAway = a;
  }
  return { cells, maxDelta: Math.max(1, maxDelta) };
}

export function AttackMomentumCard({
  trends,
  homeName,
  awayName,
}: AttackMomentumCardProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const [width, setWidth] = useState(0);

  const chosen = useMemo(
    () => (trends && trends.length > 0 ? pickTrend(trends) : null),
    [trends],
  );

  const series = useMemo(
    () => (chosen ? buildMinuteSeries(chosen) : null),
    [chosen],
  );

  const onLayout = (e: LayoutChangeEvent) => setWidth(e.nativeEvent.layout.width);

  if (!series || series.cells.length === 0) return null;

  const totalMinutes = series.cells.length;
  const homeColor = c.brand;
  const awayColor = c.live ?? '#d97070';
  const innerWidth = Math.max(0, width - 16); // 8px padding each side
  const slotWidth = innerWidth > 0 ? innerWidth / totalMinutes : 0;
  const barWidth = Math.max(1, slotWidth * 0.6);
  const zeroY = CHART_VPAD + HALF_HEIGHT;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <View style={styles.headerRow}>
        <MaterialCommunityIcons
          name="chart-bell-curve-cumulative"
          size={16}
          color={c.textMuted}
        />
        <ThemedText style={[styles.title, { color: c.textMuted }]}>
          {t('fixture.momentum.title').toUpperCase()}
        </ThemedText>
      </View>
      <View style={styles.teamRow}>
        <View style={[styles.dot, { backgroundColor: homeColor }]} />
        <ThemedText
          style={[styles.teamName, { color: c.text }]}
          numberOfLines={1}>
          {homeName ?? '—'}
        </ThemedText>
        <View style={styles.spacer} />
        <ThemedText
          style={[styles.teamName, { color: c.text, textAlign: 'right' }]}
          numberOfLines={1}>
          {awayName ?? '—'}
        </ThemedText>
        <View style={[styles.dot, { backgroundColor: awayColor }]} />
      </View>
      <View style={styles.chartWrap} onLayout={onLayout}>
        {innerWidth > 0 ? (
          <Svg width={innerWidth} height={CHART_HEIGHT}>
            {/* zero line + 45min divider */}
            <Line
              x1={0}
              y1={zeroY}
              x2={innerWidth}
              y2={zeroY}
              stroke={c.borderSoft}
              strokeWidth={1}
            />
            <Line
              x1={innerWidth / 2}
              y1={0}
              x2={innerWidth / 2}
              y2={CHART_HEIGHT}
              stroke={c.borderSoft}
              strokeWidth={StyleSheet.hairlineWidth}
              strokeDasharray="2 3"
            />
            {series.cells.map((cell, i) => {
              const x = i * slotWidth + (slotWidth - barWidth) / 2;
              const hHome = (cell.homeDelta / series.maxDelta) * HALF_HEIGHT;
              const hAway = (cell.awayDelta / series.maxDelta) * HALF_HEIGHT;
              return (
                <Fragment key={cell.minute}>
                  {hHome > 0 ? (
                    <Rect
                      x={x}
                      y={zeroY - hHome}
                      width={barWidth}
                      height={hHome}
                      fill={homeColor}
                      rx={barWidth / 4}
                    />
                  ) : null}
                  {hAway > 0 ? (
                    <Rect
                      x={x}
                      y={zeroY}
                      width={barWidth}
                      height={hAway}
                      fill={awayColor}
                      rx={barWidth / 4}
                    />
                  ) : null}
                </Fragment>
              );
            })}
          </Svg>
        ) : null}
      </View>
      <View style={styles.axisRow}>
        <ThemedText style={[styles.axisLabel, { color: c.textMuted }]}>0'</ThemedText>
        <ThemedText style={[styles.axisLabel, { color: c.textMuted }]}>
          45'
        </ThemedText>
        <ThemedText style={[styles.axisLabel, { color: c.textMuted }]}>
          {totalMinutes}'
        </ThemedText>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    paddingHorizontal: 8,
    paddingTop: 10,
    paddingBottom: 8,
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 6,
    marginBottom: 8,
  },
  title: {
    fontSize: 11,
    fontWeight: '600',
    letterSpacing: 0.6,
  },
  teamRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 6,
    marginBottom: 4,
  },
  spacer: {
    flex: 1,
  },
  dot: {
    width: 8,
    height: 8,
    borderRadius: 4,
  },
  teamName: {
    fontSize: 12,
    fontWeight: '600',
    maxWidth: '40%',
  },
  chartWrap: {
    paddingHorizontal: 8,
  },
  axisRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingHorizontal: 12,
    marginTop: 4,
  },
  axisLabel: {
    fontSize: 10,
    fontWeight: '500',
  },
});
