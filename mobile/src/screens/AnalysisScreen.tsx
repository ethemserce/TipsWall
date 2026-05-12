import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { format } from 'date-fns';
import { useCallback, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
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
import { AppBrand } from '@/src/components/AppBrand';
import { DateBar } from '@/src/components/DateBar';
import { LeagueHeader } from '@/src/components/LeagueHeader';
import { MarketLegendButton } from '@/src/components/MarketLegendButton';
import { RateMatchCard } from '@/src/components/RateMatchCard';
import { LeagueSectionSkeleton } from '@/src/components/Skeleton';
import { useCountryLookup } from '@/src/hooks/useCountryLookup';
import { useFixtureLookup } from '@/src/hooks/useFixtureLookup';
import { useLeagueLookup } from '@/src/hooks/useLeagueLookup';
import { useMarkets } from '@/src/hooks/useMarkets';
import { useSignals } from '@/src/hooks/useSignals';
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
  const [selectedDate, setSelectedDate] = useState(() => new Date());
  const [filters, setFilters] = useState<AnalysisFilterState>(DEFAULT_FILTERS);
  const [filtersOpen, setFiltersOpen] = useState(false);
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
  const { data, isLoading, isFetching, isError, error, refetch } = useSignals({
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
    perPage: 100,
  });

  const { lookup: marketLookup } = useMarkets();
  const items = data?.data.items ?? [];

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

  // Group fixtures by league, ordered like the home list (by league
  // category first, then name).
  const leagueGroups = useMemo<LeagueGroup[]>(() => {
    const map = new Map<number, FixtureGroup[]>();
    const orphans: FixtureGroup[] = [];
    for (const fg of filteredFixtureGroups) {
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
  }, [filteredFixtureGroups, fixtureLookup, leagueLookup]);

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
  const toggleAll = useCallback(() => {
    setCollapsed((prev) => {
      if (leagueGroups.every((g) => prev.has(g.leagueId))) {
        return new Set();
      }
      return new Set(leagueGroups.map((g) => g.leagueId));
    });
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

      {leagueGroups.length > 0 ? (
        <View style={styles.metaRow}>
          <ThemedText style={[styles.metaText, { color: c.textMuted }]}>
            {t('home.meta.summary', {
              leagues: leagueGroups.length,
              matches: visibleFixtureCount,
            })}
          </ThemedText>
          <Pressable
            onPress={toggleAll}
            hitSlop={6}
            style={({ pressed }) => [
              styles.toggleAllBtn,
              {
                backgroundColor: pressed ? c.brandSoft : 'transparent',
                borderColor: c.borderSoft,
              },
            ]}>
            <MaterialCommunityIcons
              name={allCollapsed ? 'unfold-more-horizontal' : 'unfold-less-horizontal'}
              size={14}
              color={c.brand}
            />
            <ThemedText style={[styles.toggleAllText, { color: c.brand }]}>
              {allCollapsed ? t('home.toggleAll.expand') : t('home.toggleAll.collapse')}
            </ThemedText>
          </Pressable>
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
