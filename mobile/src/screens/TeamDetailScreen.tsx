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
  ScrollView,
  SectionList,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { FixtureCard } from '@/src/components/FixtureCard';
import { FixturePeekOverlay } from '@/src/components/FixturePeekOverlay';
import { StandingsTab } from '@/src/components/StandingsTab';
import { TabEmpty } from '@/src/components/TabFeedback';
import { TeamSquadCard } from '@/src/components/TeamSquadCard';
import { TeamStatsCard } from '@/src/components/TeamStatsCard';
import { useCountryLookup } from '@/src/hooks/useCountryLookup';
import { useFixtures } from '@/src/hooks/useFixtures';
import { useLeagueTable } from '@/src/hooks/useLeagueTable';
import { useTeam } from '@/src/hooks/useTeam';
import { useTeamSeasonStats } from '@/src/hooks/useTeamSeasonStats';
import { useTeamSquad } from '@/src/hooks/useTeamSquad';
import { countryName } from '@/src/lib/countryName';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureSummary } from '@/src/types/fixture';

interface TeamDetailScreenProps {
  teamId: number;
}

const PAST_WINDOW_DAYS = 30;
const FUTURE_WINDOW_DAYS = 60;

type TeamTab = 'stats' | 'matches' | 'squad' | 'standings';
const TAB_ORDER: { key: TeamTab; i18nKey: string; defaultLabel: string }[] = [
  { key: 'stats', i18nKey: 'team.tabs.stats', defaultLabel: 'İstatistikler' },
  { key: 'matches', i18nKey: 'team.tabs.matches', defaultLabel: 'Maçlar' },
  { key: 'squad', i18nKey: 'team.tabs.squad', defaultLabel: 'Kadro' },
  { key: 'standings', i18nKey: 'team.tabs.standings', defaultLabel: 'Sıralama' },
];

interface MatchesSection {
  title: string;
  data: FixtureSummary[];
}

