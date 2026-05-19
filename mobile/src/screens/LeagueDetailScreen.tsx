import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import {
  addDays,
  addWeeks,
  endOfWeek,
  format,
  isToday,
  isYesterday,
  parseISO,
  startOfWeek,
  subDays,
} from 'date-fns';
import { Image } from 'expo-image';
import { router } from 'expo-router';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  PanResponder,
  Pressable,
  RefreshControl,
  ScrollView,
  SectionList,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { FixtureCard } from '@/src/components/FixtureCard';
import { FixturePeekOverlay } from '@/src/components/FixturePeekOverlay';
import { StandingsTab } from '@/src/components/StandingsTab';
import { useCountryLookup } from '@/src/hooks/useCountryLookup';
import { useFixtures } from '@/src/hooks/useFixtures';
import { useLeagueLookup } from '@/src/hooks/useLeagueLookup';
import { useLeagueTable } from '@/src/hooks/useLeagueTable';
import { countryName } from '@/src/lib/countryName';
import { getStateBucket } from '@/src/lib/fixtureState';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureSummary } from '@/src/types/fixture';

interface LeagueDetailScreenProps {
  leagueId: number;
}

type Tab = 'matches' | 'stats' | 'standings';

const TAB_ORDER: Tab[] = ['matches', 'stats', 'standings'];

// Wide enough to support several weeks of swipe in either direction
// without re-fetching. ~5 weeks back, ~9 weeks forward.
const PAST_WINDOW_DAYS = 35;
const FUTURE_WINDOW_DAYS = 63;

// Monday-start weeks line up with how league fixtures are typically
// scheduled in Europe (matchweek runs Mon→Sun).
const WEEK_STARTS_ON: 1 = 1;

// Swipe thresholds match the rest of the app's gesture dictionary so the
// interaction feels uniform (home filter, fixture detail tabs, etc.).
const SWIPE_DOMINANCE = 1.5;
const SWIPE_TRIGGER_DISTANCE = 50;
const SWIPE_RECOGNITION_THRESHOLD = 12;

