import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  FlatList,
  PanResponder,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  TextInput,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { AppBrand } from '@/src/components/AppBrand';
import { DateBar } from '@/src/components/DateBar';
import { FixtureCard } from '@/src/components/FixtureCard';
import { FixturePeekOverlay } from '@/src/components/FixturePeekOverlay';
import { LeagueHeader } from '@/src/components/LeagueHeader';
import { LeagueSectionSkeleton } from '@/src/components/Skeleton';
import { StateFilterBar, type FixtureFilter } from '@/src/components/StateFilterBar';
import { useCountryLookup } from '@/src/hooks/useCountryLookup';
import { useFixtures } from '@/src/hooks/useFixtures';
import { useLeagueLookup } from '@/src/hooks/useLeagueLookup';
import { useLiveTicker } from '@/src/hooks/useLiveTicker';
import { getStateBucket } from '@/src/lib/fixtureState';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureSummary } from '@/src/types/fixture';

const LIVE_REFETCH_MS = 30 * 1000;

interface Section {
  leagueId: number;
  data: FixtureSummary[];
}

// Turkish-aware lowercasing so "İstanbul" / "ISTANBUL" / "istanbul" all match
// when the user types in any case. Default toLowerCase folds İ→i̇ on Latin
// locales, missing matches that toLocaleLowerCase('tr-TR') would catch.
const normalizeForSearch = (value: string | null | undefined): string =>
  (value ?? '').toLocaleLowerCase('tr-TR');

// Same horizontal-swipe thresholds used in the fixture detail screen so
// the gesture feels uniform across the app.
const FILTER_ORDER: FixtureFilter[] = ['all', 'live', 'upcoming', 'finished'];
const SWIPE_DOMINANCE = 1.5;
const SWIPE_TRIGGER_DISTANCE = 50;
const SWIPE_RECOGNITION_THRESHOLD = 12;

