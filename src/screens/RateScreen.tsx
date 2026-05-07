import { useRouter } from 'expo-router';
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  RefreshControl,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { AppBrand } from '@/src/components/AppBrand';
import { useRate, type RateKind } from '@/src/hooks/useRates';
import { useTheme } from '@/src/lib/useTheme';
import type { RateResult } from '@/src/types/rateResult';

interface RateScreenProps {
  kind: RateKind;
  title: string;
  primaryMetric: 'winning_percent' | 'earning_percent';
}

const BOOKMAKER_ID = 1;
const WINDOW = 'all';

export function RateScreen({ kind, title, primaryMetric }: RateScreenProps) {
  const c = useTheme();
  const { data, isLoading, isFetching, isError, error, refetch } = useRate(kind, {
    bookmakerId: BOOKMAKER_ID,
    window: WINDOW,
    perPage: 50,
  });

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.headerRow}>
        <AppBrand />
      </View>
      <View style={[styles.titleRow, { borderBottomColor: c.border }]}>
        <ThemedText style={[styles.title, { color: c.text }]}>{title}</ThemedText>
        <ThemedText style={[styles.subtitle, { color: c.textMuted }]}>
          Bookmaker #{BOOKMAKER_ID} · Window: {WINDOW}
        </ThemedText>
      </View>

      {isLoading ? (
        <View style={styles.center}>
          <ActivityIndicator color={c.brand} />
        </View>
      ) : isError ? (
        <View style={styles.center}>
          <ThemedText style={[styles.errorTitle, { color: c.text }]}>
            Couldn&apos;t load {title.toLowerCase()}
          </ThemedText>
          <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
            {error instanceof Error ? error.message : 'Unknown error'}
          </ThemedText>
        </View>
      ) : (data?.items ?? []).length === 0 ? (
        <View style={styles.center}>
          <ThemedText style={{ color: c.textMuted }}>
            No results in this window yet.
          </ThemedText>
        </View>
      ) : (
        <FlatList
          data={data?.items ?? []}
          keyExtractor={(it) => it.id}
          renderItem={({ item }) => (
            <RateRow item={item} primaryMetric={primaryMetric} />
          )}
          contentContainerStyle={styles.list}
          refreshControl={
            <RefreshControl
              refreshing={isFetching}
              onRefresh={refetch}
              tintColor={c.brand}
            />
          }
        />
      )}
    </SafeAreaView>
  );
}

function RateRow({
  item,
  primaryMetric,
}: {
  item: RateResult;
  primaryMetric: 'winning_percent' | 'earning_percent';
}) {
  const c = useTheme();
  const router = useRouter();
  const primaryValue = item[primaryMetric];
  const primaryColor =
    primaryMetric === 'earning_percent'
      ? primaryValue != null && primaryValue > 0
        ? c.brand
        : primaryValue != null && primaryValue < 0
          ? c.live
          : c.text
      : c.brand;

  const outcomeLabel = formatOutcome(item);
  return (
    <Pressable
      onPress={() => router.push(`/fixture/${item.fixture_id}` as never)}
      style={({ pressed }) => [
        styles.card,
        {
          backgroundColor: pressed ? c.surfaceElevated : c.surface,
          borderColor: c.border,
        },
      ]}>
      <View style={[styles.rankCol, { backgroundColor: c.bg }]}>
        <ThemedText style={[styles.rankText, { color: c.text }]}>
          #{item.rank_order}
        </ThemedText>
      </View>
      <View style={styles.infoCol}>
        <ThemedText style={[styles.outcomeText, { color: c.text }]} numberOfLines={1}>
          {outcomeLabel}
        </ThemedText>
        <ThemedText style={[styles.metaText, { color: c.textMuted }]} numberOfLines={1}>
          Market #{item.market_id} · Fixture #{item.fixture_id}
        </ThemedText>
        <View style={styles.statsRow}>
          <Stat label="ORAN" value={formatOdd(item.odd_value)} />
          <Stat label="KZ" value={String(item.win_count)} />
          <Stat label="KY" value={String(item.lost_count)} />
        </View>
      </View>
      <View style={styles.metricCol}>
        <ThemedText style={[styles.metricValue, { color: primaryColor }]}>
          {formatPercent(primaryValue)}
        </ThemedText>
        <ThemedText style={[styles.metricLabel, { color: c.textMuted }]}>
          {primaryMetric === 'winning_percent' ? 'WIN %' : 'ROI %'}
        </ThemedText>
      </View>
    </Pressable>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  const c = useTheme();
  return (
    <View style={styles.stat}>
      <ThemedText style={[styles.statLabel, { color: c.textMuted }]}>{label}</ThemedText>
      <ThemedText style={[styles.statValue, { color: c.text }]}>{value}</ThemedText>
    </View>
  );
}

function formatOutcome(item: RateResult): string {
  const parts = [item.label];
  if (item.total) parts.push(item.total);
  if (item.handicap) parts.push(`(${item.handicap})`);
  return parts.join(' ');
}

function formatOdd(value: number | null): string {
  return value != null ? value.toFixed(2) : '-';
}

function formatPercent(value: number | null): string {
  if (value == null) return '-';
  const sign = value > 0 ? '+' : '';
  return `${sign}${value.toFixed(1)}%`;
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  headerRow: {
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 4,
  },
  titleRow: {
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 12,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  title: {
    fontSize: 22,
    fontWeight: '700',
  },
  subtitle: {
    fontSize: 12,
    marginTop: 2,
  },
  list: {
    paddingHorizontal: 12,
    paddingTop: 8,
    paddingBottom: 32,
    gap: 8,
  },
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 12,
    borderRadius: 12,
    borderWidth: StyleSheet.hairlineWidth,
    gap: 12,
  },
  rankCol: {
    width: 44,
    paddingVertical: 8,
    paddingHorizontal: 6,
    borderRadius: 8,
    alignItems: 'center',
  },
  rankText: {
    fontSize: 12,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  infoCol: {
    flex: 1,
    gap: 4,
  },
  outcomeText: {
    fontSize: 14,
    fontWeight: '600',
  },
  metaText: {
    fontSize: 11,
  },
  statsRow: {
    flexDirection: 'row',
    gap: 16,
    marginTop: 2,
  },
  stat: {
    alignItems: 'center',
  },
  statLabel: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.4,
  },
  statValue: {
    fontSize: 12,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  metricCol: {
    width: 64,
    alignItems: 'center',
  },
  metricValue: {
    fontSize: 16,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  metricLabel: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.4,
    marginTop: 2,
  },
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 32,
    gap: 8,
  },
  errorTitle: {
    fontSize: 16,
    fontWeight: '600',
  },
  errorMessage: {
    fontSize: 13,
    textAlign: 'center',
  },
});
