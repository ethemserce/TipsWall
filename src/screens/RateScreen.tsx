import { format } from 'date-fns';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  FlatList,
  RefreshControl,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { AppBrand } from '@/src/components/AppBrand';
import { DateBar } from '@/src/components/DateBar';
import { RateFilterBar, type RateFilters } from '@/src/components/RateFilterBar';
import { RateMatchCard } from '@/src/components/RateMatchCard';
import { RateSummaryCard } from '@/src/components/RateSummaryCard';
import { useFixtureLookup } from '@/src/hooks/useFixtureLookup';
import { useMarkets } from '@/src/hooks/useMarkets';
import { useRate, type RateKind } from '@/src/hooks/useRates';
import { useTheme } from '@/src/lib/useTheme';
import type { RateResult } from '@/src/types/rateResult';

interface RateScreenProps {
  kind: RateKind;
  title: string;
  primaryMetric: 'winning_percent' | 'earning_percent';
}

const BOOKMAKER_ID = 1;
const NOT_STARTED_STATE_ID = 1;
const DSO_COLOR = '#22c55e';
const VBET_COLOR = '#f59e0b';

interface FixtureGroup {
  fixtureId: number;
  signals: RateResult[];
}

export function RateScreen({ kind, title, primaryMetric }: RateScreenProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const [selectedDate, setSelectedDate] = useState(() => new Date());
  const [filters, setFilters] = useState<RateFilters>({
    window: 'all',
    minRate: null,
    notStartedOnly: false,
  });

  const { data, isLoading, isFetching, isError, error, refetch } = useRate(kind, {
    bookmakerId: BOOKMAKER_ID,
    fixtureDate: format(selectedDate, 'yyyy-MM-dd'),
    window: filters.window,
    minRate: filters.minRate ?? undefined,
    matchState: filters.notStartedOnly ? NOT_STARTED_STATE_ID : undefined,
    perPage: 100,
  });

  const { lookup: marketLookup } = useMarkets();
  const items = data?.data.items ?? [];

  const groups = useMemo<FixtureGroup[]>(() => {
    const map = new Map<number, RateResult[]>();
    for (const r of items) {
      const list = map.get(r.fixture_id);
      if (list) list.push(r);
      else map.set(r.fixture_id, [r]);
    }
    return Array.from(map.entries()).map(([fixtureId, signals]) => ({
      fixtureId,
      signals,
    }));
  }, [items]);

  const fixtureIds = useMemo(() => groups.map((g) => g.fixtureId), [groups]);
  const { lookup: fixtureLookup } = useFixtureLookup(fixtureIds);

  // Sort groups by earliest kickoff so upcoming/today's matches surface first.
  const sortedGroups = useMemo(() => {
    return [...groups].sort((a, b) => {
      const ta = fixtureLookup.get(a.fixtureId)?.fixture.starting_at ?? '';
      const tb = fixtureLookup.get(b.fixtureId)?.fixture.starting_at ?? '';
      if (ta && tb) return ta.localeCompare(tb);
      if (ta) return -1;
      if (tb) return 1;
      return a.fixtureId - b.fixtureId;
    });
  }, [groups, fixtureLookup]);

  const gaugeColor = primaryMetric === 'earning_percent' ? VBET_COLOR : DSO_COLOR;

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.headerRow}>
        <AppBrand />
      </View>
      <View style={[styles.titleRow, { borderBottomColor: c.border }]}>
        <ThemedText style={[styles.title, { color: c.text }]}>{title}</ThemedText>
      </View>

      <DateBar selectedDate={selectedDate} onSelect={setSelectedDate} />

      <RateFilterBar filters={filters} onChange={setFilters} />

      {isLoading ? (
        <View style={styles.center}>
          <ActivityIndicator color={c.brand} />
        </View>
      ) : isError ? (
        <View style={styles.center}>
          <ThemedText style={[styles.errorTitle, { color: c.text }]}>
            {t('rate.couldNotLoad', { title })}
          </ThemedText>
          <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
            {error instanceof Error ? error.message : t('common.somethingWentWrong')}
          </ThemedText>
        </View>
      ) : (
        <FlatList
          ListHeaderComponent={
            data ? (
              <RateSummaryCard
                summary={data.data.summary}
                asOfDate={data.data.as_of_date}
              />
            ) : null
          }
          data={sortedGroups}
          keyExtractor={(g) => String(g.fixtureId)}
          renderItem={({ item }) => (
            <RateMatchCard
              fixtureId={item.fixtureId}
              fixture={fixtureLookup.get(item.fixtureId)}
              signals={item.signals}
              marketLookup={marketLookup}
              primaryMetric={primaryMetric}
              gaugeColor={gaugeColor}
            />
          )}
          contentContainerStyle={styles.list}
          ListEmptyComponent={
            <View style={styles.center}>
              <ThemedText style={{ color: c.textMuted }}>
                {t('rate.noResults')}
              </ThemedText>
            </View>
          }
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
  list: {
    paddingTop: 4,
    paddingBottom: 32,
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
