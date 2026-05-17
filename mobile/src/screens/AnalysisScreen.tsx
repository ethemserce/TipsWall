import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { format } from 'date-fns';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  TextInput,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import {
  AnalysisFiltersSheet,
  countActiveFilters,
  DEFAULT_FILTERS,
  RISK_THRESHOLDS,
  type AnalysisFilterState,
} from '@/src/components/AnalysisFiltersSheet';
import { AnalysisQuickPicksSheet } from '@/src/components/AnalysisQuickPicksSheet';
import { AppBrand } from '@/src/components/AppBrand';
import { DateBar } from '@/src/components/DateBar';
import { LeagueHeader } from '@/src/components/LeagueHeader';
import { LeagueScopeSheet, type LeagueScopeRow } from '@/src/components/LeagueScopeSheet';
import { MarketLegendButton } from '@/src/components/MarketLegendButton';
import { RateMatchCard } from '@/src/components/RateMatchCard';
import { LeagueSectionSkeleton } from '@/src/components/Skeleton';
import { StateFilterBar, type FixtureFilter } from '@/src/components/StateFilterBar';
import { useCountryLookup } from '@/src/hooks/useCountryLookup';
import { useFixtureLookup } from '@/src/hooks/useFixtureLookup';
import { useLeagueLookup } from '@/src/hooks/useLeagueLookup';
import { useLiveTicker } from '@/src/hooks/useLiveTicker';
import { useManualRefresh } from '@/src/hooks/useManualRefresh';
import { useMarketPreferences } from '@/src/hooks/useMarketPreferences';
import { useMarkets } from '@/src/hooks/useMarkets';
import { useInfiniteSignals } from '@/src/hooks/useSignals';
import { useUserCountryId } from '@/src/hooks/useUserCountry';
import { getStateBucket } from '@/src/lib/fixtureState';
import { useTheme } from '@/src/lib/useTheme';
import type { SignalSort } from '@/src/api/signals';
import type { RateResult } from '@/src/types/rateResult';

const BOOKMAKER_ID = 2;
const DSO_COLOR = '#22c55e';

interface FixtureGroup {
  fixtureId: number;
  signals: RateResult[];
}

interface LeagueGroup {
  leagueId: number;
  fixtures: FixtureGroup[];
}

// Sort is fixed to "confidence desc" — the chips that let users override
// it (Önerilen / Değer / HIT / ROI / Oran) were removed; users tune the
// list via the Filtre sheet only.
const FIXED_SORT: SignalSort = 'confidence';
const FIXED_SORT_DIR: 'asc' | 'desc' = 'desc';

// Turkish-aware lowercasing — matches the home page's search behaviour.
const normalizeForSearch = (value: string | null | undefined): string =>
  (value ?? '').toLocaleLowerCase('tr-TR');

