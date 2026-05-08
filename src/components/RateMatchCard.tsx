import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { useRouter } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { CircularGauge } from '@/src/components/CircularGauge';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureDetail } from '@/src/types/fixtureDetail';
import type { Market } from '@/src/types/market';
import type { RateResult } from '@/src/types/rateResult';

interface RateMatchCardProps {
  fixtureId: number;
  fixture: FixtureDetail | undefined;
  signals: RateResult[];
  marketLookup: Map<number, Market>;
  primaryMetric: 'winning_percent' | 'earning_percent';
  gaugeColor: string;
}

const VBET_COLOR = '#f59e0b';
const DSO_COLOR = '#22c55e';
const IKO_COLOR = '#3b82f6';
const STAR_COLOR = '#f59e0b';

export function RateMatchCard({
  fixtureId,
  fixture,
  signals,
  marketLookup,
  primaryMetric,
}: RateMatchCardProps) {
  const c = useTheme();
  const router = useRouter();
  const { t } = useTranslation();

  const homeName = fixture?.fixture.home_team_name ?? null;
  const awayName = fixture?.fixture.away_team_name ?? null;
  const homeImg = fixture?.fixture.home_team_image_path ?? null;
  const awayImg = fixture?.fixture.away_team_image_path ?? null;
  const startingAt = fixture?.fixture.starting_at ?? null;
  const dateLine = startingAt ? format(parseISO(startingAt), 'dd.MM.yyyy') : null;
  const timeLine = startingAt ? format(parseISO(startingAt), 'HH:mm') : null;

  const stars = computeStars(signals, primaryMetric);

  // Compute per-signal İKO across the same market (no-vig probability).
  const ikoByMarket = computeIkoByMarket(signals);

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <Pressable
        onPress={() => router.push(`/fixture/${fixtureId}` as never)}
        style={({ pressed }) => [styles.topBar, pressed && { opacity: 0.7 }]}>
        <View style={styles.idBadge}>
          <View style={[styles.idAccent, { backgroundColor: c.brand }]} />
          <ThemedText style={[styles.idText, { color: c.text }]}>
            {fixtureId}
          </ThemedText>
        </View>

        <View style={styles.starsRow}>
          {Array.from({ length: 3 }).map((_, i) => (
            <ThemedText
              key={i}
              style={[
                styles.star,
                { color: i < stars ? STAR_COLOR : c.border },
              ]}>
              ★
            </ThemedText>
          ))}
        </View>

        <View style={[styles.detailBtn, { borderColor: c.border, backgroundColor: c.bg }]}>
          <ThemedText style={[styles.detailBtnText, { color: c.text }]}>
            {t('common.match').toUpperCase()}
          </ThemedText>
        </View>
      </Pressable>

      <View style={[styles.divider, { backgroundColor: c.border }]} />

      <View style={styles.matchInfo}>
        {dateLine ? (
          <ThemedText style={[styles.date, { color: c.text }]}>{dateLine}</ThemedText>
        ) : null}
        {timeLine ? (
          <ThemedText style={[styles.time, { color: c.textMuted }]}>
            {timeLine}
          </ThemedText>
        ) : null}

        <View style={styles.teamsRow}>
          <TeamColumn name={homeName} imagePath={homeImg} />
          <ThemedText style={[styles.vs, { color: c.text }]}>VS</ThemedText>
          <TeamColumn name={awayName} imagePath={awayImg} />
        </View>
      </View>

      <View style={[styles.divider, { backgroundColor: c.border }]} />

      <View style={styles.headerRow}>
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

      {signals.map((s) => {
        const market = marketLookup.get(s.market_id);
        const iko = ikoByMarket.get(s.market_id)?.get(s.id) ?? null;
        const sample = s.win_count + s.lost_count;
        const hasSample = sample > 0;
        return (
          <View
            key={s.id}
            style={[styles.signalRow, { borderTopColor: c.border }]}>
            <View style={styles.cellLabel}>
              <ThemedText
                style={[styles.label, { color: c.text }]}
                numberOfLines={1}>
                {formatLabel(s, market)}
              </ThemedText>
            </View>
            <ThemedText
              style={[styles.cell, styles.cellNumber, styles.numberValue, { color: c.textMuted }]}>
              {s.odd_value != null ? s.odd_value.toFixed(2) : '-'}
            </ThemedText>
            <View style={styles.cellGauge}>
              <CircularGauge
                value={hasSample ? s.earning_percent : null}
                color={VBET_COLOR}
              />
            </View>
            <View style={styles.cellGauge}>
              <CircularGauge
                value={hasSample ? s.winning_percent : null}
                color={DSO_COLOR}
              />
            </View>
            <View style={styles.cellGauge}>
              <CircularGauge value={iko} color={IKO_COLOR} />
            </View>
            <ThemedText
              style={[styles.cell, styles.cellNarrow, styles.numberValue, { color: c.textMuted }]}>
              {hasSample ? s.win_count : '-'}
            </ThemedText>
            <ThemedText
              style={[styles.cell, styles.cellNarrow, styles.numberValue, { color: c.textMuted }]}>
              {hasSample ? s.lost_count : '-'}
            </ThemedText>
          </View>
        );
      })}
    </View>
  );
}