export function LeagueDetailScreen({ leagueId }: LeagueDetailScreenProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const [tab, setTab] = useState<Tab>('matches');

  // Long-press peek mirrors the home list. Two-phase lock: under 2s the
  // overlay closes on press-out; past 2s it pins open until the user
  // taps the X.
  const [peekFixture, setPeekFixture] = useState<FixtureSummary | null>(null);
  const [peekLocked, setPeekLocked] = useState(false);
  const lockTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const handlePeekStart = (f: FixtureSummary) => {
    setPeekFixture(f);
    setPeekLocked(false);
    if (lockTimerRef.current) clearTimeout(lockTimerRef.current);
    lockTimerRef.current = setTimeout(() => {
      setPeekLocked(true);
      lockTimerRef.current = null;
    }, 2000);
  };
  const handlePeekEnd = () => {
    if (lockTimerRef.current) {
      clearTimeout(lockTimerRef.current);
      lockTimerRef.current = null;
      setPeekFixture(null);
    }
  };
  const handlePeekClose = () => {
    if (lockTimerRef.current) {
      clearTimeout(lockTimerRef.current);
      lockTimerRef.current = null;
    }
    setPeekFixture(null);
    setPeekLocked(false);
  };
  useEffect(() => {
    return () => {
      if (lockTimerRef.current) clearTimeout(lockTimerRef.current);
    };
  }, []);

  // Tab swipe — same gesture dictionary as the rest of the app. The
  // closure-via-ref keeps the responder stable while still reading the
  // latest tab on each release.
  const tabRef = useRef<Tab>(tab);
  useEffect(() => {
    tabRef.current = tab;
  }, [tab]);
  const tabSwipeResponder = useRef(
    PanResponder.create({
      onMoveShouldSetPanResponder: (_, g) =>
        Math.abs(g.dx) > Math.abs(g.dy) * SWIPE_DOMINANCE &&
        Math.abs(g.dx) > SWIPE_RECOGNITION_THRESHOLD,
      onPanResponderRelease: (_, g) => {
        if (Math.abs(g.dx) < SWIPE_TRIGGER_DISTANCE) return;
        const idx = TAB_ORDER.indexOf(tabRef.current);
        if (idx < 0) return;
        if (g.dx > 0 && idx > 0) setTab(TAB_ORDER[idx - 1]);
        else if (g.dx < 0 && idx < TAB_ORDER.length - 1)
          setTab(TAB_ORDER[idx + 1]);
      },
    }),
  ).current;

  const leagueIds = useMemo(() => [leagueId], [leagueId]);
  const { lookup: leagueLookup } = useLeagueLookup(leagueIds);
  const league = leagueLookup.get(leagueId);

  const countryIds = useMemo(
    () => (league?.country_id != null ? [league.country_id] : []),
    [league?.country_id],
  );
  const { lookup: countryLookup } = useCountryLookup(countryIds);
  const country = league?.country_id
    ? countryLookup.get(league.country_id)
    : undefined;

  const today = useMemo(() => new Date(), []);
  const fromDate = useMemo(
    () => format(subDays(today, PAST_WINDOW_DAYS), 'yyyy-MM-dd'),
    [today],
  );
  const toDate = useMemo(
    () => format(addDays(today, FUTURE_WINDOW_DAYS), 'yyyy-MM-dd'),
    [today],
  );

  const fixturesQuery = useFixtures({
    leagueId,
    fromDate,
    toDate,
    perPage: 200,
  });
  const fixtures = fixturesQuery.data?.items ?? [];

  const standingsQuery = useLeagueTable(leagueId, null, tab === 'standings');

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
        <View style={styles.headerCenter}>
          {league?.image_path ? (
            <Image
              source={{ uri: league.image_path }}
              style={styles.headerLogo}
              contentFit="contain"
            />
          ) : null}
          <View style={styles.headerTitleBlock}>
            <ThemedText
              style={[styles.headerName, { color: c.text }]}
              numberOfLines={1}>
              {league?.name ?? `League #${leagueId}`}
            </ThemedText>
            {country?.name ? (
              <ThemedText
                style={[styles.headerSub, { color: c.textMuted }]}
                numberOfLines={1}>
                {countryName(country)}
              </ThemedText>
            ) : null}
          </View>
        </View>
        {/* Symmetry placeholder — keeps the title visually centred. */}
        <View style={styles.headerBack} />
      </View>

      <View style={[styles.tabBar, { borderBottomColor: c.border }]}>
        {(['matches', 'stats', 'standings'] as Tab[]).map((key) => {
          const active = tab === key;
          return (
            <Pressable
              key={key}
              onPress={() => setTab(key)}
              style={[
                styles.tab,
                active && { borderBottomColor: c.brand },
              ]}>
              <ThemedText
                style={[
                  styles.tabText,
                  { color: active ? c.text : c.textMuted },
                ]}>
                {t(`league.tabs.${key}`)}
              </ThemedText>
            </Pressable>
          );
        })}
      </View>

      <View style={styles.flex} {...tabSwipeResponder.panHandlers}>
      {tab === 'matches' ? (
        <MatchesByWeek
          fixtures={fixtures}
          loading={fixturesQuery.isLoading}
          fetching={fixturesQuery.isFetching}
          isError={fixturesQuery.isError}
          error={fixturesQuery.error}
          onRefresh={fixturesQuery.refetch}
          onLongPress={handlePeekStart}
          onPressOut={handlePeekEnd}
        />
      ) : tab === 'stats' ? (
        <StatsView
          fixtures={fixtures}
          loading={fixturesQuery.isLoading}
          fetching={fixturesQuery.isFetching}
          onRefresh={fixturesQuery.refetch}
        />
      ) : (
        <ScrollView
          contentContainerStyle={[
            styles.scrollContent,
            { paddingBottom: insets.bottom + 32 },
          ]}
          refreshControl={
            <RefreshControl
              refreshing={standingsQuery.isFetching}
              onRefresh={standingsQuery.refetch}
              tintColor={c.brand}
            />
          }>
          <StandingsTab
            loading={standingsQuery.isLoading}
            error={standingsQuery.error}
            rows={standingsQuery.data ?? []}
            highlightTeamIds={[]}
          />
        </ScrollView>
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

interface MatchesSection {
  title: string;
  data: FixtureSummary[];
  count: number;
}

function MatchesByWeek({
  fixtures,
  loading,
  fetching,
  isError,
  error,
  onRefresh,
  onLongPress,
  onPressOut,
}: {
  fixtures: FixtureSummary[];
  loading: boolean;
  fetching: boolean;
  isError: boolean;
  error: unknown;
  onRefresh: () => void;
  onLongPress?: (f: FixtureSummary) => void;
  onPressOut?: () => void;
}) {
  const c = useTheme();
  const { t } = useTranslation();

  // 0 = current week, -1 = previous week, +1 = next week, etc.
  const [weekOffset, setWeekOffset] = useState(0);
  const today = useMemo(() => new Date(), []);
  const weekStart = useMemo(
    () =>
      startOfWeek(addWeeks(today, weekOffset), { weekStartsOn: WEEK_STARTS_ON }),
    [today, weekOffset],
  );
  const weekEnd = useMemo(
    () => endOfWeek(weekStart, { weekStartsOn: WEEK_STARTS_ON }),
    [weekStart],
  );

  // Filter the broader fetched range down to just this week.
  const weekFixtures = useMemo(() => {
    return fixtures.filter((f) => {
      if (!f.starting_at) return false;
      const t = parseISO(f.starting_at).getTime();
      return t >= weekStart.getTime() && t <= weekEnd.getTime();
    });
  }, [fixtures, weekStart, weekEnd]);

  // Group by day inside the active week — same "today on top, then
  // chronological" rhythm as the home list.
  const sections = useMemo<MatchesSection[]>(() => {
    if (weekFixtures.length === 0) return [];
    const groups = new Map<string, FixtureSummary[]>();
    for (const f of weekFixtures) {
      const key = f.starting_at
        ? format(parseISO(f.starting_at), 'yyyy-MM-dd')
        : 'tbd';
      const list = groups.get(key);
      if (list) list.push(f);
      else groups.set(key, [f]);
    }
    return Array.from(groups.entries())
      .sort((a, b) => a[0].localeCompare(b[0]))
      .map(([key, list]) => {
        const date = key === 'tbd' ? null : parseISO(`${key}T00:00:00`);
        const title =
          date == null
            ? t('league.matches.tbd')
            : isToday(date)
              ? t('coupons.dateLabel.today')
              : isYesterday(date)
                ? t('coupons.dateLabel.yesterday')
                : format(date, 'dd MMM yyyy');
        const data = [...list].sort((a, b) =>
          (a.starting_at ?? '').localeCompare(b.starting_at ?? ''),
        );
        return { title, data, count: data.length };
      });
  }, [weekFixtures, t]);

  // Pan responder lets the user flick between weeks. Same "horizontal-
  // dominant" gate the rest of the app uses so vertical scroll keeps
  // working inside the section list.
  const offsetRef = useRef(weekOffset);
  useEffect(() => {
    offsetRef.current = weekOffset;
  }, [weekOffset]);
  const swipeResponder = useRef(
    PanResponder.create({
      onMoveShouldSetPanResponder: (_, g) =>
        Math.abs(g.dx) > Math.abs(g.dy) * SWIPE_DOMINANCE &&
        Math.abs(g.dx) > SWIPE_RECOGNITION_THRESHOLD,
      onPanResponderRelease: (_, g) => {
        if (Math.abs(g.dx) < SWIPE_TRIGGER_DISTANCE) return;
        if (g.dx > 0) setWeekOffset(offsetRef.current - 1);
        else setWeekOffset(offsetRef.current + 1);
      },
    }),
  ).current;

  // Shows "Bu Hafta" / "Geçen Hafta" / "5-11 May 2026" — humans care most
  // about "is this where I am" first, the precise dates second.
  const weekLabel = useMemo(() => {
    if (weekOffset === 0) return t('league.week.thisWeek');
    if (weekOffset === -1) return t('league.week.lastWeek');
    if (weekOffset === 1) return t('league.week.nextWeek');
    const sameMonth = weekStart.getMonth() === weekEnd.getMonth();
    return sameMonth
      ? `${format(weekStart, 'd')}-${format(weekEnd, 'd MMM yyyy')}`
      : `${format(weekStart, 'd MMM')} - ${format(weekEnd, 'd MMM yyyy')}`;
  }, [weekOffset, weekStart, weekEnd, t]);
  const containsToday = useMemo(
    () =>
      today.getTime() >= weekStart.getTime() &&
      today.getTime() <= weekEnd.getTime(),
    [today, weekStart, weekEnd],
  );

  return (
    <View style={styles.flex} {...swipeResponder.panHandlers}>
      <View
        style={[
          styles.weekBar,
          { backgroundColor: c.surface, borderBottomColor: c.border },
        ]}>
        <Pressable
          onPress={() => setWeekOffset((o) => o - 1)}
          hitSlop={10}
          style={({ pressed }) => [
            styles.weekArrow,
            pressed && { backgroundColor: c.brandSoft },
          ]}>
          <MaterialCommunityIcons
            name="chevron-left"
            size={22}
            color={c.text}
          />
        </Pressable>
        <Pressable
          onPress={() => setWeekOffset(0)}
          disabled={containsToday}
          style={styles.weekLabelBlock}>
          <ThemedText style={[styles.weekLabel, { color: c.text }]}>
            {weekLabel}
          </ThemedText>
          <ThemedText style={[styles.weekRange, { color: c.textMuted }]}>
            {format(weekStart, 'd MMM')} – {format(weekEnd, 'd MMM')}
          </ThemedText>
        </Pressable>
        <Pressable
          onPress={() => setWeekOffset((o) => o + 1)}
          hitSlop={10}
          style={({ pressed }) => [
            styles.weekArrow,
            pressed && { backgroundColor: c.brandSoft },
          ]}>
          <MaterialCommunityIcons
            name="chevron-right"
            size={22}
            color={c.text}
          />
        </Pressable>
      </View>
      <MatchesListBody
        sections={sections}
        loading={loading}
        fetching={fetching}
        isError={isError}
        error={error}
        onRefresh={onRefresh}
        weekIsFull={weekFixtures.length > 0}
        onLongPress={onLongPress}
        onPressOut={onPressOut}
      />
    </View>
  );
}

function MatchesListBody({
  sections,
  loading,
  fetching,
  isError,
  error,
  onRefresh,
  weekIsFull,
  onLongPress,
  onPressOut,
}: {
  sections: MatchesSection[];
  loading: boolean;
  fetching: boolean;
  isError: boolean;
  error: unknown;
  onRefresh: () => void;
  weekIsFull: boolean;
  onLongPress?: (f: FixtureSummary) => void;
  onPressOut?: () => void;
}) {
  const c = useTheme();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();

  if (loading && !weekIsFull) {
    return (
      <View style={styles.center}>
        <ActivityIndicator color={c.brand} />
      </View>
    );
  }

  if (isError && !weekIsFull) {
    return (
      <View style={styles.center}>
        <ThemedText style={[styles.errorTitle, { color: c.text }]}>
          {t('common.couldNotLoad')}
        </ThemedText>
        <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
          {error instanceof Error ? error.message : t('common.somethingWentWrong')}
        </ThemedText>
      </View>
    );
  }

  if (sections.length === 0) {
    return (
      <View style={styles.center}>
        <View style={[styles.emptyIcon, { backgroundColor: c.brandSoft }]}>
          <MaterialCommunityIcons
            name="calendar-blank-outline"
            size={28}
            color={c.brand}
          />
        </View>
        <ThemedText style={[styles.errorTitle, { color: c.text }]}>
          {t('league.week.empty')}
        </ThemedText>
        <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
          {t('league.week.emptyHint')}
        </ThemedText>
      </View>
    );
  }

  return (
    <SectionList
      sections={sections}
      keyExtractor={(f) => String(f.id)}
      renderItem={({ item, index, section }) => (
        <View
          style={[
            styles.cardWrap,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
            index === 0 && styles.cardWrapFirst,
            index === section.data.length - 1 && styles.cardWrapLast,
          ]}>
          {index > 0 ? (
            <View
              style={[
                styles.fixtureSeparator,
                { backgroundColor: c.borderSoft },
              ]}
            />
          ) : null}
          <FixtureCard
            fixture={item}
            onLongPress={onLongPress}
            onPressOut={onPressOut}
          />
        </View>
      )}
      renderSectionHeader={({ section }) => (
        <View style={[styles.sectionHeader, { backgroundColor: c.bg }]}>
          <ThemedText
            style={[styles.sectionHeaderText, { color: c.textMuted }]}>
            {section.title.toLocaleUpperCase('tr-TR')}
          </ThemedText>
          <View
            style={[
              styles.sectionCountBadge,
              { backgroundColor: c.brandSoft },
            ]}>
            <ThemedText style={[styles.sectionCountText, { color: c.brand }]}>
              {section.count}
            </ThemedText>
          </View>
        </View>
      )}
      stickySectionHeadersEnabled={false}
      contentContainerStyle={[
        styles.list,
        { paddingBottom: insets.bottom + 32 },
      ]}
      refreshControl={
        <RefreshControl
          refreshing={fetching}
          onRefresh={onRefresh}
          tintColor={c.brand}
        />
      }
    />
  );
}

// Stats are derived locally from the fixtures we already have in hand —
// the backend doesn't expose league-level aggregates, so we cap ambition
// at "what's computable from the season's fixture list and final scores":
// counts, result distribution, average goals per match, top attack /
// defense per team.
interface TeamGoalsRow {
  teamId: number;
  teamName: string;
  imagePath: string | null;
  scored: number;
  conceded: number;
  played: number;
}

function StatsView({
  fixtures,
  loading,
  fetching,
  onRefresh,
}: {
  fixtures: FixtureSummary[];
  loading: boolean;
  fetching: boolean;
  onRefresh: () => void;
}) {
  const c = useTheme();
  const { t } = useTranslation();

  const stats = useMemo(() => {
    let total = 0;
    let live = 0;
    let upcoming = 0;
    let finished = 0;
    let homeWins = 0;
    let draws = 0;
    let awayWins = 0;
    let goalsScored = 0;
    let scoredFixtures = 0;
    const perTeam = new Map<number, TeamGoalsRow>();

    for (const f of fixtures) {
      total++;
      const bucket = getStateBucket(f.state_id);
      if (bucket === 'live') live++;
      else if (bucket === 'upcoming') upcoming++;
      else if (bucket === 'finished') finished++;

      // Aggregate goals + result distribution from any fixture with a
      // settled scoreline (covers both 'live' with a current score and
      // 'finished'). Live fixtures get included so the snapshot reflects
      // the live state — it's a snapshot, not a season ledger.
      if (
        (bucket === 'live' || bucket === 'finished') &&
        f.home_score != null &&
        f.away_score != null
      ) {
        scoredFixtures++;
        goalsScored += f.home_score + f.away_score;
        if (bucket === 'finished') {
          if (f.home_score > f.away_score) homeWins++;
          else if (f.home_score < f.away_score) awayWins++;
          else draws++;
        }
        if (f.home_team_id != null) {
          const row =
            perTeam.get(f.home_team_id) ??
            ({
              teamId: f.home_team_id,
              teamName: f.home_team_name ?? `#${f.home_team_id}`,
              imagePath: f.home_team_image_path ?? null,
              scored: 0,
              conceded: 0,
              played: 0,
            } satisfies TeamGoalsRow);
          row.scored += f.home_score;
          row.conceded += f.away_score;
          row.played += 1;
          perTeam.set(f.home_team_id, row);
        }
        if (f.away_team_id != null) {
          const row =
            perTeam.get(f.away_team_id) ??
            ({
              teamId: f.away_team_id,
              teamName: f.away_team_name ?? `#${f.away_team_id}`,
              imagePath: f.away_team_image_path ?? null,
              scored: 0,
              conceded: 0,
              played: 0,
            } satisfies TeamGoalsRow);
          row.scored += f.away_score;
          row.conceded += f.home_score;
          row.played += 1;
          perTeam.set(f.away_team_id, row);
        }
      }
    }

    const teams = Array.from(perTeam.values());
    const topAttack = [...teams]
      .sort((a, b) => b.scored - a.scored || a.conceded - b.conceded)
      .slice(0, 5);
    const topDefense = [...teams]
      .filter((tm) => tm.played > 0)
      .sort(
        (a, b) =>
          a.conceded / a.played - b.conceded / b.played || b.played - a.played,
      )
      .slice(0, 5);
    const decided = homeWins + draws + awayWins;

    return {
      total,
      live,
      upcoming,
      finished,
      homeWins,
      draws,
      awayWins,
      decided,
      goalsScored,
      scoredFixtures,
      avgGoals: scoredFixtures > 0 ? goalsScored / scoredFixtures : 0,
      topAttack,
      topDefense,
    };
  }, [fixtures]);

  if (loading && fixtures.length === 0) {
    return (
      <View style={styles.center}>
        <ActivityIndicator color={c.brand} />
      </View>
    );
  }

  if (fixtures.length === 0) {
    return (
      <View style={styles.center}>
        <View style={[styles.emptyIcon, { backgroundColor: c.brandSoft }]}>
          <MaterialCommunityIcons
            name="chart-bar"
            size={28}
            color={c.brand}
          />
        </View>
        <ThemedText style={[styles.errorTitle, { color: c.text }]}>
          {t('league.stats.empty')}
        </ThemedText>
      </View>
    );
  }

  const homePct =
    stats.decided > 0 ? (stats.homeWins / stats.decided) * 100 : 0;
  const drawPct =
    stats.decided > 0 ? (stats.draws / stats.decided) * 100 : 0;
  const awayPct =
    stats.decided > 0 ? (stats.awayWins / stats.decided) * 100 : 0;

  return (
    <ScrollView
      contentContainerStyle={styles.statsScroll}
      refreshControl={
        <RefreshControl
          refreshing={fetching}
          onRefresh={onRefresh}
          tintColor={c.brand}
        />
      }>
      {/* Quick counts strip */}
      <View
        style={[
          styles.statsCard,
          { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
        ]}>
        <StatColumn
          value={String(stats.total)}
          label={t('league.stats.total')}
          color={c.text}
        />
        <View style={[styles.statDivider, { backgroundColor: c.borderSoft }]} />
        <StatColumn
          value={String(stats.live)}
          label={t('common.live')}
          color={stats.live > 0 ? c.live : c.textMuted}
        />
        <View style={[styles.statDivider, { backgroundColor: c.borderSoft }]} />
        <StatColumn
          value={String(stats.upcoming)}
          label={t('common.upcoming')}
          color={c.text}
        />
        <View style={[styles.statDivider, { backgroundColor: c.borderSoft }]} />
        <StatColumn
          value={String(stats.finished)}
          label={t('common.finished')}
          color={c.textMuted}
        />
      </View>

      {/* Goals card */}
      {stats.scoredFixtures > 0 ? (
        <View
          style={[
            styles.statsCard,
            styles.statsCardCol,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <ThemedText style={[styles.statsCardLabel, { color: c.textMuted }]}>
            {t('league.stats.goalsHeader')}
          </ThemedText>
          <View style={styles.goalsRow}>
            <View style={styles.goalsBlock}>
              <ThemedText style={[styles.bigNumber, { color: c.text }]}>
                {stats.goalsScored}
              </ThemedText>
              <ThemedText style={[styles.bigLabel, { color: c.textMuted }]}>
                {t('league.stats.totalGoals')}
              </ThemedText>
            </View>
            <View style={styles.goalsBlock}>
              <ThemedText style={[styles.bigNumber, { color: c.brand }]}>
                {stats.avgGoals.toFixed(2)}
              </ThemedText>
              <ThemedText style={[styles.bigLabel, { color: c.textMuted }]}>
                {t('league.stats.avgGoals')}
              </ThemedText>
            </View>
          </View>
        </View>
      ) : null}

      {/* Result distribution bar */}
      {stats.decided > 0 ? (
        <View
          style={[
            styles.statsCard,
            styles.statsCardCol,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <ThemedText style={[styles.statsCardLabel, { color: c.textMuted }]}>
            {t('league.stats.distHeader', { count: stats.decided })}
          </ThemedText>
          <View style={[styles.distBar, { backgroundColor: c.bg }]}>
            <View
              style={[
                styles.distSegment,
                { width: `${homePct}%`, backgroundColor: c.success },
              ]}
            />
            <View
              style={[
                styles.distSegment,
                { width: `${drawPct}%`, backgroundColor: c.textMuted },
              ]}
            />
            <View
              style={[
                styles.distSegment,
                { width: `${awayPct}%`, backgroundColor: c.danger },
              ]}
            />
          </View>
          <View style={styles.distLegend}>
            <DistLegendItem
              dot={c.success}
              label={t('league.stats.homeWins')}
              count={stats.homeWins}
              percent={homePct}
            />
            <DistLegendItem
              dot={c.textMuted}
              label={t('league.stats.draws')}
              count={stats.draws}
              percent={drawPct}
            />
            <DistLegendItem
              dot={c.danger}
              label={t('league.stats.awayWins')}
              count={stats.awayWins}
              percent={awayPct}
            />
          </View>
        </View>
      ) : null}

      {/* Top attack */}
      {stats.topAttack.length > 0 ? (
        <View
          style={[
            styles.statsCard,
            styles.statsCardCol,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <ThemedText style={[styles.statsCardLabel, { color: c.textMuted }]}>
            {t('league.stats.topAttack')}
          </ThemedText>
          {stats.topAttack.map((row, idx) => (
            <TeamStatRow
              key={row.teamId}
              rank={idx + 1}
              row={row}
              metric={String(row.scored)}
              metricColor={c.success}
            />
          ))}
        </View>
      ) : null}

      {/* Top defense (avg conceded) */}
      {stats.topDefense.length > 0 ? (
        <View
          style={[
            styles.statsCard,
            styles.statsCardCol,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <ThemedText style={[styles.statsCardLabel, { color: c.textMuted }]}>
            {t('league.stats.topDefense')}
          </ThemedText>
          {stats.topDefense.map((row, idx) => (
            <TeamStatRow
              key={row.teamId}
              rank={idx + 1}
              row={row}
              metric={(row.conceded / row.played).toFixed(2)}
              metricColor={c.brand}
            />
          ))}
        </View>
      ) : null}
    </ScrollView>
  );
}

function StatColumn({
  value,
  label,
  color,
}: {
  value: string;
  label: string;
  color: string;
}) {
  const c = useTheme();
  return (
    <View style={styles.statCol}>
      <ThemedText style={[styles.statValue, { color }]}>{value}</ThemedText>
      <ThemedText style={[styles.statLabel, { color: c.textMuted }]}>
        {label}
      </ThemedText>
    </View>
  );
}

function DistLegendItem({
  dot,
  label,
  count,
  percent,
}: {
  dot: string;
  label: string;
  count: number;
  percent: number;
}) {
  const c = useTheme();
  return (
    <View style={styles.distLegendItem}>
      <View style={[styles.distDot, { backgroundColor: dot }]} />
      <ThemedText style={[styles.distLegendLabel, { color: c.textMuted }]}>
        {label}
      </ThemedText>
      <ThemedText style={[styles.distLegendValue, { color: c.text }]}>
        {count} · %{percent.toFixed(0)}
      </ThemedText>
    </View>
  );
}

function TeamStatRow({
  rank,
  row,
  metric,
  metricColor,
}: {
  rank: number;
  row: TeamGoalsRow;
  metric: string;
  metricColor: string;
}) {
  const c = useTheme();
  return (
    <View style={[styles.teamStatRow, { borderTopColor: c.border }]}>
      <ThemedText style={[styles.teamStatRank, { color: c.textMuted }]}>
        {rank}
      </ThemedText>
      {row.imagePath ? (
        <Image
          source={{ uri: row.imagePath }}
          style={styles.teamStatLogo}
          contentFit="contain"
        />
      ) : (
        <View
          style={[styles.teamStatLogo, { backgroundColor: c.border }]}
        />
      )}
      <ThemedText
        style={[styles.teamStatName, { color: c.text }]}
        numberOfLines={1}>
        {row.teamName}
      </ThemedText>
      <ThemedText style={[styles.teamStatMetric, { color: metricColor }]}>
        {metric}
      </ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
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
  headerCenter: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 10,
    paddingHorizontal: 8,
  },
  headerLogo: {
    width: 26,
    height: 26,
  },
  headerTitleBlock: {
    flexShrink: 1,
    alignItems: 'center',
  },
  headerName: {
    fontSize: 15,
    fontWeight: '700',
    flexShrink: 1,
  },
  headerSub: {
    fontSize: 11,
    fontWeight: '500',
    marginTop: 1,
  },
  tabBar: {
    flexDirection: 'row',
    paddingHorizontal: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  tab: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 2,
    borderBottomColor: 'transparent',
  },
  tabText: {
    fontSize: 14,
    fontWeight: '600',
  },
  list: {
    paddingHorizontal: 12,
    paddingTop: 4,
    paddingBottom: 32,
  },
  // Week navigation strip — sits above the date sections, anchors the
  // weekly window so the user always knows which 7 days they're in.
  weekBar: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  weekArrow: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  weekLabelBlock: {
    flex: 1,
    alignItems: 'center',
  },
  weekLabel: {
    fontSize: 14,
    fontWeight: '700',
  },
  weekRange: {
    fontSize: 11,
    fontWeight: '500',
    marginTop: 1,
    fontVariant: ['tabular-nums'],
  },
  scrollContent: {
    paddingTop: 8,
    paddingBottom: 32,
  },
  sectionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 6,
    paddingTop: 18,
    paddingBottom: 8,
    gap: 8,
  },
  sectionHeaderText: {
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 0.7,
  },
  sectionCountBadge: {
    minWidth: 20,
    height: 18,
    paddingHorizontal: 6,
    borderRadius: 9,
    alignItems: 'center',
    justifyContent: 'center',
  },
  sectionCountText: {
    fontSize: 10,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  // Group fixtures of the same date inside a single rounded card so the
  // section reads as "this day's matches" instead of N detached chips.
  cardWrap: {
    borderLeftWidth: StyleSheet.hairlineWidth,
    borderRightWidth: StyleSheet.hairlineWidth,
  },
  cardWrapFirst: {
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopLeftRadius: 14,
    borderTopRightRadius: 14,
  },
  cardWrapLast: {
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomLeftRadius: 14,
    borderBottomRightRadius: 14,
  },
  fixtureSeparator: {
    height: StyleSheet.hairlineWidth,
    marginLeft: 64,
  },
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 32,
    gap: 10,
  },
  emptyIcon: {
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
  statsScroll: {
    paddingHorizontal: 12,
    paddingTop: 12,
    paddingBottom: 32,
    gap: 12,
  },
  statsCard: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    paddingVertical: 14,
    paddingHorizontal: 12,
    gap: 8,
  },
  statsCardCol: {
    flexDirection: 'column',
    alignItems: 'stretch',
    gap: 12,
  },
  statsCardLabel: {
    fontSize: 11,
    lineHeight: 14,
    fontWeight: '800',
    letterSpacing: 0.7,
  },
  statCol: {
    flex: 1,
    alignItems: 'center',
    gap: 2,
  },
  statValue: {
    fontSize: 18,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  statLabel: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
  statDivider: {
    width: StyleSheet.hairlineWidth,
    alignSelf: 'stretch',
  },
  goalsRow: {
    flexDirection: 'row',
  },
  goalsBlock: {
    flex: 1,
    alignItems: 'center',
    gap: 2,
  },
  bigNumber: {
    fontSize: 28,
    lineHeight: 34,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  bigLabel: {
    fontSize: 10,
    lineHeight: 13,
    fontWeight: '700',
    letterSpacing: 0.4,
  },
  distBar: {
    flexDirection: 'row',
    height: 10,
    borderRadius: 5,
    overflow: 'hidden',
  },
  distSegment: {
    height: '100%',
  },
  distLegend: {
    gap: 6,
  },
  distLegendItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  distDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
  },
  distLegendLabel: {
    flex: 1,
    fontSize: 12,
    fontWeight: '600',
  },
  distLegendValue: {
    fontSize: 12,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  teamStatRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 8,
    gap: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  teamStatRank: {
    width: 18,
    fontSize: 12,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  teamStatLogo: {
    width: 22,
    height: 22,
    borderRadius: 4,
  },
  teamStatName: {
    flex: 1,
    fontSize: 13,
    fontWeight: '600',
  },
  teamStatMetric: {
    fontSize: 14,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
});
