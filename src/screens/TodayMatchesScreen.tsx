import { format } from 'date-fns';
import { useMemo, useState } from 'react';
import {
  ActivityIndicator,
  RefreshControl,
  SectionList,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { DateBar } from '@/src/components/DateBar';
import { FixtureCard } from '@/src/components/FixtureCard';
import { LeagueHeader } from '@/src/components/LeagueHeader';
import { StateFilterBar, type FixtureFilter } from '@/src/components/StateFilterBar';
import { useFixtures } from '@/src/hooks/useFixtures';
import { useLeagueLookup } from '@/src/hooks/useLeagueLookup';
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

  const { data, isLoading, isFetching, isError, error, refetch } = useFixtures(
    { date: isoDate, perPage: 200 },
    { refetchIntervalMs: filter === 'live' ? LIVE_REFETCH_MS : undefined },
  );

  const fixtures = data?.items ?? [];

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

  const filtered = useMemo(() => {
    if (filter === 'all') return fixtures;
    return fixtures.filter((f) => getStateBucket(f.state_id) === filter);
  }, [fixtures, filter]);

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

  const sortedSections = useMemo(() => {
    return [...sections].sort((a, b) => {
      const la = leagueLookup.get(a.leagueId)?.name ?? `Z${a.leagueId}`;
      const lb = leagueLookup.get(b.leagueId)?.name ?? `Z${b.leagueId}`;
      return la.localeCompare(lb);
    });
  }, [sections, leagueLookup]);

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.header}>
        <ThemedText style={[styles.title, { color: c.text }]}>Matches</ThemedText>
        <ThemedText style={[styles.subtitle, { color: c.textMuted }]}>
          {format(selectedDate, 'EEEE, d MMMM yyyy')}
        </ThemedText>
      </View>

      <DateBar selectedDate={selectedDate} onSelect={setSelectedDate} />
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
        <SectionList
          sections={sortedSections}
          keyExtractor={(item) => String(item.id)}
          renderItem={({ item }) => <FixtureCard fixture={item} />}
          renderSectionHeader={({ section }) => (
            <LeagueHeader
              leagueId={section.leagueId}
              league={leagueLookup.get(section.leagueId)}
              fixtureCount={section.data.length}
            />
          )}
          stickySectionHeadersEnabled
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
  header: {
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 4,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
  },
  subtitle: {
    fontSize: 13,
    marginTop: 2,
  },
  list: {
    paddingBottom: 32,
    flexGrow: 1,
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
