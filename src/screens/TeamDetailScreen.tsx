import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import {
  addDays,
  format,
  isToday,
  isTomorrow,
  isYesterday,
  parseISO,
  subDays,
} from 'date-fns';
import { Image } from 'expo-image';
import { router } from 'expo-router';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  Pressable,
  RefreshControl,
  SectionList,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { FixtureCard } from '@/src/components/FixtureCard';
import { FixturePeekOverlay } from '@/src/components/FixturePeekOverlay';
import { useCountryLookup } from '@/src/hooks/useCountryLookup';
import { useFixtures } from '@/src/hooks/useFixtures';
import { useTeam } from '@/src/hooks/useTeam';
import { getStateBucket } from '@/src/lib/fixtureState';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureSummary } from '@/src/types/fixture';

interface TeamDetailScreenProps {
  teamId: number;
}

// Pull a generous window — teams play across multiple competitions, so a
// month back / two months forward gives the user a meaningful "recent +
// upcoming" view without season pagination.
const PAST_WINDOW_DAYS = 30;
const FUTURE_WINDOW_DAYS = 60;

interface MatchesSection {
  title: string;
  data: FixtureSummary[];
}

export function TeamDetailScreen({ teamId }: TeamDetailScreenProps) {
  const c = useTheme();
  const { t } = useTranslation();

  const teamQuery = useTeam(teamId);
  const team = teamQuery.data;

  const countryIds = useMemo(
    () => (team?.country_id != null ? [team.country_id] : []),
    [team?.country_id],
  );
  const { lookup: countryLookup } = useCountryLookup(countryIds);
  const country = team?.country_id
    ? countryLookup.get(team.country_id)
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
    teamId,
    fromDate,
    toDate,
    perPage: 200,
  });
  const fixtures = fixturesQuery.data?.items ?? [];

  // Same date-grouping rhythm as the home + league screens.
  const sections = useMemo<MatchesSection[]>(() => {
    if (fixtures.length === 0) return [];
    const groups = new Map<string, FixtureSummary[]>();
    for (const f of fixtures) {
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
                : isTomorrow(date)
                  ? t('team.dateLabel.tomorrow')
                  : format(date, 'dd MMM yyyy');
        return {
          title,
          data: [...list].sort((a, b) =>
            (a.starting_at ?? '').localeCompare(b.starting_at ?? ''),
          ),
        };
      });
  }, [fixtures, t]);

  // Stat snapshot — counts + W/D/L for finished matches involving this team.
  const stats = useMemo(() => {
    let played = 0;
    let wins = 0;
    let draws = 0;
    let losses = 0;
    let goalsFor = 0;
    let goalsAgainst = 0;
    let upcoming = 0;
    let live = 0;
    for (const f of fixtures) {
      const bucket = getStateBucket(f.state_id);
      if (bucket === 'live') live++;
      else if (bucket === 'upcoming') upcoming++;
      if (
        (bucket === 'finished' || bucket === 'live') &&
        f.home_score != null &&
        f.away_score != null
      ) {
        const isHome = f.home_team_id === teamId;
        const isAway = f.away_team_id === teamId;
        if (!isHome && !isAway) continue;
        if (bucket === 'finished') {
          played++;
          const us = isHome ? f.home_score : f.away_score;
          const them = isHome ? f.away_score : f.home_score;
          goalsFor += us;
          goalsAgainst += them;
          if (us > them) wins++;
          else if (us < them) losses++;
          else draws++;
        }
      }
    }
    return { played, wins, draws, losses, goalsFor, goalsAgainst, upcoming, live };
  }, [fixtures, teamId]);

  // Long-press peek mirrors the home + league screens.
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
          {team?.image_path ? (
            <Image
              source={{ uri: team.image_path }}
              style={styles.headerLogo}
              contentFit="contain"
            />
          ) : null}
          <View style={styles.headerTitleBlock}>
            <ThemedText
              style={[styles.headerName, { color: c.text }]}
              numberOfLines={1}>
              {team?.name ?? `Team #${teamId}`}
            </ThemedText>
            {country?.name ? (
              <ThemedText
                style={[styles.headerSub, { color: c.textMuted }]}
                numberOfLines={1}>
                {country.name}
              </ThemedText>
            ) : null}
          </View>
        </View>
        <View style={styles.headerBack} />
      </View>

      {teamQuery.isLoading && !team ? (
        <View style={styles.center}>
          <ActivityIndicator color={c.brand} />
        </View>
      ) : (
        <SectionList
          sections={sections}
          keyExtractor={(f) => String(f.id)}
          keyboardShouldPersistTaps="handled"
          renderItem={({ item, index, section }) => (
            <View
              style={[
                styles.cardWrap,
                {
                  backgroundColor: c.surfaceElevated,
                  borderColor: c.borderSoft,
                },
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
                onLongPress={handlePeekStart}
                onPressOut={handlePeekEnd}
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
                  {section.data.length}
                </ThemedText>
              </View>
            </View>
          )}
          ListHeaderComponent={
            <StatsBlock stats={stats} />
          }
          stickySectionHeadersEnabled={false}
          contentContainerStyle={styles.list}
          refreshControl={
            <RefreshControl
              refreshing={fixturesQuery.isFetching}
              onRefresh={fixturesQuery.refetch}
              tintColor={c.brand}
            />
          }
          ListEmptyComponent={
            <View style={styles.center}>
              <View style={[styles.emptyIcon, { backgroundColor: c.brandSoft }]}>
                <MaterialCommunityIcons
                  name="calendar-blank-outline"
                  size={28}
                  color={c.brand}
                />
              </View>
              <ThemedText style={[styles.errorTitle, { color: c.text }]}>
                {t('team.matches.empty')}
              </ThemedText>
            </View>
          }
        />
      )}

      <FixturePeekOverlay
        fixture={peekFixture}
        locked={peekLocked}
        onClose={handlePeekClose}
        onChangeFixture={(next) => setPeekFixture(next)}
      />
    </SafeAreaView>
  );
}

