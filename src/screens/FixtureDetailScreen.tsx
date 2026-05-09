import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Image } from 'expo-image';
import { router } from 'expo-router';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { DetailTabBar, type DetailTab } from '@/src/components/DetailTabBar';
import { EventTimelineCard } from '@/src/components/EventTimelineCard';
import { MarketLegendButton } from '@/src/components/MarketLegendButton';
import { FixtureDetailHero } from '@/src/components/FixtureDetailHero';
import { FixtureTopPicksCard } from '@/src/components/FixtureTopPicksCard';
import { H2HTab } from '@/src/components/H2HTab';
import { LineupsTab } from '@/src/components/LineupsTab';
import { MatchInfoCard } from '@/src/components/MatchInfoCard';
import { OddsRatesCard } from '@/src/components/OddsRatesCard';
import { StandingsTab } from '@/src/components/StandingsTab';
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
import { shareFixture } from '@/src/lib/share';
import { useLeagueLookup } from '@/src/hooks/useLeagueLookup';
import { useLeagueTable } from '@/src/hooks/useLeagueTable';
import { useLiveFixture } from '@/src/hooks/useLiveFixture';
import { getStateBucket } from '@/src/lib/fixtureState';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureOddsMarket } from '@/src/types/fixtureOdds';

const ODDS_BOOKMAKER_ID = 2;
// Empty list = every market with has_winning_calculations = true (server side).
const ODDS_MARKET_IDS: number[] = [];

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
    window: 'all',
  });
  const stats = useFixtureStatistics(fixtureId, tab === 'stats');
  const lineups = useFixtureLineups(fixtureId, tab === 'lineups');
  const h2h = useFixtureH2H(fixtureId, 10, tab === 'h2h');
  const standings = useLeagueTable(
    data?.fixture.league_id,
    data?.fixture.season_id,
    tab === 'standings',
  );

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

  // Outer SafeAreaView + custom header bar. Avoids react-navigation Stack
  // history altogether — back button always lands on the home tab so the
  // user never gets stuck "behind" a previously-visited fixture.
  const handleBack = () => {
    if (router.canGoBack()) router.back();
    else router.replace('/');
  };

  return (
    <SafeAreaView
      style={[styles.flex, { backgroundColor: c.bg }]}
      edges={['top']}>
      <View style={[styles.headerBar, { borderBottomColor: c.border }]}>
        <Pressable
          onPress={handleBack}
          hitSlop={12}
          style={styles.headerBack}>
          <MaterialCommunityIcons
            name="chevron-left"
            size={24}
            color={c.text}
          />
        </Pressable>
        <View style={styles.headerTitle}>
          {league?.image_path ? (
            <Image
              source={{ uri: league.image_path }}
              style={styles.headerLogo}
              contentFit="contain"
            />
          ) : null}
          <ThemedText
            style={[styles.headerName, { color: c.text }]}
            numberOfLines={1}>
            {league?.name ?? ''}
          </ThemedText>
        </View>
        <View style={styles.headerActions}>
          <Pressable
            onPress={() => {
              const fxName =
                data.fixture.home_team_name && data.fixture.away_team_name
                  ? `${data.fixture.home_team_name} - ${data.fixture.away_team_name}`
                  : `Maç #${fixtureId}`;
              shareFixture(fixtureId, fxName);
            }}
            hitSlop={12}
            accessibilityRole="button"
            accessibilityLabel="Maçı paylaş"
            style={({ pressed }) => [
              styles.headerIconBtn,
              pressed && { backgroundColor: c.brandSoft },
            ]}>
            <MaterialCommunityIcons
              name="share-variant"
              size={20}
              color={c.textMuted}
            />
          </Pressable>
          <MarketLegendButton />
        </View>
      </View>
      <FixtureDetailHero
        fixture={data.fixture}
        league={league}
        country={country}
        scores={data.scores}
        events={events.data}
      />
      <DetailTabBar selected={tab} onSelect={setTab} />

      <ScrollView
        style={styles.flex}
        contentContainerStyle={styles.content}
        refreshControl={
          <RefreshControl
            refreshing={isFetching}
            onRefresh={refetch}
            tintColor={c.brand}
          />
        }>
        {tab === 'details' ? (
          <>
            <FixtureTopPicksCard
              markets={oddsRates.data ?? []}
              fixtureId={fixtureId}
              fixtureName={
                data.fixture.home_team_name && data.fixture.away_team_name
                  ? `${data.fixture.home_team_name} - ${data.fixture.away_team_name}`
                  : `Maç #${fixtureId}`
              }
              startingAt={data.fixture.starting_at ?? null}
              bookmakerId={ODDS_BOOKMAKER_ID}
              upcoming={getStateBucket(data.fixture.state_id) === 'upcoming'}
            />
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
            fixtureId={fixtureId}
            fixtureName={
              data.fixture.home_team_name && data.fixture.away_team_name
                ? `${data.fixture.home_team_name} - ${data.fixture.away_team_name}`
                : `Maç #${fixtureId}`
            }
            startingAt={data.fixture.starting_at ?? null}
            bookmakerId={ODDS_BOOKMAKER_ID}
            liveScore={
              (getStateBucket(data.fixture.state_id) === 'live' ||
                getStateBucket(data.fixture.state_id) === 'finished') &&
              data.fixture.home_score != null &&
              data.fixture.away_score != null
                ? {
                    home: data.fixture.home_score,
                    away: data.fixture.away_score,
                  }
                : null
            }
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
        ) : tab === 'standings' ? (
          <StandingsTab
            loading={standings.isLoading}
            error={standings.error}
            rows={standings.data ?? []}
            highlightTeamIds={[
              data.fixture.home_team_id ?? null,
              data.fixture.away_team_id ?? null,
            ]}
          />
        ) : null}
      </ScrollView>
    </SafeAreaView>
  );
}

function OddsTabContent({
  loading,
  error,
  markets,
  fixtureId,
  fixtureName,
  startingAt,
  bookmakerId,
  liveScore,
}: {
  loading: boolean;
  error?: unknown;
  markets: FixtureOddsMarket[];
  fixtureId: number;
  fixtureName: string;
  startingAt: string | null;
  bookmakerId: number;
  liveScore: { home: number; away: number } | null;
}) {
  const { t } = useTranslation();
  return (
    <>
      {error && markets.length === 0 ? (
        <TabError error={error} />
      ) : loading && markets.length === 0 ? (
        <TabLoading />
      ) : markets.length === 0 ? (
        <TabEmpty message={t('fixture.odds.noOdds')} />
      ) : (
        markets.map((market) => (
          <OddsRatesCard
            key={market.market_id}
            market={market}
            fixtureId={fixtureId}
            fixtureName={fixtureName}
            startingAt={startingAt}
            bookmakerId={bookmakerId}
            liveScore={liveScore}
          />
        ))
      )}
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
  headerBar: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  headerBack: {
    width: 36,
    height: 36,
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 2,
  },
  headerIconBtn: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerTitle: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingHorizontal: 8,
  },
  headerLogo: {
    width: 22,
    height: 22,
  },
  headerName: {
    fontSize: 15,
    fontWeight: '700',
    flexShrink: 1,
  },
  empty: {
    paddingVertical: 64,
    alignItems: 'center',
  },
  emptyText: {
    fontSize: 14,
  },
});