export function TeamDetailScreen({ teamId }: TeamDetailScreenProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const [tab, setTab] = useState<TeamTab>('stats');

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

  // Season stats — first row is the team's primary league (analytics
  // table sorts by as_of_date desc + league_id). Drives StatsCard +
  // primes StandingsTab with the right league + season.
  const statsQuery = useTeamSeasonStats(teamId);
  const primaryStats = statsQuery.data?.[0] ?? null;

  const squadQuery = useTeamSquad(teamId);

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
                  ? t('team.dateLabel.tomorrow', { defaultValue: 'Yarın' })
                  : format(date, 'dd MMM yyyy');
        return {
          title,
          data: [...list].sort((a, b) =>
            (a.starting_at ?? '').localeCompare(b.starting_at ?? ''),
          ),
        };
      });
  }, [fixtures, t]);

  // Standings hook — only fetch when standings tab is active and we
  // know the league + season (otherwise the call would 404).
  const standingsQuery = useLeagueTable(
    primaryStats?.league_id,
    primaryStats?.season_id,
    tab === 'standings',
  );

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
                {countryName(country)}
                {team?.founded ? ` · ${team.founded}` : ''}
              </ThemedText>
            ) : null}
          </View>
        </View>
        <View style={styles.headerBack} />
      </View>

      <View style={[styles.tabBar, { borderBottomColor: c.border, backgroundColor: c.surface }]}>
        {TAB_ORDER.map((tabDef) => {
          const active = tab === tabDef.key;
          return (
            <Pressable
              key={tabDef.key}
              onPress={() => setTab(tabDef.key)}
              style={styles.tabBtn}>
              <ThemedText
                style={[
                  styles.tabLabel,
                  {
                    color: active ? c.brand : c.textMuted,
                    fontWeight: active ? '800' : '600',
                  },
                ]}>
                {t(tabDef.i18nKey, { defaultValue: tabDef.defaultLabel })}
              </ThemedText>
              {active ? (
                <View style={[styles.tabUnderline, { backgroundColor: c.brand }]} />
              ) : null}
            </Pressable>
          );
        })}
      </View>

      {teamQuery.isLoading && !team ? (
        <View style={styles.center}>
          <ActivityIndicator color={c.brand} />
        </View>
      ) : tab === 'matches' ? (
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
              <ThemedText style={[styles.sectionHeaderText, { color: c.textMuted }]}>
                {section.title.toLocaleUpperCase('tr-TR')}
              </ThemedText>
              <View style={[styles.sectionCountBadge, { backgroundColor: c.brandSoft }]}>
                <ThemedText style={[styles.sectionCountText, { color: c.brand }]}>
                  {section.data.length}
                </ThemedText>
              </View>
            </View>
          )}
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
            <TabEmpty
              icon="calendar-blank-outline"
              message={t('team.matches.empty')}
            />
          }
        />
      ) : (
        <ScrollView
          contentContainerStyle={styles.scrollContent}
          refreshControl={
            <RefreshControl
              refreshing={
                tab === 'stats'
                  ? statsQuery.isFetching
                  : tab === 'squad'
                    ? squadQuery.isFetching
                    : standingsQuery.isFetching
              }
              onRefresh={() => {
                if (tab === 'stats') statsQuery.refetch();
                else if (tab === 'squad') squadQuery.refetch();
                else if (tab === 'standings') standingsQuery.refetch();
              }}
              tintColor={c.brand}
            />
          }>
          {tab === 'stats' ? (
            primaryStats ? (
              <TeamStatsCard stats={primaryStats} />
            ) : statsQuery.isLoading ? (
              <View style={styles.center}>
                <ActivityIndicator color={c.brand} />
              </View>
            ) : (
              <TabEmpty
                icon="chart-bar"
                message={t('team.stats.empty', {
                  defaultValue: 'Bu takım için sezon istatistiği henüz hesaplanmadı.',
                })}
              />
            )
          ) : tab === 'squad' ? (
            squadQuery.data && squadQuery.data.length > 0 ? (
              <TeamSquadCard
                squad={squadQuery.data}
                onPlayerPress={(playerId) =>
                  router.push(`/player/${playerId}` as never)
                }
              />
            ) : squadQuery.isLoading ? (
              <View style={styles.center}>
                <ActivityIndicator color={c.brand} />
              </View>
            ) : (
              <TabEmpty
                icon="account-group-outline"
                message={t('team.squad.empty', {
                  defaultValue: 'Kadro bilgisi yok.',
                })}
              />
            )
          ) : tab === 'standings' ? (
            <StandingsTab
              loading={standingsQuery.isLoading}
              error={standingsQuery.error}
              rows={standingsQuery.data ?? []}
              highlightTeamIds={[teamId]}
            />
          ) : null}
        </ScrollView>
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

const styles = StyleSheet.create({
  flex: { flex: 1 },
  center: {
    flex: 1,
    minHeight: 200,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
    gap: 8,
  },
  scrollContent: {
    paddingBottom: 96,
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
  headerCenter: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingHorizontal: 4,
  },
  headerLogo: {
    width: 28,
    height: 28,
  },
  headerTitleBlock: {
    flex: 1,
    gap: 1,
  },
  headerName: {
    fontSize: 15,
    fontWeight: '700',
  },
  headerSub: {
    fontSize: 11,
  },
  tabBar: {
    flexDirection: 'row',
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  tabBtn: {
    flex: 1,
    paddingVertical: 11,
    alignItems: 'center',
  },
  tabLabel: {
    fontSize: 12,
    letterSpacing: 0.4,
  },
  tabUnderline: {
    position: 'absolute',
    bottom: -StyleSheet.hairlineWidth,
    left: '20%',
    right: '20%',
    height: 2,
    borderRadius: 1,
  },
  list: {
    paddingBottom: 96,
  },
  cardWrap: {
    marginHorizontal: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderTopWidth: 0,
    borderBottomWidth: 0,
    overflow: 'hidden',
  },
  cardWrapFirst: {
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopLeftRadius: 12,
    borderTopRightRadius: 12,
  },
  cardWrapLast: {
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomLeftRadius: 12,
    borderBottomRightRadius: 12,
    marginBottom: 14,
  },
  fixtureSeparator: {
    height: StyleSheet.hairlineWidth,
    marginHorizontal: 12,
  },
  sectionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingTop: 16,
    paddingBottom: 8,
  },
  sectionHeaderText: {
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
  sectionCountBadge: {
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 999,
    minWidth: 22,
    alignItems: 'center',
  },
  sectionCountText: {
    fontSize: 10,
    fontWeight: '800',
  },
});