function TeamColumn({
  name,
  imagePath,
}: {
  name: string | null;
  imagePath: string | null;
}) {
  const c = useTheme();
  return (
    <View style={styles.teamColumn}>
      {imagePath ? (
        <Image
          source={{ uri: imagePath }}
          style={styles.teamLogo}
          contentFit="contain"
          transition={150}
        />
      ) : (
        <View style={[styles.teamLogoPlaceholder, { backgroundColor: c.border }]} />
      )}
      <ThemedText
        style={[styles.teamName, { color: c.text }]}
        numberOfLines={2}>
        {(name ?? 'TBD').toUpperCase()}
      </ThemedText>
    </View>
  );
}

function formatLabel(s: RateResult, market: Market | undefined): string {
  // Prefer outcome-style label when total/handicap is set, else fall back
  // to plain label (matches the legacy 'MS 2,5 ÜST' style).
  if (s.total != null) return `${s.label} ${s.total}`.trim();
  if (s.handicap != null) return `${s.label} ${s.handicap}`.trim();
  return s.label || market?.name || `Market #${s.market_id}`;
}

function computeStars(
  signals: RateResult[],
  primaryMetric: 'winning_percent' | 'earning_percent',
): number {
  if (signals.length === 0) return 0;
  const best = signals.reduce<number>((acc, s) => {
    const v = s[primaryMetric];
    return v != null && v > acc ? v : acc;
  }, -Infinity);
  if (best === -Infinity) return 0;

  if (primaryMetric === 'winning_percent') {
    if (best >= 70) return 3;
    if (best >= 60) return 2;
    if (best >= 50) return 1;
    return 0;
  }
  // earning_percent (ROI)
  if (best >= 30) return 3;
  if (best >= 15) return 2;
  if (best >= 5) return 1;
  return 0;
}

function computeIkoByMarket(signals: RateResult[]) {
  // Group signals by market and compute no-vig implied probability per row.
  const byMarket = new Map<number, RateResult[]>();
  for (const s of signals) {
    const list = byMarket.get(s.market_id);
    if (list) list.push(s);
    else byMarket.set(s.market_id, [s]);
  }
  const out = new Map<number, Map<string, number>>();
  for (const [marketId, list] of byMarket.entries()) {
    const totalImplied = list.reduce(
      (acc, s) => (s.odd_value && s.odd_value > 0 ? acc + 1 / s.odd_value : acc),
      0,
    );
    const inner = new Map<string, number>();
    for (const s of list) {
      if (s.odd_value && s.odd_value > 0 && totalImplied > 0) {
        inner.set(s.id, (1 / s.odd_value / totalImplied) * 100);
      }
    }
    out.set(marketId, inner);
  }
  return out;
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 12,
    marginTop: 12,
    borderRadius: 12,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
  },
  topBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  idBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    minWidth: 80,
  },
  idAccent: {
    width: 4,
    height: 22,
    borderRadius: 2,
  },
  idText: {
    fontSize: 18,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  starsRow: {
    flexDirection: 'row',
    gap: 4,
  },
  star: {
    fontSize: 14,
  },
  detailBtn: {
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 6,
    borderWidth: StyleSheet.hairlineWidth,
    minWidth: 80,
    alignItems: 'center',
  },
  detailBtnText: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.6,
  },
  divider: {
    height: StyleSheet.hairlineWidth,
  },
  matchInfo: {
    paddingVertical: 14,
    paddingHorizontal: 16,
    alignItems: 'center',
  },
  date: {
    fontSize: 13,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
  },
  time: {
    fontSize: 12,
    marginTop: 2,
    fontVariant: ['tabular-nums'],
  },
  teamsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    width: '100%',
    marginTop: 12,
  },
  teamColumn: {
    flex: 1,
    alignItems: 'center',
    gap: 8,
  },
  teamLogo: {
    width: 56,
    height: 56,
  },
  teamLogoPlaceholder: {
    width: 56,
    height: 56,
    borderRadius: 8,
  },
  teamName: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.4,
    textAlign: 'center',
  },
  vs: {
    fontSize: 14,
    fontWeight: '700',
    letterSpacing: 1,
    paddingHorizontal: 8,
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 8,
  },
  headerCell: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.4,
    textAlign: 'center',
  },
  signalRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 2,
  },
  cell: {
    fontSize: 12,
  },
  cellLabel: {
    flex: 1.3,
    paddingLeft: 6,
  },
  label: {
    fontSize: 12,
    fontWeight: '600',
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