export function AnalysisScreen() {
  const c = useTheme();
  const { t } = useTranslation();
  const userCountryId = useUserCountryId();
  const [selectedDate, setSelectedDate] = useState(() => new Date());
  const [filters, setFilters] = useState<AnalysisFilterState>(DEFAULT_FILTERS);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [quickPicksOpen, setQuickPicksOpen] = useState(false);
  // Same all/live/upcoming/finished filter the matches tab uses, applied
  // on top of search + analysis filters. Lives at the fixture level —
  // signals stay grouped per fixture so a single match either passes or
  // fails as a whole.
  const [stateFilter, setStateFilter] = useState<FixtureFilter>('all');
  // League scope — empty set means "no scope, show everything"; any
  // non-empty set means "only these leagues". Stable across date changes
  // by design: league ids persist while the available fixtures rotate
  // (a stale id naturally filters out whatever isn't on the new date).
  const [scopedLeagueIds, setScopedLeagueIds] = useState<Set<number>>(
    () => new Set(),
  );
  const [scopeSheetOpen, setScopeSheetOpen] = useState(false);

  // SignalR push subscriber. Idempotent if Home is already mounted —
  // the underlying connection is a singleton, handlers stack. With it
  // here, the analysis screen still gets fresh live_minute when the
  // user deep-links straight into the tab without visiting Matches.
  useLiveTicker();
  const [searchOpen, setSearchOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const searchInputRef = useRef<TextInput | null>(null);

  const handleToggleSearch = useCallback(() => {
    setSearchOpen((prev) => {
      const next = !prev;
      if (!next) setSearchQuery('');
      else setTimeout(() => searchInputRef.current?.focus(), 0);
      return next;
    });
  }, []);

  // Risk category → backend min/max rate. The user never sees the raw oran
  // number, but the snapshots still key on it, so the screen translates.
  const riskBounds =
    filters.riskCategory != null
      ? RISK_THRESHOLDS[filters.riskCategory]
      : null;
  const { marketIds: favouriteMarketIds } = useMarketPreferences();

  const {
    data,
    isLoading,
    isError,
    error,
    refetch,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useInfiniteSignals({
    bookmakerId: BOOKMAKER_ID,
    fixtureDate: format(selectedDate, 'yyyy-MM-dd'),
    window: filters.window,
    sort: FIXED_SORT,
    sortDir: FIXED_SORT_DIR,
    minRate: riskBounds?.minRate,
    maxRate: riskBounds?.maxRate,
    minWinningPercent: filters.dsoMin > 0 ? filters.dsoMin : undefined,
    minEarningPercent: filters.vbetMin > 0 ? filters.vbetMin : undefined,
    minSampleCount: filters.kzMin,
    valueOnly: filters.valueOnly || undefined,
    topPerFixture: filters.topPerFixture ?? undefined,
    // Favourite markets gate: only fetch signals from markets the user
    // pinned. Backend treats an empty list as "no filter" (returns
    // every available_in_standard market) so we omit the param when
    // there's no selection.
    marketIds: favouriteMarketIds.length > 0 ? favouriteMarketIds : undefined,
  });
  const { refreshing, onRefresh } = useManualRefresh(refetch);

  const { lookup: marketLookup } = useMarkets();
  // Flatten paginated rows into the same shape the rest of the screen
  // already operates on — grouping, search and lookup don't care that
  // the data arrived in pages.
  const items = useMemo(
    () => data?.pages.flatMap((p) => p.data.items) ?? [],
    [data?.pages],
  );

  const handleEndReached = useCallback(() => {
    if (hasNextPage && !isFetchingNextPage) {
      void fetchNextPage();
    }
  }, [hasNextPage, isFetchingNextPage, fetchNextPage]);

  const fixtureGroups = useMemo<FixtureGroup[]>(() => {
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

  const fixtureIds = useMemo(
    () => fixtureGroups.map((g) => g.fixtureId),
    [fixtureGroups],
  );
  const { lookup: fixtureLookup } = useFixtureLookup(fixtureIds);

  // Pull in league + country info so we can group, label, and filter by
  // them. Both lookups dedup by id internally.
  const leagueIds = useMemo(() => {
    const ids = new Set<number>();
    for (const f of fixtureLookup.values()) {
      if (f.fixture.league_id != null) ids.add(f.fixture.league_id);
    }
    return Array.from(ids);
  }, [fixtureLookup]);
  const { lookup: leagueLookup } = useLeagueLookup(leagueIds);

  const countryIds = useMemo(
    () => Array.from(leagueLookup.values()).map((l) => l.country_id),
    [leagueLookup],
  );
  const { lookup: countryLookup } = useCountryLookup(countryIds);

  // Search across team and league + country names. Same matching rules as
  // the home list so users get a consistent experience across screens.
  const normalizedQuery = useMemo(
    () => normalizeForSearch(searchQuery.trim()),
    [searchQuery],
  );
  const searchActive = normalizedQuery.length > 0;
  const filteredFixtureGroups = useMemo(() => {
    if (!searchActive) return fixtureGroups;
    return fixtureGroups.filter((g) => {
      const fixture = fixtureLookup.get(g.fixtureId)?.fixture;
      if (!fixture) return false;
      if (
        normalizeForSearch(fixture.home_team_name).includes(normalizedQuery) ||
        normalizeForSearch(fixture.away_team_name).includes(normalizedQuery)
      ) {
        return true;
      }
      const league = fixture.league_id
        ? leagueLookup.get(fixture.league_id)
        : undefined;
      if (normalizeForSearch(league?.name).includes(normalizedQuery)) return true;
      const country = league?.country_id
        ? countryLookup.get(league.country_id)
        : undefined;
      return normalizeForSearch(country?.name).includes(normalizedQuery);
    });
  }, [fixtureGroups, fixtureLookup, leagueLookup, countryLookup, normalizedQuery, searchActive]);

  // League scope — applied BEFORE the state filter so the picker rows
  // stay stable as the user toggles all/live/upcoming/finished. The
  // alternative (scope after state) would silently drop the user's
  // league selection whenever a chosen league had no fixtures in the
  // current state bucket. Empty set = pass-through.
  const leagueScopedFixtureGroups = useMemo(() => {
    if (scopedLeagueIds.size === 0) return filteredFixtureGroups;
    return filteredFixtureGroups.filter((g) => {
      const fx = fixtureLookup.get(g.fixtureId)?.fixture;
      if (!fx || fx.league_id == null) return false;
      return scopedLeagueIds.has(fx.league_id);
    });
  }, [filteredFixtureGroups, fixtureLookup, scopedLeagueIds]);

  // State filter — narrows the scoped set into one of all/live/upcoming/
  // finished. Counts therefore reflect what's reachable given the
  // current scope: with Süper Lig+PL chosen, Live count = live matches
  // in those leagues only.
  const stateFilteredFixtureGroups = useMemo(() => {
    if (stateFilter === 'all') return leagueScopedFixtureGroups;
    return leagueScopedFixtureGroups.filter((g) => {
      const fx = fixtureLookup.get(g.fixtureId)?.fixture;
      if (!fx) return false;
      return getStateBucket(fx.state_id) === stateFilter;
    });
  }, [leagueScopedFixtureGroups, fixtureLookup, stateFilter]);

  const stateCounts = useMemo<Record<FixtureFilter, number>>(() => {
    const acc: Record<FixtureFilter, number> = {
      all: 0,
      live: 0,
      upcoming: 0,
      finished: 0,
    };
    for (const g of leagueScopedFixtureGroups) {
      acc.all++;
      const fx = fixtureLookup.get(g.fixtureId)?.fixture;
      if (!fx) continue;
      const bucket = getStateBucket(fx.state_id);
      if (bucket === 'live') acc.live++;
      else if (bucket === 'upcoming') acc.upcoming++;
      else if (bucket === 'finished') acc.finished++;
    }
    return acc;
  }, [leagueScopedFixtureGroups, fixtureLookup]);

  // Picker rows are derived from `filteredFixtureGroups` (search-only,
  // pre-scope, pre-state) — the user always sees every league available
  // for the date, with the per-league match count showing the day's
  // total. State filter doesn't shrink the picker; scope and state are
  // orthogonal dimensions.
  const scopePickerRows = useMemo<LeagueScopeRow[]>(() => {
    const counts = new Map<number, number>();
    for (const fg of filteredFixtureGroups) {
      const fx = fixtureLookup.get(fg.fixtureId)?.fixture;
      if (!fx || fx.league_id == null) continue;
      counts.set(fx.league_id, (counts.get(fx.league_id) ?? 0) + 1);
    }
    return Array.from(counts.entries())
      .map(([leagueId, matchCount]) => {
        const league = leagueLookup.get(leagueId);
        const country =
          league?.country_id != null
            ? countryLookup.get(league.country_id)
            : undefined;
        return { leagueId, league, country, matchCount };
      })
      .sort((a, b) => {
        // Same primary sort as the on-screen list: home league first,
        // then category, then name. Keeps the picker order intuitive.
        const homeA = userCountryId != null && a.country?.id === userCountryId ? 0 : 1;
        const homeB = userCountryId != null && b.country?.id === userCountryId ? 0 : 1;
        if (homeA !== homeB) return homeA - homeB;
        const ca = a.league?.category ?? Number.MAX_SAFE_INTEGER;
        const cb = b.league?.category ?? Number.MAX_SAFE_INTEGER;
        if (ca !== cb) return ca - cb;
        return (a.league?.name ?? `Z${a.leagueId}`).localeCompare(
          b.league?.name ?? `Z${b.leagueId}`,
        );
      });
  }, [filteredFixtureGroups, fixtureLookup, leagueLookup, countryLookup, userCountryId]);

  // Drop selections whose league no longer appears in the current set —
  // e.g. user picked Süper Lig on Tuesday, changed date to Wednesday
  // where Süper Lig has no fixtures. Keeping it selected would leave a
  // misleading "3 lig" count that filters down to 0 matches. Empty
  // selection = implicit "all" and is the natural fallback.
  useEffect(() => {
    if (scopedLeagueIds.size === 0) return;
    const availableIds = new Set(scopePickerRows.map((r) => r.leagueId));
    const stillValid = new Set<number>();
    for (const id of scopedLeagueIds) {
      if (availableIds.has(id)) stillValid.add(id);
    }
    if (stillValid.size !== scopedLeagueIds.size) {
      setScopedLeagueIds(stillValid);
    }
  }, [scopePickerRows, scopedLeagueIds]);

  const scopeActive = scopedLeagueIds.size > 0;

  // Group fixtures by league, ordered like the home list (by league
  // category first, then name).
  const leagueGroups = useMemo<LeagueGroup[]>(() => {
    const map = new Map<number, FixtureGroup[]>();
    const orphans: FixtureGroup[] = [];
    for (const fg of stateFilteredFixtureGroups) {
      const fixture = fixtureLookup.get(fg.fixtureId)?.fixture;
      const lid = fixture?.league_id;
      if (lid == null) {
        orphans.push(fg);
        continue;
      }
      const existing = map.get(lid);
      if (existing) existing.push(fg);
      else map.set(lid, [fg]);
    }
    // Within each league sort fixtures by kickoff time.
    for (const list of map.values()) {
      list.sort((a, b) => {
        const ta = fixtureLookup.get(a.fixtureId)?.fixture.starting_at ?? '';
        const tb = fixtureLookup.get(b.fixtureId)?.fixture.starting_at ?? '';
        return ta.localeCompare(tb);
      });
    }
    const sorted: LeagueGroup[] = Array.from(map.entries())
      .map(([leagueId, fixtures]) => ({ leagueId, fixtures }))
      .sort((a, b) => {
        const la = leagueLookup.get(a.leagueId);
        const lb = leagueLookup.get(b.leagueId);
        // National league first — mirrors the home tab so the sort is
        // consistent across screens. See TodayMatchesScreen sort.
        const homeA = userCountryId != null && la?.country_id === userCountryId ? 0 : 1;
        const homeB = userCountryId != null && lb?.country_id === userCountryId ? 0 : 1;
        if (homeA !== homeB) return homeA - homeB;
        const ca = la?.category ?? Number.MAX_SAFE_INTEGER;
        const cb = lb?.category ?? Number.MAX_SAFE_INTEGER;
        if (ca !== cb) return ca - cb;
        const na = la?.name ?? `Z${a.leagueId}`;
        const nb = lb?.name ?? `Z${b.leagueId}`;
        return na.localeCompare(nb);
      });
    if (orphans.length > 0) {
      // -1 sentinel keeps orphan fixtures (no league_id resolved yet)
      // visible under a "Diğer" section instead of dropping them.
      sorted.push({ leagueId: -1, fixtures: orphans });
    }
    return sorted;
  }, [stateFilteredFixtureGroups, fixtureLookup, leagueLookup, userCountryId]);

  // Per-league collapsed set + bulk toggle, mirroring the home list.
  const [collapsed, setCollapsed] = useState<Set<number>>(() => new Set());
  const toggleLeague = useCallback((leagueId: number) => {
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(leagueId)) next.delete(leagueId);
      else next.add(leagueId);
      return next;
    });
  }, []);
  const allCollapsed =
    leagueGroups.length > 0 &&
    leagueGroups.every((g) => collapsed.has(g.leagueId));
  // Only a one-way "collapse all" — expanding every league at once
  // was lagging the screen (each section eagerly renders 10-30 rows
  // with HIT/ROI/IMP calculations). Users can still expand a single
  // league header to drill in.
  const collapseAll = useCallback(() => {
    setCollapsed(new Set(leagueGroups.map((g) => g.leagueId)));
  }, [leagueGroups]);

  const visibleFixtureCount = useMemo(
    () => leagueGroups.reduce((acc, g) => acc + g.fixtures.length, 0),
    [leagueGroups],
  );

  // Live pip per league (any fixture currently in play).
  const sectionLiveSet = useMemo(() => {
    const out = new Set<number>();
    for (const g of leagueGroups) {
      for (const fg of g.fixtures) {
        const fixture = fixtureLookup.get(fg.fixtureId)?.fixture;
        if (fixture && getStateBucket(fixture.state_id) === 'live') {
          out.add(g.leagueId);
          break;
        }
      }
    }
    return out;
  }, [leagueGroups, fixtureLookup]);

  const activeCount = countActiveFilters(filters);

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.headerRow}>
        <Pressable
          onPress={() => setQuickPicksOpen(true)}
          hitSlop={12}
          accessibilityRole="button"
          accessibilityLabel={t('analysis.quickPicks.openA11y', {
            defaultValue: "Günün önerilerini aç",
          })}
          style={({ pressed }) => [
            styles.headerFlashBtn,
            {
              // Transparent button, red flash icon — colour grabs attention
              // without the full pill background dominating the header.
              backgroundColor:
                pressed || quickPicksOpen ? c.dangerSoft ?? c.brandSoft : 'transparent',
            },
          ]}>
          <MaterialCommunityIcons
            name="flash"
            size={22}
            color={c.danger ?? '#ef4444'}
          />
        </Pressable>
        <AppBrand />
        <Pressable
          onPress={handleToggleSearch}
          hitSlop={12}
          accessibilityRole="button"
          accessibilityLabel={
            searchOpen ? t('home.search.a11yClose') : t('home.search.a11yOpen')
          }
          style={({ pressed }) => [
            styles.headerSearchBtn,
            {
              backgroundColor:
                pressed || searchOpen ? c.brandSoft : 'transparent',
            },
          ]}>
          <MaterialCommunityIcons
            name={searchOpen ? 'close' : 'magnify'}
            size={22}
            color={searchOpen ? c.brand : c.textMuted}
          />
        </Pressable>
      </View>

      {searchOpen ? (
        <View
          style={[
            styles.searchRow,
            { backgroundColor: c.surface, borderColor: c.borderSoft },
          ]}>
          <MaterialCommunityIcons name="magnify" size={18} color={c.textMuted} />
          <TextInput
            ref={searchInputRef}
            value={searchQuery}
            onChangeText={setSearchQuery}
            placeholder={t('home.search.placeholder')}
            placeholderTextColor={c.textMuted}
            autoCapitalize="none"
            autoCorrect={false}
            returnKeyType="search"
            style={[styles.searchInput, { color: c.text }]}
          />
          {searchQuery.length > 0 ? (
            <Pressable
              onPress={() => setSearchQuery('')}
              hitSlop={10}
              accessibilityRole="button"
              accessibilityLabel={t('home.search.a11yClear')}>
              <MaterialCommunityIcons
                name="close-circle"
                size={18}
                color={c.textMuted}
              />
            </Pressable>
          ) : null}
        </View>
      ) : null}

      <DateBar selectedDate={selectedDate} onSelect={setSelectedDate} />

      {/* Filter button + Legend share the control row. Filter on the left
          because it's more often used; legend stays as a passive reference. */}
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
            {t('analysis.filtersButton')}
          </ThemedText>
          {activeCount > 0 ? (
            <View style={[styles.badge, { backgroundColor: c.textInverse }]}>
              <ThemedText style={[styles.badgeText, { color: c.brand }]}>
                {activeCount}
              </ThemedText>
            </View>
          ) : null}
        </Pressable>
        <View style={styles.spacer} />
        <MarketLegendButton />
      </View>

      <StateFilterBar
        selected={stateFilter}
        onSelect={setStateFilter}
        counts={stateCounts}
      />

      {leagueGroups.length > 0 || scopeActive ? (
        <View style={styles.metaRow}>
          <Pressable
            onPress={() => setScopeSheetOpen(true)}
            hitSlop={8}
            accessibilityRole="button"
            accessibilityLabel={t('leagueScope.openA11y')}
            style={({ pressed }) => [
              styles.summaryBtn,
              {
                backgroundColor: scopeActive
                  ? c.brandSoft
                  : pressed
                    ? c.surface
                    : 'transparent',
                borderColor: scopeActive ? c.brand : c.borderSoft,
              },
            ]}>
            <MaterialCommunityIcons
              name="filter-variant"
              size={12}
              color={scopeActive ? c.brand : c.textMuted}
            />
            <ThemedText
              style={[
                styles.metaText,
                { color: scopeActive ? c.brand : c.textMuted },
              ]}>
              {t('home.meta.summary', {
                leagues: leagueGroups.length,
                matches: visibleFixtureCount,
              })}
            </ThemedText>
            {scopeActive ? (
              <Pressable
                onPress={(e) => {
                  e.stopPropagation();
                  setScopedLeagueIds(new Set());
                }}
                hitSlop={8}
                accessibilityRole="button"
                accessibilityLabel={t('leagueScope.clearA11y')}>
                <MaterialCommunityIcons
                  name="close-circle"
                  size={14}
                  color={c.brand}
                />
              </Pressable>
            ) : null}
          </Pressable>
          {allCollapsed || leagueGroups.length === 0 ? null : (
            <Pressable
              onPress={collapseAll}
              hitSlop={6}
              style={({ pressed }) => [
                styles.toggleAllBtn,
                {
                  backgroundColor: pressed ? c.brandSoft : 'transparent',
                  borderColor: c.borderSoft,
                },
              ]}>
              <MaterialCommunityIcons
                name="unfold-less-horizontal"
                size={14}
                color={c.brand}
              />
              <ThemedText style={[styles.toggleAllText, { color: c.brand }]}>
                {t('home.toggleAll.collapse')}
              </ThemedText>
            </Pressable>
          )}
        </View>
      ) : null}

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
          data={leagueGroups}
          keyExtractor={(g) => String(g.leagueId)}
          keyboardShouldPersistTaps="handled"
          onEndReached={handleEndReached}
          onEndReachedThreshold={0.4}
          ListFooterComponent={
            isFetchingNextPage ? (
              <View style={styles.footerLoading}>
                <ActivityIndicator color={c.brand} />
              </View>
            ) : null
          }
          renderItem={({ item }) => {
            const league = leagueLookup.get(item.leagueId);
            const country =
              league?.country_id != null
                ? countryLookup.get(league.country_id)
                : undefined;
            // While search is active sections auto-expand so hits are
            // visible without first tapping the league header.
            const isCollapsed =
              !searchActive && collapsed.has(item.leagueId);
            return (
              <View
                style={[
                  styles.sectionCard,
                  c.shadowCard,
                  {
                    backgroundColor: c.surfaceElevated,
                    borderColor: c.borderSoft,
                  },
                ]}>
                <LeagueHeader
                  leagueId={item.leagueId}
                  league={league}
                  country={country}
                  fixtureCount={item.fixtures.length}
                  collapsed={isCollapsed}
                  hasLive={sectionLiveSet.has(item.leagueId)}
                  onToggle={() => toggleLeague(item.leagueId)}
                />
                {!isCollapsed
                  ? item.fixtures.map((fg, idx) => (
                      <View key={fg.fixtureId}>
                        {idx > 0 ? (
                          <View
                            style={[
                              styles.fixtureSeparator,
                              { backgroundColor: c.borderSoft },
                            ]}
                          />
                        ) : null}
                        <RateMatchCard
                          fixtureId={fg.fixtureId}
                          fixture={fixtureLookup.get(fg.fixtureId)}
                          signals={fg.signals}
                          marketLookup={marketLookup}
                          primaryMetric="winning_percent"
                          gaugeColor={DSO_COLOR}
                        />
                      </View>
                    ))
                  : null}
              </View>
            );
          }}
          contentContainerStyle={styles.list}
          ListEmptyComponent={
            <View style={styles.center}>
              {searchActive ? (
                <>
                  <View
                    style={[
                      styles.emptyIconCircle,
                      { backgroundColor: c.brandSoft },
                    ]}>
                    <MaterialCommunityIcons
                      name="magnify-close"
                      size={28}
                      color={c.brand}
                    />
                  </View>
                  <ThemedText style={[styles.errorTitle, { color: c.text }]}>
                    {t('home.empty.searchTitle')}
                  </ThemedText>
                  <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
                    {t('home.empty.searchBody')}
                  </ThemedText>
                </>
              ) : scopeActive ? (
                <>
                  <View
                    style={[
                      styles.emptyIconCircle,
                      { backgroundColor: c.brandSoft },
                    ]}>
                    <MaterialCommunityIcons
                      name="filter-variant"
                      size={28}
                      color={c.brand}
                    />
                  </View>
                  <ThemedText style={[styles.errorTitle, { color: c.text }]}>
                    {t('leagueScope.emptyTitle')}
                  </ThemedText>
                  <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
                    {t('leagueScope.emptyBody')}
                  </ThemedText>
                  <View style={styles.emptyCtaRow}>
                    <Pressable
                      onPress={() => setScopeSheetOpen(true)}
                      style={[styles.emptyCtaPrimary, { backgroundColor: c.brand }]}>
                      <ThemedText style={[styles.emptyCtaText, { color: c.textInverse }]}>
                        {t('leagueScope.emptyEdit')}
                      </ThemedText>
                    </Pressable>
                    <Pressable
                      onPress={() => setScopedLeagueIds(new Set())}
                      style={[styles.emptyCtaSecondary, { borderColor: c.border }]}>
                      <ThemedText style={[styles.emptyCtaText, { color: c.text }]}>
                        {t('leagueScope.emptyClear')}
                      </ThemedText>
                    </Pressable>
                  </View>
                </>
              ) : activeCount > 0 ? (
                // Rule E: instead of a flat "no results" message, point at
                // the most likely cause given the current filter shape and
                // offer a one-tap path back to a usable state.
                <>
                  <View
                    style={[
                      styles.emptyIconCircle,
                      { backgroundColor: c.brandSoft },
                    ]}>
                    <MaterialCommunityIcons
                      name="filter-variant"
                      size={28}
                      color={c.brand}
                    />
                  </View>
                  <ThemedText style={[styles.errorTitle, { color: c.text }]}>
                    {t('analysis.empty.filteredTitle')}
                  </ThemedText>
                  <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
                    {suggestLoosen(filters, t)}
                  </ThemedText>
                  <View style={styles.emptyCtaRow}>
                    <Pressable
                      onPress={() => setFiltersOpen(true)}
                      style={[styles.emptyCtaPrimary, { backgroundColor: c.brand }]}>
                      <ThemedText style={[styles.emptyCtaText, { color: c.textInverse }]}>
                        {t('analysis.empty.openFilters')}
                      </ThemedText>
                    </Pressable>
                    <Pressable
                      onPress={() => setFilters(DEFAULT_FILTERS)}
                      style={[styles.emptyCtaSecondary, { borderColor: c.border }]}>
                      <ThemedText style={[styles.emptyCtaText, { color: c.text }]}>
                        {t('analysis.empty.resetFilters')}
                      </ThemedText>
                    </Pressable>
                  </View>
                </>
              ) : (
                <ThemedText style={{ color: c.textMuted }}>
                  {t('rate.noResults')}
                </ThemedText>
              )}
            </View>
          }
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={onRefresh}
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

      <AnalysisQuickPicksSheet
        visible={quickPicksOpen}
        onClose={() => setQuickPicksOpen(false)}
        selectedDate={selectedDate}
        bookmakerId={BOOKMAKER_ID}
      />

      <LeagueScopeSheet
        visible={scopeSheetOpen}
        onClose={() => setScopeSheetOpen(false)}
        rows={scopePickerRows}
        selectedLeagueIds={scopedLeagueIds}
        onChange={setScopedLeagueIds}
      />
    </SafeAreaView>
  );
}