export function TodayMatchesScreen() {
  const c = useTheme();
  const { t } = useTranslation();
  const [selectedDate, setSelectedDate] = useState(() => new Date());
  const [filter, setFilter] = useState<FixtureFilter>('all');
  const [searchOpen, setSearchOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const searchInputRef = useRef<TextInput | null>(null);
  const isoDate = format(selectedDate, 'yyyy-MM-dd');

  // Long-press peek state. Two phases:
  //   1. Press-and-hold (under 2s) → onPressOut closes the overlay.
  //   2. Held past 2s → peek locks open; user must tap the X to dismiss.
  // The lock timer is cancelled on early release; if it fires we leave the
  // overlay up regardless of finger state.
  const [peekFixture, setPeekFixture] = useState<FixtureSummary | null>(null);
  const [peekLocked, setPeekLocked] = useState(false);
  const lockTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const handlePeekStart = useCallback((f: FixtureSummary) => {
    setPeekFixture(f);
    setPeekLocked(false);
    if (lockTimerRef.current) clearTimeout(lockTimerRef.current);
    lockTimerRef.current = setTimeout(() => {
      setPeekLocked(true);
      lockTimerRef.current = null;
    }, 2000);
  }, []);
  const handlePeekEnd = useCallback(() => {
    // Timer still pending → lock hadn't triggered → user-released early →
    // close. If the timer already fired, lockTimerRef is null and we leave
    // the overlay up so the X button stays the only exit.
    if (lockTimerRef.current) {
      clearTimeout(lockTimerRef.current);
      lockTimerRef.current = null;
      setPeekFixture(null);
    }
  }, []);
  const handlePeekClose = useCallback(() => {
    if (lockTimerRef.current) {
      clearTimeout(lockTimerRef.current);
      lockTimerRef.current = null;
    }
    setPeekFixture(null);
    setPeekLocked(false);
  }, []);
  useEffect(() => {
    return () => {
      if (lockTimerRef.current) clearTimeout(lockTimerRef.current);
    };
  }, []);

  // Horizontal-swipe-to-switch state filter. Same closure-via-ref trick the
  // fixture detail screen uses so the responder stays stable while still
  // reading the latest filter on each gesture release.
  const filterRef = useRef<FixtureFilter>(filter);
  useEffect(() => {
    filterRef.current = filter;
  }, [filter]);
  const filterSwipeResponder = useRef(
    PanResponder.create({
      onMoveShouldSetPanResponder: (_, g) =>
        Math.abs(g.dx) > Math.abs(g.dy) * SWIPE_DOMINANCE &&
        Math.abs(g.dx) > SWIPE_RECOGNITION_THRESHOLD,
      onPanResponderRelease: (_, g) => {
        if (Math.abs(g.dx) < SWIPE_TRIGGER_DISTANCE) return;
        const idx = FILTER_ORDER.indexOf(filterRef.current);
        if (idx < 0) return;
        if (g.dx > 0 && idx > 0) {
          setFilter(FILTER_ORDER[idx - 1]);
        } else if (g.dx < 0 && idx < FILTER_ORDER.length - 1) {
          setFilter(FILTER_ORDER[idx + 1]);
        }
      },
    }),
  ).current;

  const handleToggleSearch = useCallback(() => {
    setSearchOpen((prev) => {
      const next = !prev;
      if (!next) setSearchQuery('');
      else setTimeout(() => searchInputRef.current?.focus(), 0);
      return next;
    });
  }, []);

  // Memoized lowercase trim — also drives whether the search filter
  // is "active" (we skip the per-section pass when the query is empty).
  const normalizedQuery = useMemo(
    () => normalizeForSearch(searchQuery.trim()),
    [searchQuery],
  );
  const searchActive = normalizedQuery.length > 0;

  const queryClient = useQueryClient();
  const { data, isLoading, isFetching, isError, error, refetch } = useFixtures(
    { date: isoDate, perPage: 200 },
  );
  const hasLive = useMemo(
    () => (data?.items ?? []).some((f) => getStateBucket(f.state_id) === 'live'),
    [data?.items],
  );
  // Poll while any match is in-play so the live minute ticks up even if
  // SignalR is unavailable (e.g. on the web build where browser CORS can
  // block the negotiate).
  useEffect(() => {
    if (!hasLive) return undefined;
    const id = setInterval(() => {
      queryClient.invalidateQueries({ queryKey: ['fixtures'] });
    }, LIVE_REFETCH_MS);
    return () => clearInterval(id);
  }, [hasLive, queryClient]);

  // SignalR live-ticker keeps the home list fresh without polling: any
  // fixture upsert in the worker's Live tier marks ['fixtures'] stale,
  // and the active screen refetches automatically.
  useLiveTicker();

  const fixtures = data?.items ?? [];

  const filtered = useMemo(() => {
    if (filter === 'all') return fixtures;
    return fixtures.filter((f) => getStateBucket(f.state_id) === filter);
  }, [fixtures, filter]);

  const counts = useMemo<Record<FixtureFilter, number>>(() => {
    const acc = { all: fixtures.length, live: 0, upcoming: 0, finished: 0 };
    for (const f of fixtures) {
      const bucket = getStateBucket(f.state_id);
      if (bucket === 'live') acc.live++;
      else if (bucket === 'upcoming') acc.upcoming++;
      else if (bucket === 'finished') acc.finished++;
    }
    return acc;
  }, [fixtures]);

  const sections = useMemo<Section[]>(() => {
    // Dedupe by fixture id. The backend occasionally emits the same
    // fixture in two pages of /fixtures (when its `last_synced_at`
    // straddles a page boundary mid-sync); React's reconciler crashes
    // with a duplicate-key warning if we let both rows render side by
    // side inside the same league section.
    const seen = new Set<number>();
    const groups = new Map<number, FixtureSummary[]>();
    for (const f of filtered) {
      if (seen.has(f.id)) continue;
      seen.add(f.id);
      const list = groups.get(f.league_id);
      if (list) list.push(f);
      else groups.set(f.league_id, [f]);
    }
    for (const list of groups.values()) {
      list.sort((a, b) => {
        const ta = a.starting_at ?? '';
        const tb = b.starting_at ?? '';
        return ta.localeCompare(tb);
      });
    }
    return Array.from(groups.entries()).map(([leagueId, data]) => ({
      leagueId,
      data,
    }));
  }, [filtered]);

  const leagueIds = useMemo(() => sections.map((s) => s.leagueId), [sections]);
  const { lookup: leagueLookup } = useLeagueLookup(leagueIds);

  const countryIds = useMemo(
    () => Array.from(leagueLookup.values()).map((l) => l.country_id),
    [leagueLookup],
  );
  const { lookup: countryLookup } = useCountryLookup(countryIds);

  const sortedSections = useMemo(() => {
    return [...sections].sort((a, b) => {
      const la = leagueLookup.get(a.leagueId);
      const lb = leagueLookup.get(b.leagueId);
      const ca = la?.category ?? Number.MAX_SAFE_INTEGER;
      const cb = lb?.category ?? Number.MAX_SAFE_INTEGER;
      if (ca !== cb) return ca - cb;
      const na = la?.name ?? `Z${a.leagueId}`;
      const nb = lb?.name ?? `Z${b.leagueId}`;
      return na.localeCompare(nb);
    });
  }, [sections, leagueLookup]);

  // Search across team, league, and country names. A league-name hit keeps
  // every fixture in that section visible (browsing by league); a team-name
  // hit narrows to just matching fixtures within their league header.
  const visibleSections = useMemo<Section[]>(() => {
    if (!searchActive) return sortedSections;
    const out: Section[] = [];
    for (const section of sortedSections) {
      const league = leagueLookup.get(section.leagueId);
      const country =
        league?.country_id != null
          ? countryLookup.get(league.country_id)
          : undefined;
      const leagueHit =
        normalizeForSearch(league?.name).includes(normalizedQuery) ||
        normalizeForSearch(country?.name).includes(normalizedQuery);
      if (leagueHit) {
        out.push(section);
        continue;
      }
      const matches = section.data.filter(
        (f) =>
          normalizeForSearch(f.home_team_name).includes(normalizedQuery) ||
          normalizeForSearch(f.away_team_name).includes(normalizedQuery),
      );
      if (matches.length > 0) {
        out.push({ leagueId: section.leagueId, data: matches });
      }
    }
    return out;
  }, [sortedSections, leagueLookup, countryLookup, normalizedQuery, searchActive]);

  // Collapsed state per league. Persisted only in component state — fresh
  // on every screen mount so a busy day starts expanded.
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
    visibleSections.length > 0 &&
    visibleSections.every((s) => collapsed.has(s.leagueId));
  const toggleAll = useCallback(() => {
    setCollapsed((prev) => {
      if (visibleSections.every((s) => prev.has(s.leagueId))) {
        return new Set();
      }
      return new Set(visibleSections.map((s) => s.leagueId));
    });
  }, [visibleSections]);

  // Section headers can show a "live" pip when at least one fixture is in
  // play. Pre-compute so the renderer doesn't re-scan per row.
  const sectionLiveSet = useMemo(() => {
    const out = new Set<number>();
    for (const s of visibleSections) {
      if (s.data.some((f) => getStateBucket(f.state_id) === 'live')) {
        out.add(s.leagueId);
      }
    }
    return out;
  }, [visibleSections]);

  // Visible match total for the meta line — counts across whatever survives
  // the active state + search filters.
  const visibleMatchCount = useMemo(
    () => visibleSections.reduce((acc, s) => acc + s.data.length, 0),
    [visibleSections],
  );

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.headerRow}>
        <AppBrand />
        <Pressable
          onPress={handleToggleSearch}
          hitSlop={12}
          accessibilityRole="button"
          accessibilityLabel={searchOpen ? t('home.search.a11yClose') : t('home.search.a11yOpen')}
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

      <StateFilterBar selected={filter} onSelect={setFilter} counts={counts} />

      {visibleSections.length > 0 ? (
        <View style={styles.metaRow}>
          <ThemedText style={[styles.metaText, { color: c.textMuted }]}>
            {t('home.meta.summary', {
              leagues: visibleSections.length,
              matches: visibleMatchCount,
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

      <View style={styles.flex} {...filterSwipeResponder.panHandlers}>
      {isLoading ? (
        <ScrollView contentContainerStyle={styles.list}>
          <LeagueSectionSkeleton rows={4} />
          <LeagueSectionSkeleton rows={3} />
          <LeagueSectionSkeleton rows={2} />
        </ScrollView>
      ) : isError ? (
        <View style={styles.center}>
          <View style={[styles.errorIconCircle, { backgroundColor: c.dangerSoft }]}>
            <MaterialCommunityIcons
              name="alert-circle-outline"
              size={28}
              color={c.danger}
            />
          </View>
          <ThemedText style={[styles.errorTitle, { color: c.text }]}>
            {t('home.error.title')}
          </ThemedText>
          <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
            {error instanceof Error ? error.message : t('home.error.unknown')}
          </ThemedText>
        </View>
      ) : (
        <FlatList
          data={visibleSections}
          keyExtractor={(s) => String(s.leagueId)}
          keyboardShouldPersistTaps="handled"
          renderItem={({ item }) => {
            const league = leagueLookup.get(item.leagueId);
            const country =
              league?.country_id != null
                ? countryLookup.get(league.country_id)
                : undefined;
            // Auto-expand sections while a search is active so the user
            // sees their hits without first tapping the league header.
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
                  fixtureCount={item.data.length}
                  collapsed={isCollapsed}
                  hasLive={sectionLiveSet.has(item.leagueId)}
                  onToggle={() => toggleLeague(item.leagueId)}
                />
                {!isCollapsed
                  ? item.data.map((fixture, idx) => (
                      <View key={fixture.id}>
                        {idx > 0 ? (
                          <View
                            style={[
                              styles.fixtureSeparator,
                              { backgroundColor: c.borderSoft },
                            ]}
                          />
                        ) : null}
                        <FixtureCard
                          fixture={fixture}
                          onLongPress={handlePeekStart}
                          onPressOut={handlePeekEnd}
                        />
                      </View>
                    ))
                  : null}
              </View>
            );
          }}
          contentContainerStyle={styles.list}
          refreshControl={
            <RefreshControl
              refreshing={isFetching}
              onRefresh={refetch}
              tintColor={c.brand}
            />
          }
          ListEmptyComponent={
            <View style={styles.center}>
              <View
                style={[styles.errorIconCircle, { backgroundColor: c.brandSoft }]}>
                <MaterialCommunityIcons
                  name={searchActive ? 'magnify-close' : 'calendar-blank-outline'}
                  size={28}
                  color={c.brand}
                />
              </View>
              <ThemedText style={[styles.errorTitle, { color: c.text }]}>
                {searchActive
                  ? t('home.empty.searchTitle')
                  : t('home.empty.filterTitle')}
              </ThemedText>
              <ThemedText
                style={[styles.errorMessage, { color: c.textMuted }]}>
                {searchActive
                  ? t('home.empty.searchBody')
                  : t('home.empty.filterBody')}
              </ThemedText>
            </View>
          }
        />
      )}
      </View>
      <FixturePeekOverlay
        fixture={peekFixture}
        locked={peekLocked}
        onClose={handlePeekClose}
        onChangeFixture={(next) => setPeekFixture(next)}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  // Header has the centered AppBrand AND a corner search button. Brand
  // stays optically centered (it fills the row) while the button sits
  // absolutely on the right so it doesn't push the logo off-center.
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
  },
  // Indented hairline between consecutive matches in a section. The 64px
  // left inset clears the time column so the line starts under the team
  // names — feels like a gentle row separator rather than a hard divider.
  fixtureSeparator: {
    height: StyleSheet.hairlineWidth,
    marginLeft: 64,
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
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 32,
    gap: 10,
  },
  errorIconCircle: {
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
});
