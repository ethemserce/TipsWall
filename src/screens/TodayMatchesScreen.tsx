import { format } from 'date-fns';
import { useMemo, useState } from 'react';
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
  const [oddsToggle, setOddsToggle] = useState(false);
  const isoDate = format(selectedDate, 'yyyy-MM-dd');

  const { data, isLoading, isFetching, isError, error, refetch } = useFixtures(
    { date: isoDate, perPage: 200 },
    { refetchIntervalMs: filter === 'live' ? LIVE_REFETCH_MS : undefined },
  );

  // SignalR live-ticker keeps the home list fresh without polling: any
  // fixture upsert in the worker's Live tier marks ['fixtures'] stale,
  // and the active screen refetches automatically.
  useLiveTicker();

  const fixtures = data?.items ?? [];

  const filtered = useMemo(() => {
    let list = fixtures;
    if (oddsToggle) list = list.filter((f) => f.has_odds);
    if (filter !== 'all') {
      list = list.filter((f) => getStateBucket(f.state_id) === filter);
    }
    return list;
  }, [fixtures, filter, oddsToggle]);

  const counts = useMemo<Record<FixtureFilter, number>>(() => {
    const acc = { all: fixtures.length, live: 0, upcoming: 0, finished: 0 };
    const pool = oddsToggle ? fixtures.filter((f) => f.has_odds) : fixtures;
    acc.all = pool.length;
    for (const f of pool) {
      const bucket = getStateBucket(f.state_id);
      if (bucket === 'live') acc.live++;
      else if (bucket === 'upcoming') acc.upcoming++;
      else if (bucket === 'finished') acc.finished++;
    }
    return acc;
  }, [fixtures, oddsToggle]);

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

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.headerRow}>
        <AppBrand />
      </View>

      <DateBar
        selectedDate={selectedDate}
        onSelect={setSelectedDate}
        oddsToggle={oddsToggle}
        onOddsToggle={setOddsToggle}
      />

      <StateFilterBar selected={filter} onSelect={setFilter} counts={counts} />

      {isLoading ? (
        <View style={styles.center}>
          <ActivityIndicator color={c.brand} />
        </View>
      ) : isError ? (
        <View style={styles.center}>
          <ThemedText style={[styles.errorTitle, { color: c.text }]}>
            Couldn&apos;t load fixtures
          </ThemedText>
          <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
            {error instanceof Error ? error.message : 'Unknown error'}
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
            return (
              <View
                style={[
                  styles.sectionCard,
                  { backgroundColor: c.surface, borderColor: c.border },
                ]}>
                <LeagueHeader
                  leagueId={item.leagueId}
                  league={league}
                  country={country}
                  fixtureCount={item.data.length}
                />
                {item.data.map((fixture) => (
                  <FixtureCard key={fixture.id} fixture={fixture} />
                ))}
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
              <ThemedText style={{ color: c.textMuted }}>
                No fixtures match this filter.
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
    paddingTop: 8,
    paddingBottom: 32,
    gap: 12,
    flexGrow: 1,
  },
  sectionCard: {
    borderRadius: 12,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
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
