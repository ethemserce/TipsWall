import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  RefreshControl,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { DetailTabBar, type DetailTab } from '@/src/components/DetailTabBar';
import { EventTimelineCard } from '@/src/components/EventTimelineCard';
import { FixtureDetailHero } from '@/src/components/FixtureDetailHero';
import { H2HTab } from '@/src/components/H2HTab';
import { LineupsTab } from '@/src/components/LineupsTab';
import { MatchInfoCard } from '@/src/components/MatchInfoCard';
import { OddsRatesCard } from '@/src/components/OddsRatesCard';
import { StatsTab } from '@/src/components/StatsTab';
import { TabError, TabLoading, TabEmpty } from '@/src/components/TabFeedback';
import { useCountryLookup } from '@/src/hooks/useCountryLookup';
import { useFixture } from '@/src/hooks/useFixture';
import {
  useFixtureEvents,
  useFixtureH2H,
  useFixtureLineups,
  useFixtureStatistics,
} from '@/src/hooks/useFixtureExtras';
import { useFixtureOddsRates } from '@/src/hooks/useFixtureOddsRates';
import { useLeagueLookup } from '@/src/hooks/useLeagueLookup';
import { useLiveFixture } from '@/src/hooks/useLiveFixture';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureOddsMarket } from '@/src/types/fixtureOdds';

const ODDS_BOOKMAKER_ID = 2;
const ODDS_MARKET_IDS = [1, 52, 80, 31];

interface FixtureDetailScreenProps {
  fixtureId: number;
}

export function FixtureDetailScreen({ fixtureId }: FixtureDetailScreenProps) {
  const c = useTheme();
  const { t } = useTranslation();
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

  // SignalR live updates: joins the fixture group and invalidates query
  // caches whenever the backend pushes a FixtureUpdated event.
  useLiveFixture(fixtureId);

  // Hero shows scorer summary so we always need events.
  const events = useFixtureEvents(fixtureId);
  const oddsRates = useFixtureOddsRates({
    fixtureId,
    bookmakerId: ODDS_BOOKMAKER_ID,
    marketIds: ODDS_MARKET_IDS,
  });
  const stats = useFixtureStatistics(fixtureId, tab === 'stats');
  const lineups = useFixtureLineups(fixtureId, tab === 'lineups');
  const h2h = useFixtureH2H(fixtureId, 10, tab === 'h2h');

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
          {t('common.couldNotLoad')}
        </ThemedText>
        <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
          {error instanceof Error ? error.message : t('common.somethingWentWrong')}
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
        scores={data.scores}
        events={events.data}
      />
      <DetailTabBar selected={tab} onSelect={setTab} />

      {tab === 'details' ? (
        <>
          {events.data && events.data.length > 0 ? (
            <EventTimelineCard events={events.data} />
          ) : null}
          <MatchInfoCard
            fixture={data.fixture}
            league={league}
            country={country}
          />
        </>
      ) : tab === 'odds' ? (
        <OddsTabContent
          loading={oddsRates.isLoading}
          error={oddsRates.error}
          markets={oddsRates.data ?? []}
        />
      ) : tab === 'stats' ? (
        <StatsTab
          loading={stats.isLoading}
          error={stats.error}
          stats={stats.data ?? []}
        />
      ) : tab === 'lineups' ? (
        <LineupsTab
          loading={lineups.isLoading}
          error={lineups.error}
          lineups={lineups.data ?? null}
          events={events.data}
          homeName={data.fixture.home_team_name}
          awayName={data.fixture.away_team_name}
          homeImagePath={data.fixture.home_team_image_path}
          awayImagePath={data.fixture.away_team_image_path}
        />
      ) : tab === 'h2h' ? (
        <H2HTab
          loading={h2h.isLoading}
          error={h2h.error}
          fixtures={h2h.data ?? []}
          homeTeamId={data.fixture.home_team_id}
          awayTeamId={data.fixture.away_team_id}
        />
      ) : null}
    </ScrollView>
  );
}

function OddsTabContent({
  loading,
  error,
  markets,
}: {
  loading: boolean;
  error?: unknown;
  markets: FixtureOddsMarket[];
}) {
  const { t } = useTranslation();
  if (error && markets.length === 0) return <TabError error={error} />;
  if (loading && markets.length === 0) return <TabLoading />;
  if (markets.length === 0)
    return <TabEmpty message={t('fixture.odds.noOdds')} />;
  return (
    <>
      {markets.map((market) => (
        <OddsRatesCard key={market.market_id} market={market} />
      ))}
    </>
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
  empty: {
    paddingVertical: 64,
    alignItems: 'center',
  },
  emptyText: {
    fontSize: 14,
  },
});