function StatsBlock({
  stats,
}: {
  stats: {
    played: number;
    wins: number;
    draws: number;
    losses: number;
    goalsFor: number;
    goalsAgainst: number;
    upcoming: number;
    live: number;
  };
}) {
  const c = useTheme();
  const { t } = useTranslation();
  if (
    stats.played === 0 &&
    stats.upcoming === 0 &&
    stats.live === 0
  ) {
    return null;
  }
  return (
    <View style={styles.statsWrap}>
      {/* Quick counts */}
      <View
        style={[
          styles.statsCard,
          { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
        ]}>
        <StatCol
          value={String(stats.played)}
          label={t('team.stats.played')}
          color={c.text}
        />
        <View style={[styles.statDivider, { backgroundColor: c.borderSoft }]} />
        <StatCol
          value={String(stats.wins)}
          label={t('team.stats.wins')}
          color={stats.wins > 0 ? c.success : c.textMuted}
        />
        <View style={[styles.statDivider, { backgroundColor: c.borderSoft }]} />
        <StatCol
          value={String(stats.draws)}
          label={t('team.stats.draws')}
          color={c.text}
        />
        <View style={[styles.statDivider, { backgroundColor: c.borderSoft }]} />
        <StatCol
          value={String(stats.losses)}
          label={t('team.stats.losses')}
          color={stats.losses > 0 ? c.danger : c.textMuted}
        />
      </View>

      {/* Goals — for / against / diff */}
      {stats.played > 0 ? (
        <View
          style={[
            styles.statsCard,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <StatCol
            value={String(stats.goalsFor)}
            label={t('team.stats.goalsFor')}
            color={c.success}
          />
          <View style={[styles.statDivider, { backgroundColor: c.borderSoft }]} />
          <StatCol
            value={String(stats.goalsAgainst)}
            label={t('team.stats.goalsAgainst')}
            color={c.danger}
          />
          <View style={[styles.statDivider, { backgroundColor: c.borderSoft }]} />
          <StatCol
            value={`${stats.goalsFor - stats.goalsAgainst >= 0 ? '+' : ''}${
              stats.goalsFor - stats.goalsAgainst
            }`}
            label={t('team.stats.goalDiff')}
            color={
              stats.goalsFor === stats.goalsAgainst
                ? c.text
                : stats.goalsFor > stats.goalsAgainst
                  ? c.success
                  : c.danger
            }
          />
        </View>
      ) : null}
    </View>
  );
}

function StatCol({
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
    width: 28,
    height: 28,
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
  list: {
    paddingHorizontal: 12,
    paddingTop: 4,
    paddingBottom: 32,
  },
  statsWrap: {
    paddingTop: 12,
    paddingBottom: 4,
    gap: 10,
  },
  statsCard: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    paddingVertical: 12,
    paddingHorizontal: 8,
  },
  statCol: {
    flex: 1,
    alignItems: 'center',
    gap: 2,
  },
  statValue: {
    fontSize: 18,
    lineHeight: 22,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  statLabel: {
    fontSize: 10,
    lineHeight: 13,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
  statDivider: {
    width: StyleSheet.hairlineWidth,
    alignSelf: 'stretch',
  },
  sectionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 6,
    paddingTop: 16,
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
});
