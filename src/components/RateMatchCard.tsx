import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { useRouter } from 'expo-router';
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

export function RateMatchCard({
  fixtureId,
  fixture,
  signals,
  marketLookup,
  primaryMetric,
  gaugeColor,
}: RateMatchCardProps) {
  const c = useTheme();
  const router = useRouter();

  const homeName = fixture?.fixture.home_team_name ?? null;
  const awayName = fixture?.fixture.away_team_name ?? null;
  const homeImg = fixture?.fixture.home_team_image_path ?? null;
  const awayImg = fixture?.fixture.away_team_image_path ?? null;
  const startingAt = fixture?.fixture.starting_at ?? null;
  const startLine = startingAt
    ? format(parseISO(startingAt), 'd MMM • HH:mm')
    : null;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <Pressable
        onPress={() => router.push(`/fixture/${fixtureId}` as never)}
        style={({ pressed }) => [
          styles.header,
          { borderBottomColor: c.border, backgroundColor: pressed ? c.bg : 'transparent' },
        ]}>
        <View style={styles.teamRow}>
          <TeamSide name={homeName} imagePath={homeImg} alignRight />
          <ThemedText style={[styles.vs, { color: c.textMuted }]}>vs</ThemedText>
          <TeamSide name={awayName} imagePath={awayImg} />
        </View>
        {startLine ? (
          <ThemedText style={[styles.startLine, { color: c.textMuted }]}>
            {startLine}
          </ThemedText>
        ) : null}
      </Pressable>

      {signals.map((s) => (
        <SignalRow
          key={s.id}
          signal={s}
          market={marketLookup.get(s.market_id)}
          primaryMetric={primaryMetric}
          gaugeColor={gaugeColor}
        />
      ))}
    </View>
  );
}

function TeamSide({
  name,
  imagePath,
  alignRight,
}: {
  name: string | null;
  imagePath: string | null;
  alignRight?: boolean;
}) {
  const c = useTheme();
  const items = [
    imagePath ? (
      <Image
        key="logo"
        source={{ uri: imagePath }}
        style={styles.logo}
        contentFit="contain"
      />
    ) : (
      <View
        key="logo"
        style={[styles.logoPlaceholder, { backgroundColor: c.border }]}
      />
    ),
    <ThemedText
      key="name"
      style={[styles.team, { color: c.text }, alignRight && styles.teamRight]}
      numberOfLines={1}>
      {name ?? 'TBD'}
    </ThemedText>,
  ];
  return (
    <View style={[styles.teamSide, alignRight && styles.teamSideRight]}>
      {alignRight ? items.reverse() : items}
    </View>
  );
}

function SignalRow({
  signal,
  market,
  primaryMetric,
  gaugeColor,
}: {
  signal: RateResult;
  market: Market | undefined;
  primaryMetric: 'winning_percent' | 'earning_percent';
  gaugeColor: string;
}) {
  const c = useTheme();
  const marketName = market?.name ?? `Market #${signal.market_id}`;
  const outcome = formatOutcome(signal);
  const value = signal[primaryMetric];

  return (
    <View style={[styles.signalRow, { borderTopColor: c.border }]}>
      <View style={styles.signalText}>
        <ThemedText style={[styles.market, { color: c.text }]} numberOfLines={1}>
          {marketName}
        </ThemedText>
        <ThemedText style={[styles.outcome, { color: c.textMuted }]} numberOfLines={1}>
          {outcome}
        </ThemedText>
        <View style={styles.miniStats}>
          <Mini label="ORAN" value={signal.odd_value != null ? signal.odd_value.toFixed(2) : '-'} />
          <Mini label="KZ" value={String(signal.win_count)} />
          <Mini label="KY" value={String(signal.lost_count)} />
        </View>
      </View>
      <View style={styles.gaugeWrap}>
        <CircularGauge value={value} color={gaugeColor} size={48} strokeWidth={3.5} />
      </View>
    </View>
  );
}

function Mini({ label, value }: { label: string; value: string }) {
  const c = useTheme();
  return (
    <View style={styles.miniStat}>
      <ThemedText style={[styles.miniLabel, { color: c.textMuted }]}>{label}</ThemedText>
      <ThemedText style={[styles.miniValue, { color: c.text }]}>{value}</ThemedText>
    </View>
  );
}

function formatOutcome(s: RateResult): string {
  const parts = [s.label];
  if (s.total) parts.push(s.total);
  if (s.handicap) parts.push(`(${s.handicap})`);
  return parts.join(' ');
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 12,
    marginTop: 8,
    borderRadius: 12,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
  },
  header: {
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderBottomWidth: StyleSheet.hairlineWidth,
    gap: 4,
  },
  teamRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  teamSide: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  teamSideRight: {
    justifyContent: 'flex-end',
  },
  team: {
    flexShrink: 1,
    fontSize: 13,
    fontWeight: '600',
  },
  teamRight: {
    textAlign: 'right',
  },
  logo: {
    width: 18,
    height: 18,
  },
  logoPlaceholder: {
    width: 18,
    height: 18,
    borderRadius: 3,
  },
  vs: {
    fontSize: 11,
    fontWeight: '600',
    marginHorizontal: 8,
  },
  startLine: {
    fontSize: 11,
    fontVariant: ['tabular-nums'],
  },
  signalRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 10,
  },
  signalText: {
    flex: 1,
    gap: 3,
  },
  market: {
    fontSize: 13,
    fontWeight: '700',
  },
  outcome: {
    fontSize: 11,
    fontWeight: '500',
  },
  miniStats: {
    flexDirection: 'row',
    gap: 14,
    marginTop: 2,
  },
  miniStat: {
    alignItems: 'center',
  },
  miniLabel: {
    fontSize: 8,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
  miniValue: {
    fontSize: 11,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  gaugeWrap: {
    width: 52,
    alignItems: 'center',
  },
});
