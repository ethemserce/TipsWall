import { Fragment, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { LayoutChangeEvent, StyleSheet, View } from 'react-native';
import Svg, { Line, Rect } from 'react-native-svg';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type {
  FixtureExpectedGoals,
  FixtureTrend,
} from '@/src/types/fixtureDetailExtras';

interface AttackMomentumCardProps {
  trends: FixtureTrend[] | undefined;
  expectedGoals?: FixtureExpectedGoals | null;
  homeName?: string | null;
  awayName?: string | null;
}

// SportMonks emits cumulative per-minute counts per side. To plot a
// Sofascore-style attack-momentum chart we diff successive entries to
// get "events in this minute" and render home above the zero line,
// away below. dangerous-attacks is the preferred signal; attacks is
// the fallback when the trial bundle hasn't landed for a fixture yet.
const PREFERRED_CHART_CODES = ['dangerous-attacks', 'attacks'] as const;
const CHART_HEIGHT = 72;
const CHART_VPAD = 4;
const HALF_HEIGHT = (CHART_HEIGHT - CHART_VPAD * 2) / 2;
const MIN_MINUTES = 90;

interface MinuteCell {
  minute: number;
  homeDelta: number;
  awayDelta: number;
}

function pickChartTrend(trends: FixtureTrend[]): FixtureTrend | null {
  for (const code of PREFERRED_CHART_CODES) {
    const hit = trends.find((tr) => tr.type_code === code);
    if (hit && hit.points.length > 0) return hit;
  }
  return null;
}

function lastValueBySide(
  trends: FixtureTrend[],
  code: string,
): { home: number; away: number } | null {
  const trend = trends.find((tr) => tr.type_code === code);
  if (!trend) return null;
  let home = 0;
  let away = 0;
  let seen = false;
  for (const p of trend.points) {
    const v = p.value ?? 0;
    if (p.side === 'home') {
      home = Math.max(home, v);
      seen = true;
    } else if (p.side === 'away') {
      away = Math.max(away, v);
      seen = true;
    }
  }
  return seen ? { home, away } : null;
}

function buildMinuteSeries(trend: FixtureTrend): {
  cells: MinuteCell[];
  maxDelta: number;
} {
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
  expectedGoals,
  homeName,
  awayName,
}: AttackMomentumCardProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const [width, setWidth] = useState(0);

  const chartTrend = useMemo(
    () => (trends && trends.length > 0 ? pickChartTrend(trends) : null),
    [trends],
  );

  const series = useMemo(
    () => (chartTrend ? buildMinuteSeries(chartTrend) : null),
    [chartTrend],
  );

  // Stats below the chart — derived from the same trend payload so we
  // don't pay for a second roundtrip. Each row reads the most recent
  // cumulative value per side; ball possession is rendered as a
  // proportional bar, the rest as a number | label | number triple.
  const stats = useMemo(() => {
    if (!trends || trends.length === 0) return null;
    const possession = lastValueBySide(trends, 'ball-possession');
    const shotsTotal = lastValueBySide(trends, 'shots-total');
    const shotsOnTarget = lastValueBySide(trends, 'shots-on-target');
    const dangerous = lastValueBySide(trends, 'dangerous-attacks');
    return { possession, shotsTotal, shotsOnTarget, dangerous };
  }, [trends]);

  const onLayout = (e: LayoutChangeEvent) => setWidth(e.nativeEvent.layout.width);

  // Hide the card entirely when there's no trend data at all, but keep
  // it visible when we have stats without a chart (rare but possible
  // when SportMonks ships counts without per-minute history).
  if (!series && !stats) return null;

  const homeColor = c.brand;
  const awayColor = c.live ?? '#d97070';

  return (
    <View
      style={[
        styles.card,
        c.shadowCard,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      {/* Team headers — colored dots tied to chart sides */}
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

      {/* Compact chart */}
      {series ? (
        <ChartBlock
          series={series}
          homeColor={homeColor}
          awayColor={awayColor}
          width={width}
          onLayout={onLayout}
          borderSoftColor={c.borderSoft}
          mutedColor={c.textMuted}
        />
      ) : null}

      {/* Divider between chart and stats */}
      {series && stats ? (
        <View style={[styles.divider, { backgroundColor: c.border }]} />
      ) : null}

      {/* Stats grid */}
      {stats ? (
        <View style={styles.statsBlock}>
          {stats.possession ? (
            <PossessionRow
              home={stats.possession.home}
              away={stats.possession.away}
              label={t('fixture.momentum.possession')}
              homeColor={homeColor}
              awayColor={awayColor}
              trackColor={c.borderSoft}
              textColor={c.text}
              labelColor={c.textMuted}
            />
          ) : null}
          {expectedGoals &&
          (expectedGoals.home != null || expectedGoals.away != null) ? (
            <StatRow
              home={Number(expectedGoals.home ?? 0)}
              away={Number(expectedGoals.away ?? 0)}
              label={t('fixture.momentum.expectedGoals')}
              textColor={c.text}
              labelColor={c.textMuted}
              format={(n) => n.toFixed(2)}
            />
          ) : null}
          {stats.shotsTotal ? (
            <StatRow
              home={stats.shotsTotal.home}
              away={stats.shotsTotal.away}
              label={t('fixture.momentum.shotsTotal')}
              textColor={c.text}
              labelColor={c.textMuted}
            />
          ) : null}
          {stats.shotsOnTarget ? (
            <StatRow
              home={stats.shotsOnTarget.home}
              away={stats.shotsOnTarget.away}
              label={t('fixture.momentum.shotsOnTarget')}
              textColor={c.text}
              labelColor={c.textMuted}
            />
          ) : null}
          {stats.dangerous ? (
            <StatRow
              home={stats.dangerous.home}
              away={stats.dangerous.away}
              label={t('fixture.momentum.dangerousAttacks')}
              textColor={c.text}
              labelColor={c.textMuted}
            />
          ) : null}
        </View>
      ) : null}
    </View>
  );
}

function ChartBlock({
  series,
  homeColor,
  awayColor,
  width,
  onLayout,
  borderSoftColor,
  mutedColor,
}: {
  series: { cells: MinuteCell[]; maxDelta: number };
  homeColor: string;
  awayColor: string;
  width: number;
  onLayout: (e: LayoutChangeEvent) => void;
  borderSoftColor: string;
  mutedColor: string;
}) {
  const totalMinutes = series.cells.length;
  const innerWidth = Math.max(0, width - 16);
  const slotWidth = innerWidth > 0 ? innerWidth / totalMinutes : 0;
  const barWidth = Math.max(1, slotWidth * 0.6);
  const zeroY = CHART_VPAD + HALF_HEIGHT;

  return (
    <View style={styles.chartWrap} onLayout={onLayout}>
      {innerWidth > 0 ? (
        <>
          <Svg width={innerWidth} height={CHART_HEIGHT}>
            <Line
              x1={0}
              y1={zeroY}
              x2={innerWidth}
              y2={zeroY}
              stroke={borderSoftColor}
              strokeWidth={1}
            />
            <Line
              x1={innerWidth / 2}
              y1={0}
              x2={innerWidth / 2}
              y2={CHART_HEIGHT}
              stroke={borderSoftColor}
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
          <View style={styles.axisRow}>
            <ThemedText style={[styles.axisLabel, { color: mutedColor }]}>
              0'
            </ThemedText>
            <ThemedText style={[styles.axisLabel, { color: mutedColor }]}>
              45'
            </ThemedText>
            <ThemedText style={[styles.axisLabel, { color: mutedColor }]}>
              {totalMinutes}'
            </ThemedText>
          </View>
        </>
      ) : null}
    </View>
  );
}

function PossessionRow({
  home,
  away,
  label,
  homeColor,
  awayColor,
  trackColor,
  textColor,
  labelColor,
}: {
  home: number;
  away: number;
  label: string;
  homeColor: string;
  awayColor: string;
  trackColor: string;
  textColor: string;
  labelColor: string;
}) {
  // Normalize: SportMonks ball-possession comes as cumulative numbers
  // that should sum to ~100 when both sides are tracked. Guard against
  // 0/0 (rare but happens early-match) by defaulting to 50/50.
  const total = home + away;
  const homePct = total > 0 ? Math.round((home / total) * 100) : 50;
  const awayPct = 100 - homePct;
  return (
    <View style={styles.statRow}>
      <ThemedText style={[styles.statValueLeft, { color: textColor }]}>
        %{homePct}
      </ThemedText>
      <View style={styles.possessionCenter}>
        <View style={[styles.possessionBar, { backgroundColor: trackColor }]}>
          <View
            style={[
              styles.possessionFillHome,
              { backgroundColor: homeColor, width: `${homePct}%` },
            ]}
          />
          <View
            style={[
              styles.possessionFillAway,
              { backgroundColor: awayColor, width: `${awayPct}%` },
            ]}
          />
        </View>
        <ThemedText
          style={[styles.statLabel, { color: labelColor }]}
          numberOfLines={1}>
          {label}
        </ThemedText>
      </View>
      <ThemedText style={[styles.statValueRight, { color: textColor }]}>
        %{awayPct}
      </ThemedText>
    </View>
  );
}

function StatRow({
  home,
  away,
  label,
  textColor,
  labelColor,
  format,
}: {
  home: number;
  away: number;
  label: string;
  textColor: string;
  labelColor: string;
  // Some metrics (xG) are fractional and shouldn't be rounded to int.
  // Pass `(n) => n.toFixed(2)` to keep two decimals; defaults to int.
  format?: (n: number) => string;
}) {
  const render = format ?? ((n: number) => `${Math.round(n)}`);
  return (
    <View style={styles.statRow}>
      <ThemedText style={[styles.statValueLeft, { color: textColor }]}>
        {render(home)}
      </ThemedText>
      <ThemedText
        style={[styles.statLabel, { color: labelColor }]}
        numberOfLines={1}>
        {label}
      </ThemedText>
      <ThemedText style={[styles.statValueRight, { color: textColor }]}>
        {render(away)}
      </ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 14,
    paddingHorizontal: 10,
    paddingTop: 10,
    paddingBottom: 10,
  },
  teamRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 4,
    marginBottom: 6,
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
    fontWeight: '700',
    maxWidth: '40%',
  },
  chartWrap: {
    paddingHorizontal: 8,
  },
  axisRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingHorizontal: 2,
    marginTop: 2,
  },
  axisLabel: {
    fontSize: 9,
    fontWeight: '600',
    letterSpacing: 0.3,
  },
  divider: {
    height: StyleSheet.hairlineWidth,
    marginVertical: 8,
    marginHorizontal: 4,
  },
  statsBlock: {
    gap: 8,
    paddingHorizontal: 4,
  },
  statRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
  },
  statValueLeft: {
    fontSize: 14,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
    minWidth: 36,
    textAlign: 'left',
  },
  statValueRight: {
    fontSize: 14,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
    minWidth: 36,
    textAlign: 'right',
  },
  statLabel: {
    flex: 1,
    fontSize: 11,
    fontWeight: '600',
    letterSpacing: 0.3,
    textAlign: 'center',
    textTransform: 'uppercase',
  },
  possessionCenter: {
    flex: 1,
    alignItems: 'center',
    gap: 4,
  },
  possessionBar: {
    flexDirection: 'row',
    width: '100%',
    height: 6,
    borderRadius: 3,
    overflow: 'hidden',
  },
  possessionFillHome: {
    height: '100%',
  },
  possessionFillAway: {
    height: '100%',
  },
});
