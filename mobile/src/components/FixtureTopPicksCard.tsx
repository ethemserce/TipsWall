import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTryAddSelection } from '@/src/hooks/useTryAddSelection';
import { useCouponStore } from '@/src/lib/coupons/store';
import { marketShort, shortenOutcome } from '@/src/lib/marketShort';
import { outcomeSentence } from '@/src/lib/marketSentence';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureOddsMarket } from '@/src/types/fixtureOdds';

interface Props {
  markets: FixtureOddsMarket[];
  fixtureId: number;
  fixtureName: string;
  startingAt: string | null;
  bookmakerId: number;
  // True when the match hasn't kicked off yet — when false, the CTA is shown
  // for context but the add buttons are disabled (no point picking a started
  // match into a coupon).
  upcoming: boolean;
}

interface Pick {
  marketId: number;
  marketName: string | null;
  outcomeLabel: string;
  outcomeDisplay: string;
  total: string | null;
  handicap: string | null;
  dso: number;
  iko: number;
  vbet: number | null;
  sampleCount: number;
}

const TOP_LIMIT = 2;
const MIN_SAMPLE = 5;

/**
 * Surfaces the strongest 1-2 value-bet outcomes for the fixture so the user
 * doesn't have to scroll through every market to spot them. "Strongest" =
 * largest gap between DSO (historical hit rate) and İKO (no-vig implied
 * probability), with a minimum sample size to avoid noise.
 */
export function FixtureTopPicksCard({
  markets,
  fixtureId,
  fixtureName,
  startingAt,
  bookmakerId,
  upcoming,
}: Props) {
  const c = useTheme();
  const { t } = useTranslation();
  // Re-renders whenever the draft changes so the CTA can flip between
  // "Ekle" / "Eklendi" / disabled (another pick on this fixture).
  const draftSelections = useCouponStore((s) => s.draft.selections);

  const fixtureAlreadyPicked = useMemo(
    () => draftSelections.some((s) => s.fixtureId === fixtureId),
    [draftSelections, fixtureId],
  );

  const picks = useMemo<Pick[]>(() => {
    const all: Pick[] = [];
    for (const market of markets) {
      for (const o of market.outcomes) {
        const sample = o.win_count + o.lost_count;
        if (sample < MIN_SAMPLE) continue;
        if (o.iko == null) continue;
        if (o.winning_percent == null) continue;
        if (o.winning_percent <= o.iko) continue;
        all.push({
          marketId: market.market_id,
          marketName: market.market_name,
          outcomeLabel: o.label,
          outcomeDisplay: shortenOutcome(o.label, market.market_id),
          total: o.total,
          handicap: o.handicap,
          dso: o.winning_percent,
          iko: o.iko,
          vbet: o.earning_percent,
          sampleCount: sample,
        });
      }
    }
    all.sort((a, b) => b.dso - b.iko - (a.dso - a.iko));
    return all.slice(0, TOP_LIMIT);
  }, [markets]);

  if (picks.length === 0) return null;

  return (
    <View
      style={[
        styles.card,
        c.shadowCard,
        {
          backgroundColor: c.surfaceElevated,
          borderColor: c.brand,
        },
      ]}>
      <View style={[styles.header, { backgroundColor: c.brandSoft }]}>
        <MaterialCommunityIcons
          name="star-four-points"
          size={15}
          color={c.brand}
        />
        <ThemedText style={[styles.title, { color: c.brand }]}>
          {picks.length > 1
            ? t('coupons.topPicks.headerMany')
            : t('coupons.topPicks.headerOne')}
        </ThemedText>
        <View style={[styles.headerBadge, { backgroundColor: c.brand }]}>
          <ThemedText style={[styles.headerBadgeText, { color: c.textInverse }]}>
            {t('coupons.topPicks.valueBadge')}
          </ThemedText>
        </View>
      </View>
      {picks.map((pick) => (
        <PickRow
          key={`${pick.marketId}-${pick.outcomeLabel}-${pick.total ?? ''}-${pick.handicap ?? ''}`}
          pick={pick}
          fixtureId={fixtureId}
          fixtureName={fixtureName}
          startingAt={startingAt}
          bookmakerId={bookmakerId}
          upcoming={upcoming}
          fixtureAlreadyPicked={fixtureAlreadyPicked}
        />
      ))}
    </View>
  );
}

