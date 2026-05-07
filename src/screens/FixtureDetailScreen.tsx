import { useMemo } from 'react';
import {
  ActivityIndicator,
  RefreshControl,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { FixtureDetailHero } from '@/src/components/FixtureDetailHero';
import { FixtureMetaRow } from '@/src/components/FixtureMetaRow';
import { ScoreBreakdown } from '@/src/components/ScoreBreakdown';
import { useCountryLookup } from '@/src/hooks/useCountryLookup';
import { useFixture } from '@/src/hooks/useFixture';
import { useLeagueLookup } from '@/src/hooks/useLeagueLookup';
import { useTheme } from '@/src/lib/useTheme';

interface FixtureDetailScreenProps {
  fixtureId: number;
}

export function FixtureDetailScreen({ fixtureId }: FixtureDetailScreenProps) {
  const c = useTheme();
  const { data, isLoading, isFetching, isError, error, refetch } =
    useFixture(fixtureId);

  const leagueIds = useMemo(
    () => (data?.fixture.league_id != null ? [data.fixture.league_id] : []),
    [data?.fixture.league_id],
  );
  const { lookup: leagueLookup } = useLeagueLookup(leagueIds);
  const league = data?.fixture.league_id
    ? leagueLookup.get(data.fixture.league_id)
    : undefined;

  const countryIds = useMemo(
    () => (league?.country_id != null ? [league.country_id] : []),
    [league?.country_id],
  );
  const { lookup: countryLookup } = useCountryLookup(countryIds);
  const country = league?.country_id
    ? countryLookup.get(league.country_id)
    : undefined;

  if (isLoading) {
    return (
      <View style={[styles.center, { backgroundColor: c.bg }]}>
        <ActivityIndicator color={c.brand} />
      </View>
    );
  }

  if (isError || !data) {
    return (
      <View style={[styles.center, { backgroundColor: c.bg }]}>
        <ThemedText style={[styles.errorTitle, { color: c.text }]}>
          Couldn&apos;t load fixture
        </ThemedText>
        <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
          {error instanceof Error ? error.message : 'Unknown error'}
        </ThemedText>
      </View>
    );
  }

  return (
    <ScrollView
      style={[styles.flex, { backgroundColor: c.bg }]}
      contentContainerStyle={styles.content}
      refreshControl={
        <RefreshControl
          refreshing={isFetching}
          onRefresh={refetch}
          tintColor={c.brand}
        />
      }>
      <FixtureDetailHero fixture={data.fixture} />
      <FixtureMetaRow
        fixture={data.fixture}
        league={league}
        country={country}
      />
      <ScoreBreakdown scores={data.scores} />
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  content: {
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
