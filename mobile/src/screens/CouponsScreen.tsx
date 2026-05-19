import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { format, isToday, isYesterday, parseISO, subDays } from 'date-fns';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Modal,
  Pressable,
  SectionList,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { AdBanner } from '@/src/components/AdBanner';
import { AppBrand } from '@/src/components/AppBrand';
import { AppDisclaimerFooter } from '@/src/components/AppDisclaimerFooter';
import { CouponStatsCard } from '@/src/components/CouponStatsCard';
import { GuestStatsCTA } from '@/src/components/GuestStatsCTA';
import { useTier } from '@/src/lib/auth/authStore';
import { MarketLegendButton } from '@/src/components/MarketLegendButton';
import { useFixtureLookup } from '@/src/hooks/useFixtureLookup';
import { useLiveTicker } from '@/src/hooks/useLiveTicker';
import { shareCoupon } from '@/src/lib/share';
import {
  couponOutcome,
  deleteSavedCoupon,
  selectionStarted,
  useCouponStore,
} from '@/src/lib/coupons/store';
import type { Coupon } from '@/src/lib/coupons/types';
import { getStateBucket } from '@/src/lib/fixtureState';
import { outcomeLiveStatus, type LiveScore } from '@/src/lib/liveOutcome';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureDetail } from '@/src/types/fixtureDetail';

type DateFilter = 'all' | 'today' | 'week' | 'month';

const DATE_FILTERS: { key: DateFilter; i18nKey: string }[] = [
  { key: 'all', i18nKey: 'common.all' },
  { key: 'today', i18nKey: 'coupons.dateFilter.today' },
  { key: 'week', i18nKey: 'coupons.dateFilter.week' },
  { key: 'month', i18nKey: 'coupons.dateFilter.month' },
];

// 1st-half score for the finished-match badge. Mirrors the helper inside
// FixtureDetailHero — split here to avoid pulling that whole component
// into the coupon screen just for one lookup.
function findFirstHalfScore(
  scores: FixtureDetail['scores'] | undefined,
): { home: number; away: number } | null {
  if (!scores) return null;
  const home = scores.find(
    (s) => s.description === '1ST_HALF' && s.participant_location === 'home',
  );
  const away = scores.find(
    (s) => s.description === '1ST_HALF' && s.participant_location === 'away',
  );
  if (home?.goals == null || away?.goals == null) return null;
  return { home: home.goals, away: away.goals };
}

function applyDateFilter(coupons: Coupon[], filter: DateFilter): Coupon[] {
  if (filter === 'all') return coupons;
  const now = new Date();
  const cutoff =
    filter === 'today'
      ? new Date(now.getFullYear(), now.getMonth(), now.getDate())
      : filter === 'week'
        ? subDays(now, 7)
        : subDays(now, 30);
  return coupons.filter((c) => parseISO(c.createdAt) >= cutoff);
}

interface CouponSection {
  title: string;
  data: Coupon[];
  count: number;
}

function buildSections(
  coupons: Coupon[],
  todayLabel: string,
  yesterdayLabel: string,
): CouponSection[] {
  // Group coupons by their createdAt calendar day so the user sees a
  // natural temporal scroll: today's batch up top, then yesterday, then
  // older days back through history.
  const groups = new Map<string, Coupon[]>();
  for (const coupon of coupons) {
    const key = format(parseISO(coupon.createdAt), 'yyyy-MM-dd');
    const list = groups.get(key);
    if (list) list.push(coupon);
    else groups.set(key, [coupon]);
  }
  return Array.from(groups.entries())
    .sort((a, b) => b[0].localeCompare(a[0]))
    .map(([key, list]) => {
      const date = parseISO(`${key}T00:00:00`);
      const title = isToday(date)
        ? todayLabel
        : isYesterday(date)
          ? yesterdayLabel
          : format(date, 'dd MMM yyyy');
      const data = [...list].sort((a, b) =>
        b.createdAt.localeCompare(a.createdAt),
      );
      return { title, data, count: data.length };
    });
}

