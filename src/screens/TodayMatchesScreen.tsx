import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useQueryClient } from '@tanstack/react-query';
import { format } from 'date-fns';
import { useCallback, useEffect, useMemo, useState } from 'react';
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
import { DateBar } from '@/src/components/DateBar';
import { FixtureCard } from '@/src/components/FixtureCard';
import { LeagueHeader } from '@/src/components/LeagueHeader';
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

export function TodayMatchesScreen() {
  const c = useTheme();
  const [selectedDate, setSelectedDate] = useState(() => new Date());
  const [filter, setFilter] = useState<FixtureFilter>('all');
  const isoDate = format(selectedDate, 'yyyy-MM-dd');

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
    const groups = new Map<number, FixtureSummary[]>();
    for (const f of filtered) {
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
    sortedSections.length > 0 &&
    sortedSections.every((s) => collapsed.has(s.leagueId));
  const toggleAll = useCallback(() => {
    setCollapsed((prev) => {
      if (sortedSections.every((s) => prev.has(s.leagueId))) {
        return new Set();
      }
      return new Set(sortedSections.map((s) => s.leagueId));
    });
  }, [sortedSections]);

  // Section headers can show a "live" pip when at least one fixture is in
  // play. Pre-compute so the renderer doesn't re-scan per row.
  const sectionLiveSet = useMemo(() => {
    const out = new Set<number>();
    for (const s of sortedSections) {
      if (s.data.some((f) => getStateBucket(f.state_id) === 'live')) {
        out.add(s.leagueId);
      }
    }
    return out;
  }, [sortedSections]);

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.headerRow}>
        <AppBrand />
      </View>

      <DateBar selectedDate={selectedDate} onSelect={setSelectedDate} />

      <StateFilterBar selected={filter} onSelect={setFilter} counts={counts} />

      {sortedSections.length > 0 ? (
        <View style={styles.metaRow}>
          <ThemedText style={[styles.metaText, { color: c.textMuted }]}>
            {sortedSections.length} lig · {filtered.length} maç
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
              {allCollapsed ? 'Hepsini aç' : 'Hepsini kapat'}
            </ThemedText>
          </Pressable>
        </View>
      ) : null}

      {isLoading ? (
        <View style={styles.center}>
          <ActivityIndicator color={c.brand} />
        </View>
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
            Maçlar yüklenemedi
          </ThemedText>
          <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
            {error instanceof Error ? error.message : 'Bilinmeyen hata'}
          </ThemedText>
        </View>
      ) : (
        <FlatList
          data={sortedSections}
          keyExtractor={(s) => String(s.leagueId)}
          renderItem={({ item }) => {
            const league = leagueLookup.get(item.leagueId);
            const country =
              league?.country_id != null
                ? countryLookup.get(league.country_id)
                : undefined;
            const isCollapsed = collapsed.has(item.leagueId);
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
                        <FixtureCard fixture={fixture} />
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
                  name="calendar-blank-outline"
                  size={28}
                  color={c.brand}
                />
              </View>
              <ThemedText style={[styles.errorTitle, { color: c.text }]}>
                Bu filtreye uygun maç yok
              </ThemedText>
              <ThemedText
                style={[styles.errorMessage, { color: c.textMuted }]}>
                Farklı bir tarih veya filtre deneyebilirsin.
              </ThemedText>
            </View>
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
