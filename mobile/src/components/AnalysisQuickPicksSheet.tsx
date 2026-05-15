import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { router } from 'expo-router';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  FlatList,
  Modal,
  Pressable,
  StyleSheet,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { useFixtureLookup } from '@/src/hooks/useFixtureLookup';
import { useMarketPreferences } from '@/src/hooks/useMarketPreferences';
import { useSignals } from '@/src/hooks/useSignals';
import { useTryAddSelection } from '@/src/hooks/useTryAddSelection';
import { isInDraft } from '@/src/lib/coupons/store';
import { MARKET_SHORT } from '@/src/lib/marketShort';
import { useTheme } from '@/src/lib/useTheme';
import type { RateResult } from '@/src/types/rateResult';

interface QuickPicksSheetProps {
  visible: boolean;
  onClose: () => void;
  selectedDate: Date;
  bookmakerId: number;
}

const TOP_N = 25;
const VALUE_MIN_HIT = 30;
const MIN_SAMPLE = 5;

/**
 * "Günün önerileri" quick-picks bottom sheet for the Analiz screen.
 * Pulls the highest-confidence picks for the selected day across the
 * user's favourite-market filter, one per fixture, sorted by Wilson
 * confidence. Tapping a row adds it to the coupon draft — same code
 * path as RateMatchCard so the quota/limit rules apply uniformly.
 *
 * Style + animation mirrors AnalysisFiltersSheet so the two share a
 * visual vocabulary; user already knows the close interaction.
 */
