import { useMemo } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { CircularGauge } from '@/src/components/CircularGauge';
import { MarketInfoButton } from '@/src/components/MarketInfoButton';
import { toggleSelection, useCouponStore } from '@/src/lib/coupons/store';
import { outcomeLiveStatus, type LiveScore } from '@/src/lib/liveOutcome';
import { marketShort, shortenOutcome } from '@/src/lib/marketShort';
import { useTheme } from '@/src/lib/useTheme';
import type {
  FixtureOddOutcome,
  FixtureOddsMarket,
} from '@/src/types/fixtureOdds';

interface OddsRatesCardProps {
  market: FixtureOddsMarket;
  // Coupon context — required so a tap on the odd cell can stash the right
  // fixture metadata into the basket. The detail screen passes these from
  // the loaded fixture.
  fixtureId: number;
  fixtureName: string;
  startingAt: string | null;
  bookmakerId: number;
  // Once the fixture has any score (live or final), each outcome is
  // coloured based on whether it would currently settle as a winner.
  // Null while the match hasn't kicked off.
  liveScore?: LiveScore | null;
}

// Softened win/loss tones — easier on the eye than the saturated 500-series
// greens/reds we were using before.
const WIN_COLOR = '#4ade80';
const LOSS_COLOR = '#f87171';

const VBET_COLOR = '#f59e0b'; // amber
const DSO_COLOR = '#22c55e'; // green
const IKO_COLOR = '#3b82f6'; // blue

