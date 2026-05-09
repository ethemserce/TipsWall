import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';
import Svg, { Circle, Line, Polyline } from 'react-native-svg';

import { ThemedText } from '@/components/themed-text';
import {
  computeCalibration,
  computeCalibrationTimeline,
  computeCouponStats,
  computeMarketBreakdown,
  type CalibrationPoint,
} from '@/src/lib/coupons/stats';
import type { Coupon } from '@/src/lib/coupons/types';
import { useTheme } from '@/src/lib/useTheme';


interface CouponStatsCardProps {
  coupons: Coupon[];
}

/**
 * Personal performance summary that lives at the top of CouponsScreen.
 * Collapsed by default to keep the list dominant; user expands to see
 * per-market breakdown.
 */
export function CouponStatsCard({ coupons }: CouponStatsCardProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const [expanded, setExpanded] = useState(false);
  const stats = useMemo(() => computeCouponStats(coupons), [coupons]);
  const breakdown = useMemo(() => computeMarketBreakdown(coupons), [coupons]);
  const calibration = useMemo(() => computeCalibration(coupons), [coupons]);
  const timeline = useMemo(
    () => computeCalibrationTimeline(coupons),
    [coupons],
  );

  // Render nothing until there's at least one settled coupon so we don't
  // show empty stats on first install.
  if (stats.settledCoupons === 0) return null;

  const hitRate = stats.couponHitRate;
  const avgOdd = stats.avgWinningOdd;

  return (
    <View
      style={[
        styles.card,
        c.shadowCard,
        { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
      ]}>
      <Pressable
        onPress={() => setExpanded((v) => !v)}
        accessibilityRole="button"
        accessibilityLabel={t('coupons.stats.a11yShow')}
        accessibilityState={{ expanded }}
        style={({ pressed }) => [
          styles.header,
          pressed && { backgroundColor: c.brandSoft },
        ]}>
        <View style={styles.headerLeft}>
          <View style={[styles.headerIcon, { backgroundColor: c.brandSoft }]}>
            <MaterialCommunityIcons
              name="chart-box"
              size={16}
              color={c.brand}
            />
          </View>
          <ThemedText style={[styles.title, { color: c.text }]}>
            {t('coupons.stats.title')}
          </ThemedText>
        </View>
        <MaterialCommunityIcons
          name={expanded ? 'chevron-up' : 'chevron-down'}
          size={20}
          color={c.textMuted}
        />
      </Pressable>

      <View style={[styles.summary, { borderTopColor: c.border }]}>
        <Stat
          value={String(stats.totalCoupons)}
          label={t('coupons.stats.kupon')}
          color={c.text}
        />
        <Stat
          value={`${stats.wonCoupons}/${stats.settledCoupons}`}
          label={t('coupons.stats.won')}
          color={c.text}
        />
        <Stat
          value={hitRate != null ? `%${hitRate.toFixed(0)}` : '–'}
          label={t('coupons.stats.hitRate')}
          color={hitRate != null && hitRate >= 50 ? c.success : c.text}
        />
        <Stat
          value={avgOdd != null ? avgOdd.toFixed(2) : '–'}
          label={t('coupons.stats.avgOdd')}
          color={c.text}
        />
      </View>

      {expanded ? (
        <>
          {calibration ? (
            <View style={[styles.breakdown, { borderTopColor: c.border }]}>
              <ThemedText style={[styles.sectionLabel, { color: c.textMuted }]}>
                {t('coupons.stats.calibration', { count: calibration.totalSelections })}
              </ThemedText>
              <CalibrationRow
                label={t('coupons.stats.calibrationSystem')}
                percent={calibration.systemAvgPercent}
                color={c.brand}
              />
              <CalibrationRow
                label={t('coupons.stats.calibrationUser')}
                percent={calibration.userActualPercent}
                color={
                  calibration.deltaPoints >= 0 ? c.success : c.danger
                }
              />
              <ThemedText
                style={[
                  styles.deltaText,
                  {
                    color:
                      calibration.deltaPoints >= 0 ? c.success : c.danger,
                  },
                ]}>
                {t('coupons.stats.deltaPoints', {
                  delta:
                    (calibration.deltaPoints >= 0 ? '+' : '') +
                    calibration.deltaPoints.toFixed(1),
                })}{' '}
                <ThemedText style={[styles.deltaSub, { color: c.textMuted }]}>
                  ·{' '}
                  {calibration.deltaPoints >= 0
                    ? t('coupons.stats.deltaAbove')
                    : t('coupons.stats.deltaBelow')}
                </ThemedText>
              </ThemedText>
              {timeline.length >= 2 ? (
                <CalibrationTimelineChart points={timeline} />
              ) : null}
            </View>
          ) : null}

          {breakdown.length > 0 ? (
            <View style={[styles.breakdown, { borderTopColor: c.border }]}>
              <ThemedText style={[styles.sectionLabel, { color: c.textMuted }]}>
                {t('coupons.stats.marketBreakdown')}
              </ThemedText>
              {breakdown.map((row) => (
                <View key={row.marketShort} style={styles.breakdownRow}>
                  <ThemedText style={[styles.market, { color: c.text }]}>
                    {row.marketShort}
                  </ThemedText>
                  <ThemedText style={[styles.fraction, { color: c.textMuted }]}>
                    {row.won}/{row.total}
                  </ThemedText>
                  <View style={[styles.barTrack, { backgroundColor: c.border }]}>
                    <View
                      style={[
                        styles.barFill,
                        {
                          width: `${Math.min(100, row.hitRate)}%`,
                          backgroundColor:
                            row.hitRate >= 50 ? c.success : c.brand,
                        },
                      ]}
                    />
                  </View>
                  <ThemedText style={[styles.percent, { color: c.text }]}>
                    %{row.hitRate.toFixed(0)}
                  </ThemedText>
                </View>
              ))}
            </View>
          ) : null}
        </>
      ) : null}
    </View>
  );
}

/**
 * Cumulative DSO (system) vs cumulative actual hit rate (user) over time.
 * Each X step is one settled selection in chronological order; each Y is
 * the running average up to that step. The dots track the actual outcome
 * (green = hit, red = miss) so streaks become visible against the smooth
 * cumulative line.
 */
function CalibrationTimelineChart({ points }: { points: CalibrationPoint[] }) {
  const c = useTheme();
  const { t } = useTranslation();
  const [width, setWidth] = useState(0);
  const HEIGHT = 92;
  const PAD_X = 4;
  const PAD_Y = 6;

  if (points.length < 2) return null;

  const innerW = Math.max(0, width - PAD_X * 2);
  const innerH = HEIGHT - PAD_Y * 2;
  const xAt = (i: number) =>
    PAD_X + (points.length === 1 ? 0 : (i / (points.length - 1)) * innerW);
  const yAt = (pct: number) =>
    PAD_Y + (1 - Math.max(0, Math.min(100, pct)) / 100) * innerH;

  const sysPoints = points
    .map((p, i) => `${xAt(i)},${yAt(p.systemCumPercent)}`)
    .join(' ');
  const userPoints = points
    .map((p, i) => `${xAt(i)},${yAt(p.userCumPercent)}`)
    .join(' ');

  return (
    <View
      onLayout={(e) => setWidth(e.nativeEvent.layout.width)}
      style={styles.chartWrap}>
      {width > 0 ? (
        <Svg width={width} height={HEIGHT}>
          {[25, 50, 75].map((g) => (
            <Line
              key={g}
              x1={PAD_X}
              x2={width - PAD_X}
              y1={yAt(g)}
              y2={yAt(g)}
              stroke={c.border}
              strokeWidth={0.5}
              strokeDasharray="2 3"
            />
          ))}
          <Polyline
            points={sysPoints}
            fill="none"
            stroke={c.brand}
            strokeWidth={1.5}
          />
          <Polyline
            points={userPoints}
            fill="none"
            stroke={c.success}
            strokeWidth={1.5}
          />
          {points.map((p, i) => (
            <Circle
              key={i}
              cx={xAt(i)}
              cy={yAt(p.userCumPercent)}
              r={2.2}
              fill={p.hit ? c.success : c.danger}
            />
          ))}
        </Svg>
      ) : null}
      <View style={styles.legendRow}>
        <View style={styles.legendItem}>
          <View style={[styles.legendDot, { backgroundColor: c.brand }]} />
          <ThemedText style={[styles.legendText, { color: c.textMuted }]}>
            {t('coupons.stats.legendSystem')}
          </ThemedText>
        </View>
        <View style={styles.legendItem}>
          <View style={[styles.legendDot, { backgroundColor: c.success }]} />
          <ThemedText style={[styles.legendText, { color: c.textMuted }]}>
            {t('coupons.stats.legendUser')}
          </ThemedText>
        </View>
        <ThemedText style={[styles.legendText, { color: c.textMuted, marginLeft: 'auto' }]}>
          {t('coupons.stats.legendCount', { count: points.length })}
        </ThemedText>
      </View>
    </View>
  );
}

function CalibrationRow({
  label,
  percent,
  color,
}: {
  label: string;
  percent: number;
  color: string;
}) {
  const c = useTheme();
  return (
    <View style={styles.calibrationRow}>
      <ThemedText style={[styles.calibrationLabel, { color: c.textMuted }]}>
        {label}
      </ThemedText>
      <View style={[styles.barTrack, { backgroundColor: c.border }]}>
        <View
          style={[
            styles.barFill,
            { width: `${Math.min(100, percent)}%`, backgroundColor: color },
          ]}
        />
      </View>
      <ThemedText style={[styles.calibrationPercent, { color }]}>
        %{percent.toFixed(0)}
      </ThemedText>
    </View>
  );
}

function Stat({
  value,
  label,
  color,
}: {
  value: string;
  label: string;
  color: string;
}) {
  const c = useTheme();
  return (
    <View style={styles.stat}>
      <ThemedText style={[styles.statValue, { color }]}>{value}</ThemedText>
      <ThemedText style={[styles.statLabel, { color: c.textMuted }]}>
        {label}
      </ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 12,
    marginBottom: 14,
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  headerLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  headerIcon: {
    width: 28,
    height: 28,
    borderRadius: 14,
    alignItems: 'center',
    justifyContent: 'center',
  },
  title: {
    fontSize: 14,
    fontWeight: '700',
    letterSpacing: 0.2,
  },
  summary: {
    flexDirection: 'row',
    paddingVertical: 12,
    paddingHorizontal: 8,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  stat: {
    flex: 1,
    alignItems: 'center',
    gap: 2,
  },
  statValue: {
    fontSize: 18,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  statLabel: {
    fontSize: 10,
    fontWeight: '600',
    letterSpacing: 0.3,
  },
  breakdown: {
    paddingHorizontal: 14,
    paddingTop: 8,
    paddingBottom: 12,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 4,
  },
  sectionLabel: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.6,
    paddingBottom: 4,
  },
  breakdownRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 4,
    gap: 8,
  },
  market: {
    width: 48,
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
  fraction: {
    width: 36,
    fontSize: 11,
    fontVariant: ['tabular-nums'],
    textAlign: 'right',
  },
  barTrack: {
    flex: 1,
    height: 5,
    borderRadius: 3,
    overflow: 'hidden',
  },
  barFill: {
    height: '100%',
    borderRadius: 3,
  },
  percent: {
    width: 36,
    fontSize: 11,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
    textAlign: 'right',
  },
  calibrationRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 4,
    gap: 8,
  },
  calibrationLabel: {
    width: 56,
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
  calibrationPercent: {
    width: 40,
    fontSize: 12,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
    textAlign: 'right',
  },
  deltaText: {
    fontSize: 11,
    fontWeight: '800',
    paddingTop: 6,
    paddingLeft: 56 + 8,
    fontVariant: ['tabular-nums'],
    letterSpacing: 0.3,
  },
  deltaSub: {
    fontSize: 10,
    fontWeight: '500',
  },
  chartWrap: {
    marginTop: 10,
  },
  legendRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingTop: 4,
  },
  legendItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  legendDot: {
    width: 7,
    height: 7,
    borderRadius: 4,
  },
  legendText: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
});