export function AnalysisQuickPicksSheet({
  visible,
  onClose,
  selectedDate,
  bookmakerId,
}: QuickPicksSheetProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const { marketIds: favouriteMarketIds } = useMarketPreferences();

  const fixtureDate = useMemo(() => {
    const y = selectedDate.getFullYear();
    const m = String(selectedDate.getMonth() + 1).padStart(2, '0');
    const d = String(selectedDate.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }, [selectedDate]);

  const signalsQuery = useSignals(
    visible
      ? {
          bookmakerId,
          fixtureDate,
          sort: 'confidence',
          sortDir: 'desc',
          // One pick per fixture so the list reads as a curated 20-row
          // catalogue, not 5× the same Real Madrid match.
          topPerFixture: 1,
          minWinningPercent: VALUE_MIN_HIT,
          minSampleCount: MIN_SAMPLE,
          perPage: TOP_N,
          marketIds:
            favouriteMarketIds.length > 0 ? favouriteMarketIds : undefined,
        }
      : {},
  );

  const items: RateResult[] = visible ? signalsQuery.data?.data.items ?? [] : [];
  const fixtureIds = useMemo(
    () => Array.from(new Set(items.map((r) => r.fixture_id))),
    [items],
  );
  const { lookup: fixtureLookup } = useFixtureLookup(fixtureIds);

  return (
    <Modal
      visible={visible}
      transparent
      animationType="slide"
      onRequestClose={onClose}>
      <Pressable style={styles.backdrop} onPress={onClose}>
        <Pressable
          onPress={(e) => e.stopPropagation()}
          style={[
            styles.sheet,
            {
              backgroundColor: c.surface,
              borderColor: c.border,
              paddingBottom: Math.max(12, insets.bottom + 8),
            },
          ]}>
          <View style={styles.handle}>
            <View style={[styles.handleBar, { backgroundColor: c.border }]} />
          </View>
          <View style={styles.header}>
            <MaterialCommunityIcons name="flash" size={18} color={c.brand} />
            <ThemedText style={[styles.title, { color: c.text }]}>
              {t('analysis.quickPicks.title', { defaultValue: 'Günün Önerileri' })}
            </ThemedText>
            <View style={{ flex: 1 }} />
            <Pressable onPress={onClose} hitSlop={10}>
              <MaterialCommunityIcons name="close" size={22} color={c.textMuted} />
            </Pressable>
          </View>

          {signalsQuery.isLoading ? (
            <View style={styles.center}>
              <ActivityIndicator color={c.brand} />
            </View>
          ) : items.length === 0 ? (
            <View style={styles.center}>
              <ThemedText style={[styles.emptyText, { color: c.textMuted }]}>
                {t('analysis.quickPicks.empty', {
                  defaultValue: 'Bugün için filtrelerine uyan bir öneri yok.',
                })}
              </ThemedText>
            </View>
          ) : (
            <FlatList
              data={items}
              keyExtractor={(r, idx) =>
                `${r.fixture_id}-${r.market_id}-${r.label}-${idx}`
              }
              ItemSeparatorComponent={() => (
                <View
                  style={[
                    styles.separator,
                    { backgroundColor: c.borderSoft },
                  ]}
                />
              )}
              renderItem={({ item }) => {
                const fx = fixtureLookup.get(item.fixture_id)?.fixture;
                return (
                  <QuickPickRow
                    signal={item}
                    homeName={fx?.home_team_name ?? null}
                    awayName={fx?.away_team_name ?? null}
                    marketShort={MARKET_SHORT[item.market_id] ?? `M${item.market_id}`}
                    onAfterAdd={onClose}
                  />
                );
              }}
              contentContainerStyle={styles.listContent}
            />
          )}
        </Pressable>
      </Pressable>
    </Modal>
  );
}

function QuickPickRow({
  signal,
  homeName,
  awayName,
  marketShort,
  onAfterAdd,
}: {
  signal: RateResult;
  homeName: string | null;
  awayName: string | null;
  marketShort: string;
  onAfterAdd: () => void;
}) {
  const c = useTheme();
  const tryAdd = useTryAddSelection();
  // oddValue 0 matches the placeholder coupon picks elsewhere store
  // (the no-betting-framing keeps the actual odd hidden).
  const inDraft = isInDraft({
    fixtureId: signal.fixture_id,
    marketId: signal.market_id,
    outcomeLabel: signal.label,
    total: signal.total ?? null,
    handicap: signal.handicap ?? null,
    oddValue: 0,
  });

  const teams = homeName && awayName
    ? `${homeName} - ${awayName}`
    : `Maç #${signal.fixture_id}`;
  const label = `${marketShort} · ${signal.label}`;
  const hit = signal.winning_percent != null ? Math.round(signal.winning_percent) : null;
  const imp = signal.iko != null ? Math.round(signal.iko) : null;

  const handle = async () => {
    const res = await tryAdd({
      fixtureId: signal.fixture_id,
      fixtureName: teams,
      startingAt: null,
      bookmakerId: 2,
      marketId: signal.market_id,
      marketShort,
      outcomeLabel: signal.label,
      outcomeDisplay: signal.label,
      total: signal.total ?? null,
      handicap: signal.handicap ?? null,
      oddValue: 0,
      dso: signal.winning_percent ?? null,
      vbet: signal.earning_percent ?? null,
      iko: signal.iko ?? null,
      sampleCount: signal.sample_count ?? null,
    });
    if (res.ok) onAfterAdd();
  };

  return (
    <Pressable
      onPress={handle}
      style={({ pressed }) => [
        styles.row,
        pressed && { backgroundColor: c.brandSoft },
      ]}>
      <View style={styles.rowInfo}>
        <ThemedText
          style={[styles.rowTeams, { color: c.text }]}
          numberOfLines={1}>
          {teams}
        </ThemedText>
        <ThemedText
          style={[styles.rowTip, { color: c.textMuted }]}
          numberOfLines={1}>
          {label}
        </ThemedText>
      </View>
      <View style={styles.rowStats}>
        {hit != null ? (
          <View style={[styles.stat, { backgroundColor: c.brandSoft }]}>
            <ThemedText style={[styles.statText, { color: c.brand }]}>
              {`HIT ${hit}%`}
            </ThemedText>
          </View>
        ) : null}
        {imp != null ? (
          <ThemedText style={[styles.impText, { color: c.textMuted }]}>
            {`IMP ${imp}%`}
          </ThemedText>
        ) : null}
      </View>
      <MaterialCommunityIcons
        name={inDraft ? 'check-circle' : 'plus-circle-outline'}
        size={22}
        color={inDraft ? c.success : c.brand}
        style={styles.rowAction}
      />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.55)',
    justifyContent: 'flex-end',
  },
  sheet: {
    maxHeight: '85%',
    borderTopLeftRadius: 16,
    borderTopRightRadius: 16,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  handle: { alignItems: 'center', paddingTop: 6, paddingBottom: 4 },
  handleBar: { width: 36, height: 3, borderRadius: 2 },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 10,
  },
  title: { fontSize: 14, fontWeight: '800' },
  listContent: { paddingBottom: 16 },
  separator: { height: StyleSheet.hairlineWidth, marginHorizontal: 14 },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingHorizontal: 14,
    paddingVertical: 10,
  },
  rowInfo: { flex: 1, gap: 2 },
  rowTeams: { fontSize: 13, fontWeight: '700' },
  rowTip: { fontSize: 11, fontWeight: '600' },
  rowStats: { alignItems: 'flex-end', gap: 2 },
  stat: {
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 6,
  },
  statText: { fontSize: 10, fontWeight: '800' },
  impText: { fontSize: 10, fontWeight: '600' },
  rowAction: { marginLeft: 4 },
  center: {
    paddingVertical: 32,
    alignItems: 'center',
    justifyContent: 'center',
  },
  emptyText: { fontSize: 13, textAlign: 'center' },
});
