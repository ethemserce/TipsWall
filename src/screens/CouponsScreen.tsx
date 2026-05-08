import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { format, parseISO } from 'date-fns';
import { useMemo, useState } from 'react';
import { FlatList, Modal, Pressable, StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { AppBrand } from '@/src/components/AppBrand';
import { CouponStatsCard } from '@/src/components/CouponStatsCard';
import { MarketLegendButton } from '@/src/components/MarketLegendButton';
import { useFixtureLookup } from '@/src/hooks/useFixtureLookup';
import { useLiveTicker } from '@/src/hooks/useLiveTicker';
import {
  couponOutcome,
  deleteSavedCoupon,
  selectionStarted,
  totalOdd,
  useCouponStore,
} from '@/src/lib/coupons/store';
import type { Coupon } from '@/src/lib/coupons/types';
import { getStateBucket } from '@/src/lib/fixtureState';
import { outcomeLiveStatus, type LiveScore } from '@/src/lib/liveOutcome';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureDetail } from '@/src/types/fixtureDetail';


export function CouponsScreen() {
  const c = useTheme();
  const saved = useCouponStore((s) => s.saved);
  const hydrated = useCouponStore((s) => s.hydrated);
  // Settlement runs at the tab-shell level (app/(tabs)/_layout.tsx) so it's
  // active across every screen — no need to mount it here too.

  // Subscribe to the global live-ticker so per-fixture caches refresh as
  // soon as the backend pushes a state/score update. Only the fixtures
  // we're already fetching (via useFixtureLookup below) actually re-pull.
  useLiveTicker();

  // Pull fresh fixture state for every selection in a coupon that's still
  // pending. Settled coupons (won/lost) don't need live tracking — their
  // outcome is final regardless of score updates.
  const liveFixtureIds = useMemo(() => {
    const ids = new Set<number>();
    for (const coupon of saved) {
      if (couponOutcome(coupon).state !== 'pending') continue;
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
            Henüz kupon yok
          </ThemedText>
          <ThemedText style={[styles.emptyText, { color: c.textMuted }]}>
            Bir oranın üstüne dokunarak sepete ekle. Sepet dolunca buradan
            kaydedip takip edebilirsin.
          </ThemedText>
          <View style={styles.emptyHints}>
            <EmptyHint
              icon="chart-line"
              label="Analiz"
              text="DSO/VBET sıralı oranlar"
            />
            <EmptyHint
              icon="basket-plus-outline"
              label="Detay"
              text="Maç sayfasında öneriler"
            />
          </View>
        </View>
      ) : (
        <FlatList
          data={saved}
          keyExtractor={(c) => c.id}
          renderItem={({ item }) => (
            <CouponCard
              coupon={item}
              fixtureLookup={fixtureLookup}
              onRequestDelete={() => setPendingDeleteId(item.id)}
            />
          )}
          ListHeaderComponent={<CouponStatsCard coupons={saved} />}
          contentContainerStyle={styles.list}
          extraData={`${pendingDeleteId}|${fixtureLookup.size}`}
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
            Kuponu sil
          </ThemedText>
          <ThemedText style={[styles.modalBody, { color: c.textMuted }]}>
            {coupon
              ? `"${coupon.name}" silinsin mi? Bu işlem geri alınamaz.`
              : ''}
          </ThemedText>
          <View style={styles.modalActions}>
            <Pressable
              onPress={onCancel}
              style={[styles.modalBtn, { borderColor: c.border }]}>
              <ThemedText style={[styles.modalBtnText, { color: c.text }]}>
                İPTAL
              </ThemedText>
            </Pressable>
            <Pressable
              onPress={onConfirm}
              style={[
                styles.modalBtn,
                { backgroundColor: c.danger, borderColor: c.danger },
              ]}>
              <ThemedText style={[styles.modalBtnText, { color: '#fff' }]}>
                SİL
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
  const created = format(parseISO(coupon.createdAt), 'dd.MM.yyyy HH:mm');
  const odd = totalOdd(coupon).toFixed(2);
  const outcome = couponOutcome(coupon);
  // Delete is allowed while the coupon's outcome is still pending — that
  // covers everything from "no match started yet" to "some matches in
  // progress, none lost yet". Once won/lost it's a record we preserve.
  const deletable = outcome.state === 'pending';

  const statusLabel =
    outcome.state === 'won'
      ? 'KAZANDI'
      : outcome.state === 'lost'
        ? 'KAYBETTİ'
        : 'BEKLEMEDE';
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
      <View style={styles.cardHeader}>
        <View style={styles.cardTitleBlock}>
          <ThemedText
            style={[styles.cardTitle, { color: c.text }]}
            numberOfLines={1}>
            {coupon.name}
          </ThemedText>
          <ThemedText style={[styles.cardSub, { color: c.textMuted }]}>
            {created} · {coupon.selections.length} seçim ·{' '}
            {outcome.settled}/{coupon.selections.length} sonuçlandı
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
        <View style={styles.cardOdds}>
          <ThemedText style={[styles.oddLabel, { color: c.textMuted }]}>
            TOPLAM
          </ThemedText>
          <ThemedText
            style={[
              styles.oddValue,
              {
                color:
                  outcome.state === 'won'
                    ? c.success
                    : outcome.state === 'lost'
                      ? c.danger
                      : c.brand,
              },
            ]}>
            {odd}
          </ThemedText>
        </View>
        {deletable ? (
          <Pressable
            onPress={onRequestDelete}
            hitSlop={12}
            style={({ pressed }) => [
              styles.deleteBtn,
              { backgroundColor: pressed ? c.dangerSoft : 'transparent' },
            ]}>
            <MaterialCommunityIcons
              name="trash-can-outline"
              size={20}
              color={c.danger}
            />
          </Pressable>
        ) : null}
      </View>
      <View style={styles.divider} />
      {coupon.selections.map((s) => {
        // Don't show win/loss until the fixture has actually started,
        // even if betWinning leaked through earlier from stale data.
        const started = selectionStarted(s);
        const fixture = fixtureLookup.get(s.fixtureId);
        const bucket = fixture
          ? getStateBucket(fixture.fixture.state_id)
          : null;
        const isLive = bucket === 'live';
        const liveScore: LiveScore | null =
          (isLive || bucket === 'finished') &&
          fixture?.fixture.home_score != null &&
          fixture?.fixture.away_score != null
            ? {
                home: fixture.fixture.home_score,
                away: fixture.fixture.away_score,
              }
            : null;
        // While the match is live, color the odd by what the bet *would* do
        // if the match ended right now. Final settled flag (s.betWinning)
        // wins over live evaluation since it's authoritative.
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
        const won =
          (started && s.betWinning === true) || liveStatus === 'win';
        const lost =
          (started && s.betWinning === false) || liveStatus === 'loss';
        const settled = s.betWinning === true || s.betWinning === false;
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
        const liveBadge =
          isLive && liveScore
            ? `${liveScore.home}-${liveScore.away}${
                fixture?.fixture.live_minute != null
                  ? ` · ${fixture.fixture.live_minute}'`
                  : ''
              }`
            : null;
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
                  {s.fixtureName}
                </ThemedText>
                {liveBadge ? (
                  <ThemedText
                    style={[
                      styles.liveBadge,
                      { color: c.live ?? c.brand },
                    ]}>
                    {liveBadge}
                  </ThemedText>
                ) : null}
              </View>
              <ThemedText style={[styles.tipLine, { color: c.brand }]}>
                {s.marketShort} {s.outcomeDisplay ?? s.outcomeLabel}
              </ThemedText>
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
                ]}>
                {s.oddValue.toFixed(2)}
              </ThemedText>
            </View>
          </View>
        );
      })}
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
    padding: 12,
    gap: 12,
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
  card: {
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
    marginBottom: 14,
  },
  cardAccent: {
    height: 3,
  },
  cardHeader: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    paddingHorizontal: 14,
    paddingTop: 12,
    paddingBottom: 12,
    gap: 12,
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
  cardOdds: {
    alignItems: 'flex-end',
    gap: 1,
  },
  oddLabel: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.6,
  },
  oddValue: {
    fontSize: 18,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  deleteBtn: {
    width: 32,
    height: 32,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
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
  tipLine: {
    fontSize: 11,
    fontWeight: '700',
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
