import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Image } from 'expo-image';
import { router } from 'expo-router';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  type LayoutChangeEvent,
  PanResponder,
  Pressable,
  RefreshControl,
  StyleSheet,
  View,
} from 'react-native';
import Reanimated, {
  Extrapolation,
  interpolate,
  useAnimatedScrollHandler,
  useAnimatedStyle,
  useSharedValue,
} from 'react-native-reanimated';
import { SafeAreaView } from 'react-native-safe-area-context';

import { format, parseISO } from 'date-fns';

import { ThemedText } from '@/components/themed-text';
import { AiPicksCard } from '@/src/components/AiPicksCard';
import { AttackMomentumCard } from '@/src/components/AttackMomentumCard';
import { DetailTabBar, type DetailTab } from '@/src/components/DetailTabBar';
import { EventTimelineCard } from '@/src/components/EventTimelineCard';
import { MarketLegendButton } from '@/src/components/MarketLegendButton';
import { FixtureDetailHero } from '@/src/components/FixtureDetailHero';
import { FixtureTopPicksCard } from '@/src/components/FixtureTopPicksCard';
import { H2HTab } from '@/src/components/H2HTab';
import { LineupsTab } from '@/src/components/LineupsTab';
import { MatchInfoCard } from '@/src/components/MatchInfoCard';
import { MatchInsightsCard } from '@/src/components/MatchInsightsCard';
import { OddsRatesCard } from '@/src/components/OddsRatesCard';
import { StandingsTab } from '@/src/components/StandingsTab';
import { StatsTab } from '@/src/components/StatsTab';
import { TabError, TabLoading, TabEmpty } from '@/src/components/TabFeedback';
import { TvStationsCard } from '@/src/components/TvStationsCard';
import { useCountryLookup } from '@/src/hooks/useCountryLookup';
import { useFixture } from '@/src/hooks/useFixture';
import {
  useFixtureEvents,
  useFixtureExpectedGoals,
  useFixtureH2H,
  useFixtureLineups,
  useFixtureMatchFacts,
  useFixtureSidelined,
  useFixtureStatistics,
  useFixtureTrends,
  useFixtureTvStations,
  useFixtureValueBets,
  useFixtureWeather,
} from '@/src/hooks/useFixtureExtras';
import { useFixtureOddsRates } from '@/src/hooks/useFixtureOddsRates';
import { useFixtures } from '@/src/hooks/useFixtures';
import { shareFixture } from '@/src/lib/share';
import { useLeagueLookup } from '@/src/hooks/useLeagueLookup';
import { useLeagueTable } from '@/src/hooks/useLeagueTable';
import { useLiveFixture } from '@/src/hooks/useLiveFixture';
import { getStateBucket } from '@/src/lib/fixtureState';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureSummary } from '@/src/types/fixture';
import type { FixtureOddsMarket } from '@/src/types/fixtureOdds';

// Hero shrinks from full height → 0 over this scroll range. Past it the
// CompactHeroBar (home name | score | away name) is the only fixture context
// the user sees, so the tab content gets the screen real estate.
const HERO_COLLAPSE_RANGE = 90;
const COMPACT_BAR_HEIGHT = 44;

// Tab bar order — used by the swipe handler to step left/right when the
// user flicks horizontally over the tab body. Same order as DetailTabBar.
const TAB_ORDER: DetailTab[] = [
  'details',
  'odds',
  'stats',
  'lineups',
  'standings',
  'h2h',
  'insights',
];

