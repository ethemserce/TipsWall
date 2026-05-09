import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { format } from 'date-fns';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  FlatList,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import {
  AnalysisFiltersSheet,
  countActiveFilters,
  DEFAULT_FILTERS,
  type AnalysisFilterState,
} from '@/src/components/AnalysisFiltersSheet';
import { AppBrand } from '@/src/components/AppBrand';
import { DateBar } from '@/src/components/DateBar';
import { MarketLegendButton } from '@/src/components/MarketLegendButton';
import { RateMatchCard } from '@/src/components/RateMatchCard';
import { LeagueSectionSkeleton } from '@/src/components/Skeleton';
import { useFixtureLookup } from '@/src/hooks/useFixtureLookup';
import { useMarkets } from '@/src/hooks/useMarkets';
import { useSignals } from '@/src/hooks/useSignals';
import { useTheme } from '@/src/lib/useTheme';
import type { SignalSort } from '@/src/api/signals';
import type { RateResult } from '@/src/types/rateResult';

const BOOKMAKER_ID = 2;
const DSO_COLOR = '#22c55e';

interface FixtureGroup {
  fixtureId: number;
  signals: RateResult[];
}

const SORTS: { key: SignalSort; label: string }[] = [
  { key: 'confidence', label: 'Önerilen' },
  { key: 'edge', label: 'Değer' },
  { key: 'winning', label: 'DSO' },
  { key: 'earning', label: 'VBET' },
  { key: 'odd', label: 'Oran' },
];

export function AnalysisScreen() {
  const c = useTheme();
  const { t } = useTranslation();
  const [selectedDate, setSelectedDate] = useState(() => new Date());
  const [sort, setSort] = useState<SignalSort>('confidence');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');
  const [filters, setFilters] = useState<AnalysisFilterState>(DEFAULT_FILTERS);
  const [filtersOpen, setFiltersOpen] = useState(false);

  const { data, isLoading, isFetching, isError, error, refetch } = useSignals({
    bookmakerId: BOOKMAKER_ID,
    fixtureDate: format(selectedDate, 'yyyy-MM-dd'),
    window: filters.window,
    sort,
    sortDir,
    minRate:
      filters.rateValue != null && filters.rateBound === 'min'
        ? filters.rateValue
        : undefined,
    maxRate:
      filters.rateValue != null && filters.rateBound === 'max'
        ? filters.rateValue
        : undefined,
    minWinningPercent: filters.dsoMin > 0 ? filters.dsoMin : undefined,
    minEarningPercent: filters.vbetMin > 0 ? filters.vbetMin : undefined,
    minSampleCount: filters.kzMin,
    valueOnly: filters.valueOnly || undefined,
    topPerFixture: filters.topPerFixture ?? undefined,
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

  const activeCount = countActiveFilters(filters);

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.headerRow}>
        <AppBrand />
        <View style={styles.headerRight}>
          <MarketLegendButton />
        </View>
      </View>

      <DateBar selectedDate={selectedDate} onSelect={setSelectedDate} />

      {/* Single-line control row: Filtre button + Sort chips */}
      <View style={[styles.controlRow, { borderBottomColor: c.border }]}>
        <Pressable
          onPress={() => setFiltersOpen(true)}
          style={[
            styles.filterBtn,
            {
              backgroundColor: activeCount > 0 ? c.brand : 'transparent',
              borderColor: activeCount > 0 ? c.brand : c.border,
            },
          ]}>
          <MaterialCommunityIcons
            name="tune-variant"
            size={14}
            color={activeCount > 0 ? c.textInverse : c.textMuted}
          />
          <ThemedText
            style={[
              styles.filterBtnText,
              { color: activeCount > 0 ? c.textInverse : c.text },
            ]}>
            Filtre
          </ThemedText>
          {activeCount > 0 ? (
            <View style={[styles.badge, { backgroundColor: c.textInverse }]}>
              <ThemedText style={[styles.badgeText, { color: c.brand }]}>
                {activeCount}
              </ThemedText>
            </View>
          ) : null}
        </Pressable>

        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={styles.sortChipsContent}>
          {SORTS.map((s) => {
            const active = sort === s.key;
            const onPress = () => {
              if (active) {
                // Re-tapping the active sort flips its direction.
                setSortDir((d) => (d === 'desc' ? 'asc' : 'desc'));
              } else {
                setSort(s.key);
                setSortDir('desc'); // new sort defaults to "best first"
              }
            };
            return (
              <Pressable
                key={s.key}
                onPress={onPress}
                style={[
                  styles.sortPill,
                  { borderColor: c.border },
                  active && { backgroundColor: c.brand, borderColor: c.brand },
                ]}>
                <ThemedText
                  style={[
                    styles.sortText,
                    { color: active ? c.textInverse : c.textMuted },
                  ]}>
                  {s.label}
                </ThemedText>
                {active ? (
                  <MaterialCommunityIcons
                    name={sortDir === 'desc' ? 'arrow-down' : 'arrow-up'}
                    size={11}
                    color={c.textInverse}
                    style={styles.sortArrow}
                  />
                ) : null}
              </Pressable>
            );
          })}
        </ScrollView>
      </View>

      {isLoading ? (
        <ScrollView contentContainerStyle={styles.list}>
          <LeagueSectionSkeleton rows={3} />
          <LeagueSectionSkeleton rows={2} />
          <LeagueSectionSkeleton rows={2} />
        </ScrollView>
      ) : isError ? (
        <View style={styles.center}>
          <ThemedText style={[styles.errorTitle, { color: c.text }]}>
            {t('common.couldNotLoad')}
          </ThemedText>
          <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
            {error instanceof Error ? error.message : t('common.somethingWentWrong')}
          </ThemedText>
        </View>
      ) : (
        <FlatList
          data={sortedGroups}
          keyExtractor={(g) => String(g.fixtureId)}
          renderItem={({ item }) => (
            <RateMatchCard
              fixtureId={item.fixtureId}
              fixture={fixtureLookup.get(item.fixtureId)}
              signals={item.signals}
              marketLookup={marketLookup}
              primaryMetric={
                sort === 'earning' ? 'earning_percent' : 'winning_percent'
              }
              gaugeColor={DSO_COLOR}
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

      <AnalysisFiltersSheet
        visible={filtersOpen}
        filters={filters}
        onApply={setFilters}
        onClose={() => setFiltersOpen(false)}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  headerRow: {
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 4,
    justifyContent: 'center',
  },
  headerRight: {
    position: 'absolute',
    right: 12,
    top: 8,
  },
  controlRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 8,
    gap: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  filterBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
  },
  filterBtnText: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
  badge: {
    minWidth: 16,
    height: 16,
    borderRadius: 8,
    paddingHorizontal: 4,
    alignItems: 'center',
    justifyContent: 'center',
  },
  badgeText: {
    fontSize: 10,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  sortChipsContent: {
    flexDirection: 'row',
    gap: 4,
    alignItems: 'center',
  },
  sortPill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 3,
    paddingHorizontal: 9,
    paddingVertical: 5,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
  },
  sortText: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
  sortArrow: {
    marginLeft: 1,
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