function PickRow({
  pick,
  fixtureId,
  fixtureName,
  startingAt,
  bookmakerId,
  upcoming,
  fixtureAlreadyPicked,
}: {
  pick: Pick;
  fixtureId: number;
  fixtureName: string;
  startingAt: string | null;
  bookmakerId: number;
  upcoming: boolean;
  fixtureAlreadyPicked: boolean;
}) {
  const c = useTheme();
  const { t } = useTranslation();
  const tryAdd = useTryAddSelection();
  const draftSelections = useCouponStore((s) => s.draft.selections);
  const inCoupon = draftSelections.some(
    (s) =>
      s.fixtureId === fixtureId &&
      s.marketId === pick.marketId &&
      s.outcomeLabel.toLowerCase() === pick.outcomeLabel.toLowerCase() &&
      (s.total ?? '-') === (pick.total ?? '-') &&
      (s.handicap ?? '-') === (pick.handicap ?? '-'),
  );
  // Disabled: not upcoming, OR a different selection from this fixture is
  // already in the draft (one-pick-per-fixture rule).
  const disabled = !upcoming || (fixtureAlreadyPicked && !inCoupon);

  const handlePress = () => {
    if (disabled && !inCoupon) return;
    void tryAdd({
      fixtureId,
      fixtureName,
      startingAt,
      bookmakerId,
      marketId: pick.marketId,
      marketShort: marketShort(pick.marketId, pick.marketName),
      outcomeLabel: pick.outcomeLabel,
      outcomeDisplay: pick.outcomeDisplay,
      total: pick.total,
      handicap: pick.handicap,
      oddValue: 0,
      dso: pick.dso,
      vbet: pick.vbet,
      iko: pick.iko,
      sampleCount: pick.sampleCount,
    });
  };

  const buttonLabel = inCoupon
    ? t('coupons.topPicks.actionInBasket')
    : disabled
      ? t('coupons.topPicks.actionDisabled')
      : t('coupons.topPicks.actionAdd');

  // Only dim when the one-pick-per-fixture rule is the blocker. Finished
  // / live matches stay full-opacity so the tip text + stats remain
  // readable; the button itself still reflects the "KAPALI" state.
  const dim = fixtureAlreadyPicked && !inCoupon;
  return (
    <Pressable
      onPress={handlePress}
      disabled={disabled && !inCoupon}
      style={({ pressed }) => [
        styles.row,
        {
          backgroundColor: pressed ? c.bg : 'transparent',
          borderTopColor: c.border,
          opacity: dim ? 0.5 : 1,
        },
      ]}>
      <View style={styles.pickInfo}>
        <ThemedText style={[styles.pickTip, { color: c.brand }]} numberOfLines={2}>
          {outcomeSentence(t, pick.marketId, pick.outcomeLabel, pick.total, pick.handicap)}
        </ThemedText>
        <ThemedText style={[styles.pickStats, { color: c.textMuted }]}>
          {t('coupons.topPicks.stats', {
            dso: pick.dso.toFixed(0),
            iko: pick.iko.toFixed(0),
            sample: pick.sampleCount,
          })}
        </ThemedText>
      </View>
      <View
        style={[
          styles.btn,
          {
            backgroundColor: inCoupon ? c.brand : 'transparent',
            borderColor: c.brand,
          },
        ]}>
        <ThemedText
          style={[
            styles.btnText,
            { color: inCoupon ? c.textInverse : c.brand },
          ]}>
          {buttonLabel}
        </ThemedText>
      </View>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: 1.5,
    borderRadius: 14,
    overflow: 'hidden',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 12,
    paddingVertical: 9,
  },
  title: {
    flex: 1,
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
  headerBadge: {
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 999,
  },
  headerBadgeText: {
    fontSize: 9,
    fontWeight: '900',
    letterSpacing: 0.7,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  pickInfo: {
    flex: 1,
    gap: 2,
  },
  pickTip: {
    fontSize: 13,
    fontWeight: '800',
  },
  pickStats: {
    fontSize: 10,
    fontVariant: ['tabular-nums'],
  },
  btn: {
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 8,
    borderWidth: 1,
  },
  btnText: {
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
});