// Rule E: choose the loosen-this-knob hint that's most likely to unblock
// the user. Picks the most aggressive constraint relative to defaults so
// the message is actionable rather than generic.
function suggestLoosen(
  filters: AnalysisFilterState,
  t: (key: string, opts?: Record<string, unknown>) => string,
): string {
  if (filters.dsoMin >= 70) return t('analysis.empty.suggest.dsoHigh');
  if (filters.vbetMin >= 50) return t('analysis.empty.suggest.vbetHigh');
  if (filters.kzMin >= 6) return t('analysis.empty.suggest.kzHigh');
  if (filters.window === '1m' || filters.window === '3m')
    return t('analysis.empty.suggest.windowNarrow');
  if (filters.valueOnly && filters.riskCategory != null)
    return t('analysis.empty.suggest.valuePlusRisk');
  return t('analysis.empty.suggest.generic');
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  // Same layout as the home page so the screens feel like siblings: brand
  // centered, magnify button corner-anchored.
  headerRow: {
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 4,
    justifyContent: 'center',
  },
  headerSearchBtn: {
    position: 'absolute',
    right: 12,
    top: 6,
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  // Mirror of headerSearchBtn pinned to the left — the flash trigger
  // for "Günün Önerileri" sits opposite the search button so the
  // header keeps its brand-centred symmetry. Sharing the absolute-right
  // style was a bug (both icons stacked on top of each other).
  headerFlashBtn: {
    position: 'absolute',
    left: 12,
    top: 6,
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  searchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginHorizontal: 16,
    marginTop: 4,
    marginBottom: 4,
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
  },
  searchInput: {
    flex: 1,
    fontSize: 14,
    paddingVertical: 0,
  },
  controlRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 8,
    gap: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  spacer: {
    flex: 1,
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
  metaRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 6,
  },
  // Tappable wrapper around the "N lig · M maç" summary so the user can
  // open the league-scope sheet from the text itself. When a scope is
  // active the pill picks up brand-soft fill so it doubles as the "filter
  // is on" indicator, with an inline X to clear in one tap.
  summaryBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
  },
  metaText: {
    fontSize: 11,
    fontWeight: '600',
    letterSpacing: 0.3,
  },
  toggleAllBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
  },
  toggleAllText: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
  list: {
    paddingHorizontal: 12,
    paddingTop: 4,
    paddingBottom: 32,
    gap: 12,
    flexGrow: 1,
  },
  sectionCard: {
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
    // RateMatchCard has marginTop:12 so cards inside stack with a 12px
    // gap between them. The last card has no bottom margin of its own —
    // mirror the inter-card spacing as section padding so the section
    // closes with the same breathing room cards have between each other.
    paddingBottom: 12,
  },
  fixtureSeparator: {
    height: StyleSheet.hairlineWidth,
    marginLeft: 16,
  },
  footerLoading: {
    paddingVertical: 16,
    alignItems: 'center',
    justifyContent: 'center',
  },
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 32,
    gap: 8,
  },
  emptyIconCircle: {
    width: 64,
    height: 64,
    borderRadius: 32,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 4,
  },
  errorTitle: {
    fontSize: 16,
    fontWeight: '700',
  },
  errorMessage: {
    fontSize: 13,
    textAlign: 'center',
    fontWeight: '500',
  },
  emptyCtaRow: {
    flexDirection: 'row',
    gap: 8,
    marginTop: 12,
  },
  emptyCtaPrimary: {
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderRadius: 10,
  },
  emptyCtaSecondary: {
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
  },
  emptyCtaText: {
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
});