export function CouponsScreen() {
  const c = useTheme();
  const { t } = useTranslation();
  const saved = useCouponStore((s) => s.saved);
  const hydrated = useCouponStore((s) => s.hydrated);
  // Guest users see a CTA where the stats card would otherwise sit —
  // the streak / calibration / market breakdown only make sense once
  // there's a history the user owns.
  const tier = useTier();
  // Settlement runs at the tab-shell level (app/(tabs)/_layout.tsx) so it's
  // active across every screen — no need to mount it here too.

  // Subscribe to the global live-ticker so per-fixture caches refresh as
  // soon as the backend pushes a state/score update. Only the fixtures
  // we're already fetching (via useFixtureLookup below) actually re-pull.
  useLiveTicker();

  // Pull fresh fixture state for every selection on every coupon. Pending
  // coupons need the live score; settled coupons need the finished score +
  // 1st-half score for the "MS / İY" badge. Cheap because useFixtureLookup
  // caches per id and only refetches when the screen is mounted.
  const liveFixtureIds = useMemo(() => {
    const ids = new Set<number>();
    for (const coupon of saved) {
      for (const sel of coupon.selections) ids.add(sel.fixtureId);
    }
    return Array.from(ids);
  }, [saved]);
  const { lookup: fixtureLookup } = useFixtureLookup(liveFixtureIds);

  // Controlled confirmation modal. Built in-app rather than using Alert.alert
  // because the platform Alert callback was unreliable on the user's device
  // — pressing "Sil" never reached deleteSavedCoupon.
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null);
  const pendingCoupon = pendingDeleteId
    ? saved.find((c) => c.id === pendingDeleteId) ?? null
    : null;

  const [dateFilter, setDateFilter] = useState<DateFilter>('all');
  const filteredCoupons = useMemo(
    () => applyDateFilter(saved, dateFilter),
    [saved, dateFilter],
  );
  const todayLabel = t('coupons.dateLabel.today');
  const yesterdayLabel = t('coupons.dateLabel.yesterday');
  const sections = useMemo(
    () => buildSections(filteredCoupons, todayLabel, yesterdayLabel),
    [filteredCoupons, todayLabel, yesterdayLabel],
  );

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.headerRow}>
        {/* Left spacer matches the right-side icon's footprint so the centred
            logo stays anchored to the screen midpoint as the user switches
            tabs. Without this the brand jumps left when this tab mounts. */}
        <View style={styles.headerSide} />
        <View style={styles.headerCenter}>
          <AppBrand />
        </View>
        <View style={styles.headerSide}>
          <MarketLegendButton />
        </View>
      </View>

      {hydrated && saved.length === 0 ? (
        <View style={styles.empty}>
          <View
            style={[
              styles.emptyIconCircle,
              { backgroundColor: c.brandSoft },
            ]}>
            <MaterialCommunityIcons
              name="basket-outline"
              size={40}
              color={c.brand}
            />
          </View>
          <ThemedText style={[styles.emptyTitle, { color: c.text }]}>
            {t('coupons.empty.title')}
          </ThemedText>
          <ThemedText style={[styles.emptyText, { color: c.textMuted }]}>
            {t('coupons.empty.body')}
          </ThemedText>
          <View style={styles.emptyHints}>
            <EmptyHint
              icon="chart-line"
              label={t('coupons.empty.hintAnalysisLabel')}
              text={t('coupons.empty.hintAnalysisText')}
            />
            <EmptyHint
              icon="basket-plus-outline"
              label={t('coupons.empty.hintDetailLabel')}
              text={t('coupons.empty.hintDetailText')}
            />
          </View>
        </View>
      ) : (
        <SectionList
          sections={sections}
          keyExtractor={(c) => c.id}
          renderItem={({ item }) => (
            <CouponCard
              coupon={item}
              fixtureLookup={fixtureLookup}
              onRequestDelete={() => setPendingDeleteId(item.id)}
            />
          )}
          renderSectionHeader={({ section }) => (
            <View
              style={[
                styles.sectionHeader,
                { backgroundColor: c.bg },
              ]}>
              <ThemedText
                style={[styles.sectionHeaderText, { color: c.textMuted }]}>
                {section.title.toLocaleUpperCase('tr-TR')}
              </ThemedText>
              <View
                style={[
                  styles.sectionCountBadge,
                  { backgroundColor: c.brandSoft },
                ]}>
                <ThemedText
                  style={[styles.sectionCountText, { color: c.brand }]}>
                  {section.count}
                </ThemedText>
              </View>
            </View>
          )}
          renderSectionFooter={() => <View style={styles.sectionFooter} />}
          stickySectionHeadersEnabled={false}
          ListHeaderComponent={
            <View>
              {tier === 'guest' ? (
                <GuestStatsCTA />
              ) : (
                <CouponStatsCard coupons={saved} />
              )}
              <View style={styles.dateFilterRow}>
                {DATE_FILTERS.map(({ key, i18nKey }) => {
                  const active = dateFilter === key;
                  return (
                    <Pressable
                      key={key}
                      onPress={() => setDateFilter(key)}
                      style={[
                        styles.dateFilterPill,
                        { borderColor: c.border },
                        active && {
                          backgroundColor: c.brand,
                          borderColor: c.brand,
                        },
                      ]}>
                      <ThemedText
                        style={[
                          styles.dateFilterText,
                          { color: active ? c.textInverse : c.textMuted },
                        ]}>
                        {t(i18nKey)}
                      </ThemedText>
                    </Pressable>
                  );
                })}
              </View>
            </View>
          }
          ListEmptyComponent={
            saved.length > 0 ? (
              <View style={styles.filterEmpty}>
                <MaterialCommunityIcons
                  name="filter-variant-remove"
                  size={28}
                  color={c.textMuted}
                />
                <ThemedText
                  style={[styles.filterEmptyText, { color: c.textMuted }]}>
                  {t('coupons.dateFilter.empty')}
                </ThemedText>
              </View>
            ) : null
          }
          ListFooterComponent={
            <>
              <AdBanner />
              <AppDisclaimerFooter />
            </>
          }
          contentContainerStyle={styles.list}
          extraData={`${pendingDeleteId}|${fixtureLookup.size}|${dateFilter}`}
        />
      )}

      <DeleteConfirmModal
        coupon={pendingCoupon}
        onCancel={() => setPendingDeleteId(null)}
        onConfirm={() => {
          if (pendingDeleteId) deleteSavedCoupon(pendingDeleteId);
          setPendingDeleteId(null);
        }}
      />
    </SafeAreaView>
  );
}