// Swipe needs to be both: clearly horizontal (dx significantly > dy) AND
// far enough to feel intentional. The 1.5 ratio + 12px threshold keeps
// vertical scrolling responsive while still picking up swift side flicks.
const SWIPE_DOMINANCE = 1.5;
const SWIPE_TRIGGER_DISTANCE = 50;
const SWIPE_RECOGNITION_THRESHOLD = 12;

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

  // Hero collapse + compact-bar reveal run on Reanimated's UI thread via a
  // shared scroll position — the JS-driven Animated.Value version flickered
  // because each frame had to round-trip the bridge, then run a Yoga
  // relayout for the height change. Reanimated keeps both interpolations
  // on the UI thread so the header tracks the scroll position smoothly.
  //
  // The visible wrapper has an interpolated height (and overflow hidden),
  // which means onLayout there reports the *clipped* size — so we'd freeze
  // the height before async data (events / goal lines) lands. Measure the
  // natural size in a hidden, absolutely-positioned twin so late-arriving
  // content (e.g. a goal scored in the 70th minute) makes the wrapper
  // grow to fit.
  const scrollY = useSharedValue(0);
  // Hero height lives in a shared value (not React state) so a late
  // measurement — events arriving with goals, weather row appearing —
  // doesn't trigger a JS re-render that resnaps the useAnimatedStyle
  // worklet mid-frame. The visible flicker the user reports when
  // scrolling fast was that resnap.
  const heroHeightSV = useSharedValue(0);
  const onHeroLayout = useCallback(
    (e: LayoutChangeEvent) => {
      const h = e.nativeEvent.layout.height;
      if (h > 0 && Math.abs(heroHeightSV.value - h) >= 1) {
        heroHeightSV.value = h;
      }
    },
    [heroHeightSV],
  );
  const scrollHandler = useAnimatedScrollHandler({
    onScroll: (e) => {
      scrollY.value = e.contentOffset.y;
    },
  });

  const heroAnimatedStyle = useAnimatedStyle(() => {
    const h = heroHeightSV.value;
    if (h === 0) {
      // First few frames before onLayout fires — let the wrapper take
      // its natural size so the hero shows immediately at full height
      // rather than blinking from 0 → measured.
      return { opacity: 1, overflow: 'hidden' };
    }
    return {
      height: interpolate(
        scrollY.value,
        [0, HERO_COLLAPSE_RANGE],
        [h, 0],
        Extrapolation.CLAMP,
      ),
      opacity: interpolate(
        scrollY.value,
        [0, HERO_COLLAPSE_RANGE * 0.6],
        [1, 0],
        Extrapolation.CLAMP,
      ),
      overflow: 'hidden',
    };
  });

  const compactAnimatedStyle = useAnimatedStyle(() => ({
    height: interpolate(
      scrollY.value,
      [0, HERO_COLLAPSE_RANGE],
      [0, COMPACT_BAR_HEIGHT],
      Extrapolation.CLAMP,
    ),
    opacity: interpolate(
      scrollY.value,
      [HERO_COLLAPSE_RANGE * 0.5, HERO_COLLAPSE_RANGE],
      [0, 1],
      Extrapolation.CLAMP,
    ),
  }));

  // Horizontal-swipe-to-switch-tabs. The PanResponder is created once,
  // so we keep the latest tab in a ref and read it inside the handler —
  // capturing `tab` directly would freeze the closure to the first value.
  const tabRef = useRef<DetailTab>(tab);
  useEffect(() => {
    tabRef.current = tab;
  }, [tab]);
  const swipeResponder = useRef(
    PanResponder.create({
      // Only claim the gesture when it's clearly horizontal; otherwise the
      // inner ScrollView keeps owning vertical scroll.
      onMoveShouldSetPanResponder: (_, g) =>
        Math.abs(g.dx) > Math.abs(g.dy) * SWIPE_DOMINANCE &&
        Math.abs(g.dx) > SWIPE_RECOGNITION_THRESHOLD,
      onPanResponderRelease: (_, g) => {
        if (Math.abs(g.dx) < SWIPE_TRIGGER_DISTANCE) return;
        const idx = TAB_ORDER.indexOf(tabRef.current);
        if (idx < 0) return;
        if (g.dx > 0 && idx > 0) {
          setTab(TAB_ORDER[idx - 1]);
        } else if (g.dx < 0 && idx < TAB_ORDER.length - 1) {
          setTab(TAB_ORDER[idx + 1]);
        }
      },
    }),
  ).current;

  // Sibling fixtures from the same league + matchday. Used to wire the
  // header swipe so the user can flip through every match in the league
  // without going back to the home list.
  const fixtureDate = data?.fixture.starting_at
    ? format(parseISO(data.fixture.starting_at), 'yyyy-MM-dd')
    : null;
  const { data: dayFixtures } = useFixtures(
    {
      date: fixtureDate ?? '',
      leagueId: data?.fixture.league_id ?? undefined,
      perPage: 100,
    },
    { refetchIntervalMs: undefined },
  );
  const leagueFixtures = useMemo(() => {
    if (!dayFixtures?.items) return [];
    return [...dayFixtures.items].sort((a, b) =>
      (a.starting_at ?? '').localeCompare(b.starting_at ?? ''),
    );
  }, [dayFixtures?.items]);
  const leagueFixturesRef = useRef(leagueFixtures);
  useEffect(() => {
    leagueFixturesRef.current = leagueFixtures;
  }, [leagueFixtures]);
  const fixtureIdRef = useRef(fixtureId);
  useEffect(() => {
    fixtureIdRef.current = fixtureId;
  }, [fixtureId]);

  // Header-area swipe: jump to sibling matches in the same league. Same
  // gesture thresholds as the tab swipe so they feel uniform; left-flick
  // = next kickoff, right-flick = previous.
  const leagueSwipeResponder = useRef(
    PanResponder.create({
      onMoveShouldSetPanResponder: (_, g) =>
        Math.abs(g.dx) > Math.abs(g.dy) * SWIPE_DOMINANCE &&
        Math.abs(g.dx) > SWIPE_RECOGNITION_THRESHOLD,
      onPanResponderRelease: (_, g) => {
        if (Math.abs(g.dx) < SWIPE_TRIGGER_DISTANCE) return;
        const list = leagueFixturesRef.current;
        if (list.length < 2) return;
        const idx = list.findIndex((f) => f.id === fixtureIdRef.current);
        if (idx < 0) return;
        if (g.dx > 0 && idx > 0) {
          router.replace(`/fixture/${list[idx - 1].id}` as never);
        } else if (g.dx < 0 && idx < list.length - 1) {
          router.replace(`/fixture/${list[idx + 1].id}` as never);
        }
      },
    }),
  ).current;

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
  const sidelined = useFixtureSidelined(fixtureId, tab === 'lineups');
  const h2h = useFixtureH2H(fixtureId, 10, tab === 'h2h');
  // Trial-bundle streams — fetched per tab so we don't pay for trends
  // while the user is on the standings tab. Weather is the exception
  // because it lives inside the hero, which is visible on every tab.
  const trends = useFixtureTrends(fixtureId, tab === 'details');
  const expectedGoals = useFixtureExpectedGoals(fixtureId, tab === 'details');
  const matchFacts = useFixtureMatchFacts(fixtureId, 30, tab === 'insights');
  const weather = useFixtureWeather(fixtureId, true);
  const tvStations = useFixtureTvStations(fixtureId, tab === 'details');
  const valueBets = useFixtureValueBets(fixtureId, tab === 'details');
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
      <View {...leagueSwipeResponder.panHandlers}>
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
        <Pressable
          onPress={() => {
            if (data.fixture.league_id != null) {
              router.push(
                `/league/${data.fixture.league_id}` as never,
              );
            }
          }}
          accessibilityRole="button"
          accessibilityLabel={league?.name ?? ''}
          style={({ pressed }) => [
            styles.headerTitle,
            pressed && { opacity: 0.6 },
          ]}>
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
        </Pressable>
        <View style={styles.headerActions}>
          <Pressable
            onPress={() => {
              const fxName =
                data.fixture.home_team_name && data.fixture.away_team_name
                  ? `${data.fixture.home_team_name} - ${data.fixture.away_team_name}`
                  : t('markets.fallback', { id: fixtureId });
              shareFixture(fixtureId, fxName);
            }}
            hitSlop={12}
            accessibilityRole="button"
            accessibilityLabel={t('common.share')}
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
      <Reanimated.View
        style={[
          styles.compactBar,
          {
            backgroundColor: c.surface,
            borderBottomColor: c.border,
          },
          compactAnimatedStyle,
        ]}
        pointerEvents="none">
        <CompactHeroBar fixture={data.fixture} />
      </Reanimated.View>
      {/* Hidden measurer — rendered once at natural height so late-arriving
          events (goals) update heroHeight, letting the visible wrapper
          grow. Absolute positioning keeps it out of layout flow. */}
      <View
        style={styles.heroMeasurer}
        pointerEvents="none"
        onLayout={onHeroLayout}>
        <FixtureDetailHero
          fixture={data.fixture}
          league={league}
          country={country}
          scores={data.scores}
          events={events.data}
          weather={weather.data}
        />
      </View>
      <Reanimated.View style={heroAnimatedStyle}>
        <FixtureDetailHero
          fixture={data.fixture}
          league={league}
          country={country}
          scores={data.scores}
          events={events.data}
          weather={weather.data}
        />
      </Reanimated.View>
      </View>
      <DetailTabBar selected={tab} onSelect={setTab} />

      <View style={styles.flex} {...swipeResponder.panHandlers}>
      <Reanimated.ScrollView
        style={styles.flex}
        contentContainerStyle={styles.content}
        onScroll={scrollHandler}
        scrollEventThrottle={16}
        refreshControl={
          <RefreshControl
            refreshing={isFetching}
            onRefresh={refetch}
            tintColor={c.brand}
          />
        }>
        {tab === 'details' ? (
          <>
            <AttackMomentumCard
              trends={trends.data}
              expectedGoals={expectedGoals.data ?? null}
              homeName={data.fixture.home_team_name}
              awayName={data.fixture.away_team_name}
            />
            <FixtureTopPicksCard
              markets={oddsRates.data ?? []}
              fixtureId={fixtureId}
              fixtureName={
                data.fixture.home_team_name && data.fixture.away_team_name
                  ? `${data.fixture.home_team_name} - ${data.fixture.away_team_name}`
                  : t('markets.fallback', { id: fixtureId })
              }
              startingAt={data.fixture.starting_at ?? null}
              bookmakerId={ODDS_BOOKMAKER_ID}
              upcoming={getStateBucket(data.fixture.state_id) === 'upcoming'}
            />
            <AiPicksCard
              bets={valueBets.data}
              homeName={data.fixture.home_team_name}
              awayName={data.fixture.away_team_name}
            />
            {events.data && events.data.length > 0 ? (
              <EventTimelineCard events={events.data} />
            ) : null}
            <TvStationsCard stations={tvStations.data} />
            <MatchInfoCard
              fixture={data.fixture}
              league={league}
              country={country}
            />
          </>
        ) : tab === 'insights' ? (
          matchFacts.isLoading && !matchFacts.data ? (
            <TabLoading />
          ) : matchFacts.error && !matchFacts.data ? (
            <TabError error={matchFacts.error} />
          ) : !matchFacts.data || matchFacts.data.length === 0 ? (
            <TabEmpty
              icon="lightbulb-outline"
              message={t('fixture.insights.empty')}
            />
          ) : (
            <MatchInsightsCard facts={matchFacts.data} collapsedCount={50} />
          )
        ) : tab === 'odds' ? (
          <OddsTabContent
            loading={oddsRates.isLoading}
            error={oddsRates.error}
            markets={oddsRates.data ?? []}
            fixtureId={fixtureId}
            fixtureName={
              data.fixture.home_team_name && data.fixture.away_team_name
                ? `${data.fixture.home_team_name} - ${data.fixture.away_team_name}`
                : t('markets.fallback', { id: fixtureId })
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
            sidelined={sidelined.data ?? null}
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
      </Reanimated.ScrollView>
      </View>
    </SafeAreaView>
  );
}

function CompactHeroBar({ fixture }: { fixture: FixtureSummary }) {
  const c = useTheme();
  const bucket = getStateBucket(fixture.state_id);
  const live = bucket === 'live';
  const scored = live || bucket === 'finished';
  const center = scored
    ? `${fixture.home_score ?? 0} - ${fixture.away_score ?? 0}`
    : fixture.starting_at
      ? format(parseISO(fixture.starting_at), 'HH:mm')
      : '--:--';
  return (
    <View style={styles.compactRow}>
      <View style={styles.compactTeamHome}>
        {fixture.home_team_image_path ? (
          <Image
            source={{ uri: fixture.home_team_image_path }}
            style={styles.compactLogo}
            contentFit="contain"
          />
        ) : (
          <View style={[styles.compactLogo, styles.compactLogoFallback, { backgroundColor: c.border }]} />
        )}
        <ThemedText
          style={[styles.compactName, { color: c.text }]}
          numberOfLines={1}>
          {fixture.home_team_name ?? 'TBD'}
        </ThemedText>
      </View>
      <ThemedText
        style={[styles.compactScore, { color: live ? c.live : c.text }]}>
        {center}
      </ThemedText>
      <View style={styles.compactTeamAway}>
        <ThemedText
          style={[styles.compactName, styles.compactNameRight, { color: c.text }]}
          numberOfLines={1}>
          {fixture.away_team_name ?? 'TBD'}
        </ThemedText>
        {fixture.away_team_image_path ? (
          <Image
            source={{ uri: fixture.away_team_image_path }}
            style={styles.compactLogo}
            contentFit="contain"
          />
        ) : (
          <View style={[styles.compactLogo, styles.compactLogoFallback, { backgroundColor: c.border }]} />
        )}
      </View>
    </View>
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
        <TabEmpty icon="tag-outline" message={t('fixture.odds.noOdds')} />
      ) : (
        markets.map((market, idx) => (
          <OddsRatesCard
            key={market.market_id}
            market={market}
            fixtureId={fixtureId}
            fixtureName={fixtureName}
            startingAt={startingAt}
            bookmakerId={bookmakerId}
            liveScore={liveScore}
            initiallyCollapsed={idx > 0}
          />
        ))
      )}
    </>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  content: {
    // Bottom inset large enough that the last card clears both the iOS
    // home-indicator strip and the tab bar even on edge-to-edge Android
    // devices that draw under the gesture area. 32px was hiding ~half
    // of the final card on phones with a gesture bar.
    paddingBottom: 96,
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
  compactBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  heroMeasurer: {
    position: 'absolute',
    left: 0,
    right: 0,
    top: -10000,
    opacity: 0,
  },
  compactRow: {
    flexDirection: 'row',
    alignItems: 'center',
    width: '100%',
    paddingHorizontal: 16,
    height: COMPACT_BAR_HEIGHT,
    gap: 12,
  },
  compactTeamHome: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  compactTeamAway: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'flex-end',
    gap: 8,
  },
  compactLogo: {
    width: 22,
    height: 22,
  },
  compactLogoFallback: {
    borderRadius: 4,
  },
  compactName: {
    flexShrink: 1,
    fontSize: 14,
    fontWeight: '600',
  },
  compactNameRight: {
    textAlign: 'right',
  },
  compactScore: {
    fontSize: 16,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
    minWidth: 64,
    textAlign: 'center',
  },
});