export function OddsRatesCard({
  market,
  fixtureId,
  fixtureName,
  startingAt,
  bookmakerId,
  liveScore,
}: OddsRatesCardProps) {
  const c = useTheme();

  // Track which outcomes from this market are already in the draft so the
  // tip cell can render a "selected" state.
  const draftSelections = useCouponStore((s) => s.draft.selections);
  const draftKeys = useMemo(() => {
    return new Set(
      draftSelections.map((sel) =>
        [
          sel.fixtureId,
          sel.marketId,
          sel.outcomeLabel.toLowerCase(),
          sel.total ?? '-',
          sel.handicap ?? '-',
        ].join('|'),
      ),
    );
  }, [draftSelections]);

  // True if any selection on this fixture is already in the draft. With the
  // one-pick-per-fixture rule, that locks out every other outcome on this
  // fixture until the existing pick is removed.
  const fixtureTaken = useMemo(
    () => draftSelections.some((s) => s.fixtureId === fixtureId),
    [draftSelections, fixtureId],
  );

  // Coupon picks are only allowed for not-yet-started matches.
  // liveScore is set by the parent only when match is live OR finished, so
  // its presence equals "match is no longer upcoming".
  const couponLocked = liveScore != null;

  if (market.outcomes.length === 0) return null;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <View style={styles.titleRow}>
        <ThemedText
          style={[styles.title, { color: c.textMuted }]}
          numberOfLines={1}>
          {(market.market_name ?? `MARKET #${market.market_id}`).toUpperCase()}
        </ThemedText>
        <MarketInfoButton
          marketId={market.market_id}
          fallbackName={market.market_name}
        />
      </View>

      <View style={[styles.headerRow, { borderTopColor: c.border }]}>
        <ThemedText
          style={[
            styles.headerCell,
            styles.cellLabel,
            styles.headerLabelLeft,
            { color: c.textMuted },
          ]}>
          TAHMİN
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellGauge, { color: c.textMuted }]}>
          ROI
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellGauge, { color: c.textMuted }]}>
          HIT
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellGauge, { color: c.textMuted }]}>
          IMP
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNarrow, { color: c.textMuted }]}>
          W
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNarrow, { color: c.textMuted }]}>
          L
        </ThemedText>
      </View>

      {market.outcomes.map((outcome) => {
        const iko = outcome.iko;
        const sample = outcome.win_count + outcome.lost_count;
        const hasSample = sample > 0;
        // Value bet = DSO > İKO, i.e. historical hit rate beats the
        // bookmaker's no-vig probability. Only meaningful with sample data.
        const isValueBet =
          hasSample &&
          outcome.winning_percent != null &&
          iko != null &&
          outcome.winning_percent > iko;
        // Colours only apply once the match is in-play or finished — gated by
        // the presence of liveScore. For upcoming fixtures stale `winning`
        // flags from prematch_odds_current are ignored.
        const live = liveScore
          ? outcomeLiveStatus(
              { market_id: market.market_id, label: outcome.label, total: outcome.total, handicap: outcome.handicap },
              liveScore,
            )
          : null;
        const settled =
          liveScore && outcome.winning === true
            ? 'win'
            : liveScore && outcome.winning === false
              ? 'loss'
              : null;
        const status = live ?? settled;
        const liveColor =
          status === 'win' ? WIN_COLOR : status === 'loss' ? LOSS_COLOR : null;
        // Coupon membership for this row — keyed by the tip identity, not the
        // odd value (which we no longer surface).
        const oddKey = [
          fixtureId,
          market.market_id,
          outcome.label.toLowerCase(),
          outcome.total ?? '-',
          outcome.handicap ?? '-',
        ].join('|');
        const inCoupon = draftKeys.has(oddKey);
        // One pick per fixture — any other outcome here is blocked while a
        // selection from this fixture is in the draft. Removing the existing
        // selection re-opens all of them.
        const tapDisabled = couponLocked || (fixtureTaken && !inCoupon);
        const handleAddToCoupon = () => {
          if (tapDisabled) return;
          const rawLabel = outcome.label || '';
          toggleSelection({
            fixtureId,
            fixtureName,
            startingAt,
            bookmakerId,
            marketId: market.market_id,
            marketShort: marketShort(market.market_id, market.market_name),
            outcomeLabel: rawLabel,
            outcomeDisplay: shortenOutcome(rawLabel, market.market_id),
            total: outcome.total,
            handicap: outcome.handicap,
            oddValue: 0,
            dso: outcome.winning_percent,
            vbet: outcome.earning_percent,
            iko,
            sampleCount: hasSample ? sample : null,
          });
        };

        return (
          <View
            key={`${outcome.label}-${outcome.total ?? ''}-${outcome.handicap ?? ''}`}
            style={[
              styles.row,
              { borderTopColor: c.border },
              // Value-bet rows get a left accent bar, faint brand tint, and
              // a 2px brand bottom border — three signals together so the eye
              // catches them while scanning the card.
              isValueBet && {
                backgroundColor: 'rgba(58, 143, 111, 0.10)',
                borderBottomWidth: 2,
                borderBottomColor: c.brand,
              },
            ]}>
            {isValueBet ? (
              <View style={[styles.valueAccent, { backgroundColor: c.brand }]} />
            ) : null}
            {/* Tip cell carries the coupon-toggle now (was the odd cell). */}
            <Pressable
              onPress={handleAddToCoupon}
              disabled={tapDisabled && !inCoupon}
              accessibilityRole="button"
              accessibilityLabel={
                inCoupon
                  ? `${formatLabel(outcome)} sepetten çıkar`
                  : tapDisabled
                    ? `${formatLabel(outcome)} eklenemez`
                    : `${formatLabel(outcome)} sepete ekle`
              }
              accessibilityState={{ selected: inCoupon, disabled: tapDisabled && !inCoupon }}
              style={[
                styles.cellLabel,
                styles.tipPressable,
                inCoupon && { backgroundColor: c.brandSoft, borderColor: c.brand },
                tapDisabled && !inCoupon && { opacity: 0.4 },
              ]}>
              <ThemedText
                style={[
                  styles.cell,
                  {
                    color:
                      liveColor ??
                      (inCoupon ? c.brand : isValueBet ? c.brand : c.text),
                    fontWeight: inCoupon || isValueBet || liveColor ? '700' : '500',
                  },
                ]}
                numberOfLines={2}>
                {inCoupon ? '✓ ' : isValueBet ? '★ ' : ''}
                {formatLabel(outcome)}
              </ThemedText>
            </Pressable>
            <View style={styles.cellGauge}>
              <CircularGauge
                value={hasSample ? outcome.earning_percent : null}
                color={VBET_COLOR}
              />
            </View>
            <View style={styles.cellGauge}>
              <CircularGauge
                value={hasSample ? outcome.winning_percent : null}
                color={DSO_COLOR}
              />
            </View>
            <View style={styles.cellGauge}>
              <CircularGauge value={iko} color={IKO_COLOR} />
            </View>
            <ThemedText
              style={[styles.cell, styles.cellNarrow, styles.numberValue, { color: c.textMuted }]}>
              {hasSample ? outcome.win_count : '-'}
            </ThemedText>
            <ThemedText
              style={[styles.cell, styles.cellNarrow, styles.numberValue, { color: c.textMuted }]}>
              {hasSample ? outcome.lost_count : '-'}
            </ThemedText>
          </View>
        );
      })}
    </View>
  );
}

function formatLabel(o: FixtureOddOutcome): string {
  const suffix = o.total ?? o.handicap ?? null;
  return suffix ? `${o.label} ${suffix}` : o.label;
}


const styles = StyleSheet.create({
  card: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    overflow: 'hidden',
  },
  titleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 14,
    paddingTop: 8,
    paddingBottom: 6,
  },
  title: {
    flexShrink: 1,
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 6,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  headerCell: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.4,
    textAlign: 'center',
  },
  headerLabelLeft: {
    textAlign: 'left',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 8,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 2,
  },
  cell: {
    fontSize: 12,
  },
  cellLabel: {
    flex: 2.4,
    fontWeight: '500',
    paddingLeft: 6,
  },
  // Visual treatment for the tip cell when it's a tap target. Subtle border
  // hints at affordance without competing with the value-bet ★ row accent.
  tipPressable: {
    paddingVertical: 6,
    paddingHorizontal: 8,
    borderRadius: 6,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: 'transparent',
    justifyContent: 'center',
  },
  valueAccent: {
    position: 'absolute',
    left: 0,
    top: 0,
    bottom: 0,
    width: 4,
  },
  cellGauge: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  cellNarrow: {
    flex: 0.5,
    textAlign: 'center',
  },
  numberValue: {
    fontVariant: ['tabular-nums'],
    fontWeight: '600',
  },
});