function DeleteConfirmModal({
  coupon,
  onCancel,
  onConfirm,
}: {
  coupon: Coupon | null;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  const c = useTheme();
  const { t } = useTranslation();
  return (
    <Modal
      visible={coupon != null}
      transparent
      animationType="fade"
      onRequestClose={onCancel}>
      <Pressable style={styles.modalBackdrop} onPress={onCancel}>
        <Pressable
          onPress={(e) => e.stopPropagation()}
          style={[
            styles.modalSheet,
            { backgroundColor: c.surface, borderColor: c.border },
          ]}>
          <ThemedText style={[styles.modalTitle, { color: c.text }]}>
            {t('coupons.delete.title')}
          </ThemedText>
          <ThemedText style={[styles.modalBody, { color: c.textMuted }]}>
            {coupon ? t('coupons.delete.body', { name: coupon.name }) : ''}
          </ThemedText>
          <View style={styles.modalActions}>
            <Pressable
              onPress={onCancel}
              accessibilityRole="button"
              accessibilityLabel={t('coupons.a11y.confirmCancel')}
              style={[styles.modalBtn, { borderColor: c.border }]}>
              <ThemedText style={[styles.modalBtnText, { color: c.text }]}>
                {t('coupons.delete.cancel')}
              </ThemedText>
            </Pressable>
            <Pressable
              onPress={onConfirm}
              accessibilityRole="button"
              accessibilityLabel={t('coupons.a11y.confirmDelete')}
              style={[
                styles.modalBtn,
                { backgroundColor: c.danger, borderColor: c.danger },
              ]}>
              <ThemedText style={[styles.modalBtnText, { color: '#fff' }]}>
                {t('coupons.delete.confirm')}
              </ThemedText>
            </Pressable>
          </View>
        </Pressable>
      </Pressable>
    </Modal>
  );
}

function EmptyHint({
  icon,
  label,
  text,
}: {
  icon: keyof typeof MaterialCommunityIcons.glyphMap;
  label: string;
  text: string;
}) {
  const c = useTheme();
  return (
    <View
      style={[
        styles.emptyHintCard,
        { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
      ]}>
      <MaterialCommunityIcons name={icon} size={18} color={c.brand} />
      <View style={styles.emptyHintText}>
        <ThemedText style={[styles.emptyHintLabel, { color: c.text }]}>
          {label}
        </ThemedText>
        <ThemedText style={[styles.emptyHintBody, { color: c.textMuted }]}>
          {text}
        </ThemedText>
      </View>
    </View>
  );
}

function CouponCard({
  coupon,
  fixtureLookup,
  onRequestDelete,
}: {
  coupon: Coupon;
  fixtureLookup: Map<number, FixtureDetail>;
  onRequestDelete: () => void;
}) {
  const c = useTheme();
  const { t } = useTranslation();
  // Cards default closed — users almost always want a quick scroll over
  // their history; expanding shows the full leg-by-leg breakdown.
  const [expanded, setExpanded] = useState(false);
  const created = format(parseISO(coupon.createdAt), 'dd.MM.yyyy HH:mm');
  const outcome = couponOutcome(coupon);
  // Delete is allowed while the coupon's outcome is still pending — that
  // covers everything from "no match started yet" to "some matches in
  // progress, none lost yet". Once won/lost it's a record we preserve.
  const deletable = outcome.state === 'pending';

  const statusLabel =
    outcome.state === 'won'
      ? t('coupons.status.won')
      : outcome.state === 'lost'
        ? t('coupons.status.lost')
        : t('coupons.status.pending');
  const statusAccent =
    outcome.state === 'won'
      ? c.success
      : outcome.state === 'lost'
        ? c.danger
        : c.brand;
  const statusBg =
    outcome.state === 'won'
      ? c.successSoft
      : outcome.state === 'lost'
        ? c.dangerSoft
        : c.brandSoft;
  const statusIcon: keyof typeof MaterialCommunityIcons.glyphMap =
    outcome.state === 'won'
      ? 'check-circle'
      : outcome.state === 'lost'
        ? 'close-circle'
        : 'progress-clock';

  return (
    <View
      style={[
        styles.card,
        c.shadowCard,
        {
          backgroundColor: c.surfaceElevated,
          borderColor: c.borderSoft,
        },
      ]}>
      {/* Coloured top edge — instant scan of status without reading the pill */}
      <View style={[styles.cardAccent, { backgroundColor: statusAccent }]} />
      <Pressable
        onPress={() => setExpanded((v) => !v)}
        accessibilityRole="button"
        accessibilityLabel={`${coupon.name}, ${statusLabel}`}
        accessibilityState={{ expanded }}
        style={({ pressed }) => [
          styles.cardHeader,
          pressed && { backgroundColor: c.brandSoft },
        ]}>
        <View style={styles.cardTitleBlock}>
          <ThemedText
            style={[styles.cardTitle, { color: c.text }]}
            numberOfLines={1}>
            {coupon.name}
          </ThemedText>
          <ThemedText style={[styles.cardSub, { color: c.textMuted }]}>
            {created} ·{' '}
            {t('coupons.summary.selectionsSettled', {
              count: coupon.selections.length,
              settled: outcome.settled,
            })}
          </ThemedText>
          <View
            style={[
              styles.statusPill,
              { backgroundColor: statusBg },
            ]}>
            <MaterialCommunityIcons
              name={statusIcon}
              size={12}
              color={statusAccent}
            />
            <ThemedText style={[styles.statusText, { color: statusAccent }]}>
              {statusLabel}
            </ThemedText>
          </View>
        </View>
        <MaterialCommunityIcons
          name={expanded ? 'chevron-up' : 'chevron-down'}
          size={22}
          color={c.textMuted}
          style={styles.chevron}
        />
      </Pressable>

      {expanded ? (
        <>
          <View style={[styles.actionsRow, { borderTopColor: c.border }]}>
            <Pressable
              onPress={() => shareCoupon(coupon)}
              hitSlop={8}
              accessibilityRole="button"
              accessibilityLabel={t('coupons.a11y.shareCoupon', { name: coupon.name })}
              style={({ pressed }) => [
                styles.actionBtn,
                { backgroundColor: pressed ? c.brandSoft : 'transparent' },
              ]}>
              <MaterialCommunityIcons
                name="share-variant"
                size={16}
                color={c.textMuted}
              />
              <ThemedText style={[styles.actionText, { color: c.textMuted }]}>
                {t('common.share').toLocaleUpperCase('tr-TR')}
              </ThemedText>
            </Pressable>
            {deletable ? (
              <Pressable
                onPress={onRequestDelete}
                hitSlop={8}
                accessibilityRole="button"
                accessibilityLabel={t('coupons.a11y.deleteCoupon', { name: coupon.name })}
                accessibilityHint={t('coupons.a11y.deleteCouponHint')}
                style={({ pressed }) => [
                  styles.actionBtn,
                  { backgroundColor: pressed ? c.dangerSoft : 'transparent' },
                ]}>
                <MaterialCommunityIcons
                  name="trash-can-outline"
                  size={16}
                  color={c.danger}
                />
                <ThemedText style={[styles.actionText, { color: c.danger }]}>
                  {t('common.delete').toLocaleUpperCase('tr-TR')}
                </ThemedText>
              </Pressable>
            ) : null}
          </View>
          <View style={styles.divider} />
        </>
      ) : null}
      {expanded
        ? coupon.selections.map((s) => {
        // Don't show win/loss until the fixture has actually started,
        // even if betWinning leaked through earlier from stale data.
        const started = selectionStarted(s);
        const fixture = fixtureLookup.get(s.fixtureId);
        const bucket = fixture
          ? getStateBucket(fixture.fixture.state_id)
          : null;
        const isLive = bucket === 'live';
        const isFinished = bucket === 'finished';
        // Treat anything that hasn't reached live / finished as upcoming
        // even when SportMonks leaks a betWinning flag in early — user
        // doesn't want pre-kickoff matches to render green/red.
        const isUpcoming = bucket === 'upcoming' || !started;
        const liveScore: LiveScore | null =
          (isLive || isFinished) &&
          fixture?.fixture.home_score != null &&
          fixture?.fixture.away_score != null
            ? {
                home: fixture.fixture.home_score,
                away: fixture.fixture.away_score,
              }
            : null;
        // While the match is live, colour the odd by what the bet *would*
        // do if the match ended right now. Final settled flag
        // (s.betWinning) wins over the live evaluation since it's
        // authoritative.
        const liveStatus =
          isLive && liveScore
            ? outcomeLiveStatus(
                {
                  market_id: s.marketId,
                  label: s.outcomeLabel,
                  total: s.total,
                  handicap: s.handicap,
                },
                liveScore,
              )
            : null;
        const won = !isUpcoming &&
          ((isFinished && s.betWinning === true) || liveStatus === 'win');
        const lost = !isUpcoming &&
          ((isFinished && s.betWinning === false) || liveStatus === 'loss');
        const settled = isFinished &&
          (s.betWinning === true || s.betWinning === false);
        // Live but no verdict yet → keep the clock so the user knows it's
        // still in motion even though the odd is already coloured.
        const iconName = settled
          ? won
            ? 'check-circle'
            : 'close-circle'
          : isLive
            ? 'progress-clock'
            : 'progress-clock';
        const iconColor = won
          ? c.success
          : lost
            ? c.danger
            : isLive
              ? c.live ?? c.brand
              : c.textMuted;
        // Score badge:
        //   live      → "67'" (the score is now inlined in the team line)
        //   finished  → "İY 1-0" (1st-half companion to the inlined FT)
        const firstHalf = isFinished
          ? findFirstHalfScore(fixture?.scores)
          : null;
        const liveBadge = isLive && liveScore && fixture?.fixture.live_minute != null
          ? `${fixture.fixture.live_minute}'`
          : isFinished && firstHalf
            ? `İY ${firstHalf.home}-${firstHalf.away}`
            : null;
        // Replace the " - " separator in the cached fixtureName with the
        // current score for live + finished matches so the score sits
        // between the two team names ("Home 2-1 Away") instead of a
        // dash placeholder. Fall back to the original text otherwise.
        const scoreSeparator =
          (isLive || isFinished) && liveScore
            ? ` ${liveScore.home}-${liveScore.away} `
            : null;
        const fixtureLine = scoreSeparator
          ? s.fixtureName.replace(' - ', scoreSeparator)
          : s.fixtureName;
        return (
          <View
            key={s.id}
            style={[styles.selectionRow, { borderTopColor: c.border }]}>
            <MaterialCommunityIcons
              name={iconName}
              size={18}
              color={iconColor}
              style={styles.statusIcon}
            />
            <View style={styles.selectionMain}>
              <View style={styles.selectionTopRow}>
                <ThemedText
                  style={[styles.fixtureLine, { color: c.text }]}
                  numberOfLines={1}>
                  {fixtureLine}
                </ThemedText>
                {liveBadge ? (
                  <ThemedText
                    style={[
                      styles.liveBadge,
                      // Live → orange-y `c.live`. Finished → muted text
                      // so the score reads as historical, not in-motion.
                      { color: isLive ? c.live ?? c.brand : c.textMuted },
                    ]}>
                    {liveBadge}
                  </ThemedText>
                ) : null}
              </View>
            </View>
            <View
              style={[
                styles.oddPill,
                {
                  backgroundColor: won
                    ? c.successSoft
                    : lost
                      ? c.dangerSoft
                      : 'transparent',
                  borderColor: won
                    ? c.success
                    : lost
                      ? c.danger
                      : 'transparent',
                },
              ]}>
              <ThemedText
                style={[
                  styles.selectionOdd,
                  {
                    color: won ? c.success : lost ? c.danger : c.text,
                    fontWeight: won || lost ? '800' : '700',
                  },
                ]}
                numberOfLines={1}>
                {`${s.marketShort} ${s.outcomeDisplay ?? s.outcomeLabel}`}
              </ThemedText>
            </View>
          </View>
        );
      })
        : null}
    </View>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 4,
  },
  headerCenter: {
    flex: 1,
    alignItems: 'center',
  },
  headerSide: {
    width: 36,
    alignItems: 'flex-end',
  },
  list: {
    paddingHorizontal: 12,
    paddingTop: 4,
    paddingBottom: 32,
  },
  // Compact, lowercase-friendly chip strip — sits between the stats card
  // and the first date section. The Filtre style mirrors the analysis tab
  // so the visual vocabulary stays consistent across the app.
  dateFilterRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 6,
    paddingHorizontal: 4,
    paddingTop: 4,
    paddingBottom: 8,
  },
  dateFilterPill: {
    paddingHorizontal: 11,
    paddingVertical: 5,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
  },
  dateFilterText: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
  // "BUGÜN · 3" style — small uppercase label + count badge. Acts as the
  // visual divider between groups so cards within a day stay tight while
  // groups feel meaningfully separated.
  sectionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 6,
    paddingTop: 22,
    paddingBottom: 10,
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
  sectionFooter: {
    height: 8,
  },
  filterEmpty: {
    paddingVertical: 36,
    alignItems: 'center',
    gap: 8,
  },
  filterEmptyText: {
    fontSize: 13,
    fontWeight: '600',
  },
  empty: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 28,
    gap: 14,
  },
  emptyIconCircle: {
    width: 88,
    height: 88,
    borderRadius: 44,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 4,
  },
  emptyTitle: {
    fontSize: 18,
    fontWeight: '800',
    letterSpacing: 0.2,
  },
  emptyText: {
    fontSize: 13,
    textAlign: 'center',
    lineHeight: 19,
    fontWeight: '500',
  },
  emptyHints: {
    flexDirection: 'row',
    gap: 10,
    marginTop: 12,
    width: '100%',
  },
  emptyHintCard: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderRadius: 12,
    borderWidth: StyleSheet.hairlineWidth,
  },
  emptyHintText: {
    flex: 1,
    gap: 1,
  },
  emptyHintLabel: {
    fontSize: 12,
    fontWeight: '700',
  },
  emptyHintBody: {
    fontSize: 10,
    fontWeight: '500',
    lineHeight: 13,
  },
  // Spacious gap between cards so each coupon stands on its own. Section
  // header padding above puts even more air between groups (different
  // days). Combined with the cards-default-collapsed pattern this keeps
  // long histories scannable without feeling like a wall of cards.
  card: {
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
    marginBottom: 18,
  },
  cardAccent: {
    height: 4,
  },
  cardHeader: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    paddingHorizontal: 14,
    paddingTop: 12,
    paddingBottom: 12,
    gap: 10,
  },
  cardTitleBlock: {
    flex: 1,
    gap: 4,
  },
  cardTitle: {
    fontSize: 15,
    fontWeight: '700',
    flexShrink: 1,
  },
  statusPill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 999,
    alignSelf: 'flex-start',
    marginTop: 2,
  },
  statusText: {
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
  cardSub: {
    fontSize: 11,
    fontWeight: '500',
  },
  deleteBtn: {
    width: 32,
    height: 32,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
  },
  // Chevron sits at the far right of the (collapsed) card header — it's
  // the visual cue that the row is expandable. Only renders inside the
  // header Pressable so the whole strip stays a single tap target.
  chevron: {
    alignSelf: 'center',
    marginLeft: 4,
  },
  // Action footer surfaces only when the card is expanded — keeps the
  // collapsed view clean and shifts the destructive Sil button below
  // the more-frequent Paylaş button so finger-stretch matches frequency.
  actionsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  actionBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 12,
    paddingVertical: 7,
    borderRadius: 999,
  },
  actionText: {
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 0.5,
  },
  divider: {
    height: StyleSheet.hairlineWidth,
    backgroundColor: 'transparent',
  },
  selectionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 14,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 10,
  },
  statusIcon: {
    width: 20,
  },
  selectionMain: {
    flex: 1,
    gap: 2,
  },
  selectionTopRow: {
    flexDirection: 'row',
    alignItems: 'baseline',
    gap: 8,
  },
  fixtureLine: {
    flex: 1,
    fontSize: 12,
    fontWeight: '600',
  },
  liveBadge: {
    fontSize: 11,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
    letterSpacing: 0.3,
  },
  oddPill: {
    paddingHorizontal: 9,
    paddingVertical: 3,
    borderRadius: 6,
    borderWidth: 1,
    minWidth: 50,
    alignItems: 'center',
  },
  selectionOdd: {
    fontSize: 13,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  modalBackdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.55)',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  modalSheet: {
    width: '100%',
    maxWidth: 360,
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 16,
    gap: 8,
  },
  modalTitle: {
    fontSize: 16,
    fontWeight: '700',
  },
  modalBody: {
    fontSize: 13,
    lineHeight: 18,
  },
  modalActions: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    gap: 8,
    paddingTop: 8,
  },
  modalBtn: {
    paddingHorizontal: 14,
    paddingVertical: 9,
    borderRadius: 8,
    borderWidth: 1,
  },
  modalBtnText: {
    fontSize: 12,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
});
