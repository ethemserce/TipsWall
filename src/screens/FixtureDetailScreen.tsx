import { useMemo, useState } from 'react';
import {
  ActivityIndicator,
  RefreshControl,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { DetailTabBar, type DetailTab } from '@/src/components/DetailTabBar';
import { FixtureDetailHero } from '@/src/components/FixtureDetailHero';
import { MatchInfoCard } from '@/src/components/MatchInfoCard';
import { OddsRatesCard } from '@/src/components/OddsRatesCard';
import { ScoreBreakdown } from '@/src/components/ScoreBreakdown';
import { useCountryLookup } from '@/src/hooks/useCountryLookup';
import { useFixture } from '@/src/hooks/useFixture';
import { useFixtureOddsRates } from '@/src/hooks/useFixtureOddsRates';
import { useLeagueLookup } from '@/src/hooks/useLeagueLookup';
import { useTheme } from '@/src/lib/useTheme';

const ODDS_BOOKMAKER_ID = 1;
const ODDS_MARKET_IDS = [1, 52, 80, 31];

interface FixtureDetailScreenProps {
  fixtureId: number;
}

export function FixtureDetailScreen({ fixtureId }: FixtureDetailScreenProps) {
  const c = useTheme();
  const [tab, setTab] = useState<DetailTab>('details');
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

  const oddsRates = useFixtureOddsRates({
    fixtureId,
    bookmakerId: ODDS_BOOKMAKER_ID,
    marketIds: ODDS_MARKET_IDS,
  });

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
      <FixtureDetailHero
        fixture={data.fixture}
        league={league}
        country={country}
      />
      <DetailTabBar selected={tab} onSelect={setTab} />

      {tab === 'details' ? (
        <>
          <MatchInfoCard
            fixture={data.fixture}
            league={league}
            country={country}
          />
          <ScoreBreakdown
            scores={data.scores}
            homeName={data.fixture.home_team_short_code ?? data.fixture.home_team_name}
            awayName={data.fixture.away_team_short_code ?? data.fixture.away_team_name}
          />
          {oddsRates.data?.map((market) => (
            <OddsRatesCard key={market.market_id} market={market} />
          ))}
        </>
      ) : (
        <EmptyTab label={tab} />
      )}
    </ScrollView>
  );
}

function EmptyTab({ label }: { label: DetailTab }) {
  const c = useTheme();
  return (
    <View style={styles.empty}>
      <ThemedText style={[styles.emptyText, { color: c.textMuted }]}>
        {prettyTabName(label)} coming soon.
      </ThemedText>
    </View>
  );
}

function prettyTabName(tab: DetailTab): string {
  switch (tab) {
    case 'stats':
      return 'Statistics';
    case 'lineups':
      return 'Lineups';
    case 'h2h':
      return 'Head-to-head';
    default:
      return 'Details';
  }
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
  empty: {
    paddingVertical: 64,
    alignItems: 'center',
  },
  emptyText: {
    fontSize: 14,
  },
});
