import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { CircularGauge } from '@/src/components/CircularGauge';
import { MarketInfoButton } from '@/src/components/MarketInfoButton';
import { useTryAddSelection } from '@/src/hooks/useTryAddSelection';
import { useCouponStore } from '@/src/lib/coupons/store';
import { outcomeLiveStatus, type LiveScore } from '@/src/lib/liveOutcome';
import { marketLongName, marketShort, shortenOutcome } from '@/src/lib/marketShort';
import { useOddsHidden } from '@/src/lib/settings/settingsStore';
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
  // Team names — needed so shortenOutcome can collapse SportMonks's
  // team-specific Double Chance labels ("Arka Gdynia or Draw") into
  // the canonical 1X / 12 / X2 codes. Optional; if missing the helper
  // falls back to the raw label.
  homeName?: string | null;
  awayName?: string | null;
  // Once the fixture has any score (live or final), each outcome is
  // coloured based on whether it would currently settle as a winner.
  // Null while the match hasn't kicked off.
  liveScore?: LiveScore | null;
  // Starting collapsed state. The odds tab passes `false` for the first
  // market (so the screen isn't a wall of chevrons) and `true` for the
  // rest, but the user can toggle either way.
  initiallyCollapsed?: boolean;
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
  homeName,
  awayName,
  liveScore,
  initiallyCollapsed = false,
}: OddsRatesCardProps) {
  const c = useTheme();
  const { t, i18n } = useTranslation();
  const tryAdd = useTryAddSelection();
  const [collapsed, setCollapsed] = useState(initiallyCollapsed);
  const oddsHidden = useOddsHidden();
  const showOdd = !oddsHidden;

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

  // Group Over/Under markets by total — for market 80 (Goals O/U) and
  // similar (53 = 2nd Half, 28 = 1st Half), SportMonks ships 18 totals
  // (0.5/1.25/1.5/.../5.5/6.5) all with the same label ("Over"/"Under").
  // Rendering them as a flat list of 36 rows is unreadable. Instead we
  // sort by (total ASC, label) so each total's Over+Under appear as a
  // consecutive pair, and insert a thin divider showing the total value
  // between groups. The label cell drops the total suffix since the
  // divider now carries that information.
  const isPairedTotalsMarket = useMemo(
    () => market.outcomes.some((o) => o.total != null),
    [market.outcomes],
  );
  const sortedOutcomes = useMemo(() => {
    if (!isPairedTotalsMarket) return market.outcomes;
    return [...market.outcomes]
      // Hide stray total=NULL Over/Under rows that arrive via the embedded
      // include=odds path (writer leaves total blank when SportMonks does).
      // Their alt-line counterparts already render the proper pair —
      // showing the NULL row creates a header-less ghost group.
      .filter((o) => o.total != null && o.total !== '')
      .sort((a, b) => {
        const ta = parseFloat(a.total ?? '0');
        const tb = parseFloat(b.total ?? '0');
        if (Number.isFinite(ta) && Number.isFinite(tb) && ta !== tb) return ta - tb;
        // Within the same total, Over sorts before Under (rough alphabetical
        // works for both EN "Over/Under" and bookmaker localizations since
        // we canonicalise label upstream in the writer's OutcomeKey).
        return (a.label ?? '').localeCompare(b.label ?? '');
      });
  }, [market.outcomes, isPairedTotalsMarket]);

  if (market.outcomes.length === 0) return null;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <Pressable
        onPress={() => setCollapsed((v) => !v)}
        style={styles.titleRow}
        hitSlop={4}>
        <ThemedText
          style={[styles.title, { color: c.textMuted }]}
          numberOfLines={1}>
          {marketLongName(market.market_id, i18n.language, market.market_name).toLocaleUpperCase(i18n.language === 'tr' ? 'tr-TR' : 'en-US')}
        </ThemedText>
        <View style={styles.titleActions}>
          <MarketInfoButton
            marketId={market.market_id}
            fallbackName={market.market_name}
          />
          <MaterialCommunityIcons
            name={collapsed ? 'chevron-down' : 'chevron-up'}
            size={20}
            color={c.textMuted}
          />
        </View>
      </Pressable>

      {collapsed ? null : (
      <>
      <View style={[styles.headerRow, { borderTopColor: c.border }]}>
        <ThemedText
          style={[
            styles.headerCell,
            styles.cellLabel,
            styles.headerCellPick,
            { color: c.textMuted },
          ]}>
          {t('markets.cols.pick')}
        </ThemedText>
        {showOdd ? (
          <ThemedText style={[styles.headerCell, styles.cellOdd, { color: c.textMuted }]}>
            {t('markets.cols.odd')}
          </ThemedText>
        ) : null}
        <ThemedText style={[styles.headerCell, styles.cellGauge, { color: c.textMuted }]}>
          {t('markets.cols.roi')}
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellGauge, { color: c.textMuted }]}>
          {t('markets.cols.hit')}
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellGauge, { color: c.textMuted }]}>
          {t('markets.cols.imp')}
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNarrow, { color: c.textMuted }]}>
          {t('markets.cols.win')}
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNarrow, { color: c.textMuted }]}>
          {t('markets.cols.loss')}
        </ThemedText>
      </View>

      {sortedOutcomes.map((outcome, idx) => {
        // Insert a thin "total" divider when starting a new total group
        // (paired-totals markets only). For non-paired markets this
        // condition never fires and we render a plain row sequence.
        const previousOutcome = idx > 0 ? sortedOutcomes[idx - 1] : null;
        const showTotalDivider =
          isPairedTotalsMarket &&
          outcome.total != null &&
          (previousOutcome == null || previousOutcome.total !== outcome.total);
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
          void tryAdd({
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
            // Stash the real odd in the coupon so parlay totals can be
            // computed even when the user has odds hidden in the UI.
            oddValue: outcome.value ?? 0,
            dso: outcome.winning_percent,
            vbet: outcome.earning_percent,
            iko,
            sampleCount: hasSample ? sample : null,
          });
        };

        // In paired-totals markets the divider above the group carries the
        // total value, so the label cell just needs the side ("Üst"/"Alt").
        // Outside that case fall back to the standard "Üst 2.5" form.
        const rowLabel = isPairedTotalsMarket
          ? shortenOutcome(outcome.label, market.market_id, homeName, awayName)
          : formatLabel(outcome, market.market_id, homeName, awayName);
        return (
          <View
            key={`${outcome.label}-${outcome.total ?? ''}-${outcome.handicap ?? ''}`}>
            {showTotalDivider ? (
              <View style={[styles.totalDivider, { borderTopColor: c.border, backgroundColor: c.surface }]}>
                <ThemedText style={[styles.totalDividerText, { color: c.textMuted }]}>
                  {outcome.total}
                </ThemedText>
              </View>
            ) : null}
          <View
            style={[
              styles.row,
              { borderTopColor: c.border },
              // Value-bet rows get a left accent bar, faint brand tint, and
              // a 2px brand bottom border — three signals together so the eye
              // catches them while scanning the card.
              isValueBet && {
                backgroundColor: 'rgba(58, 143, 111, 0.10)',
                borderBottomWidth: StyleSheet.hairlineWidth,
                borderBottomColor: c.brandSoft,
              },
            ]}>
            {isValueBet ? (
              <View style={[styles.valueAccent, { backgroundColor: c.brandSoft }]} />
            ) : null}
            {/* Tip cell carries the coupon-toggle now (was the odd cell). */}
            <Pressable
              onPress={handleAddToCoupon}
              disabled={tapDisabled && !inCoupon}
              accessibilityRole="button"
              accessibilityLabel={
                inCoupon
                  ? `${rowLabel} kaldır`
                  : tapDisabled
                    ? `${rowLabel} eklenemez`
                    : `${rowLabel} ekle`
              }
              accessibilityState={{ selected: inCoupon, disabled: tapDisabled && !inCoupon }}
              style={[
                styles.cellLabel,
                styles.tipPressable,
                inCoupon && { backgroundColor: c.brandSoft, borderColor: c.brand },
                // Only dim when another row in this card already holds the
                // pick (one-pick-per-fixture). Live / finished matches stay
                // at full opacity so the green/red coloured tip is readable.
                fixtureTaken && !inCoupon && { opacity: 0.4 },
              ]}>
              <ThemedText
                style={[
                  styles.cell,
                  styles.tipText,
                  {
                    color:
                      liveColor ??
                      (inCoupon ? c.brand : isValueBet ? c.brand : c.text),
                    fontWeight: inCoupon || isValueBet || liveColor ? '700' : '500',
                  },
                ]}
                numberOfLines={2}>
                {inCoupon ? '✓ ' : isValueBet ? '★ ' : ''}
                {rowLabel}
              </ThemedText>
            </Pressable>
            {showOdd ? (
              <ThemedText
                style={[
                  styles.cell,
                  styles.cellOdd,
                  styles.numberValue,
                  { color: c.text },
                ]}>
                {outcome.value != null ? outcome.value.toFixed(2) : '-'}
              </ThemedText>
            ) : null}
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
          </View>
        );
      })}
      </>
      )}
    </View>
  );
}

function formatLabel(
  o: FixtureOddOutcome,
  marketId: number,
  homeName?: string | null,
  awayName?: string | null,
): string {
  // Run the raw SportMonks label through shortenOutcome so EN-only
  // strings ("Over", "Under", "Draw or Termalica BB Nieciecza") render
  // with the locale-aware short codes (Üst / Alt / X2). Falls back to
  // the raw label for markets shortenOutcome doesn't translate.
  const short = shortenOutcome(o.label, marketId, homeName, awayName);
  const suffix = o.total ?? o.handicap ?? null;
  return suffix ? `${short} ${suffix}` : short;
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
    paddingTop: 10,
    paddingBottom: 10,
  },
  titleActions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
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
  // Pick column header reads left-aligned to match the tip cell below
  // (cellLabel + tipText also lean left). Other column headers stay
  // center-aligned over their numeric values.
  headerCellPick: {
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
    textAlign: 'center',
  },
  cellLabel: {
    flex: 2.4,
    fontWeight: '500',
  },
  // Tip text leans left inside the pressable cell — the column header
  // ("TAHMİN") still reads center-aligned because it uses cellLabel +
  // headerCell.textAlign='center'. Two different alignments on the same
  // flex column.
  tipText: {
    textAlign: 'left',
    alignSelf: 'flex-start',
  },
  cellOdd: {
    flex: 0.8,
    textAlign: 'center',
  },
  // Visual treatment for the tip cell when it's a tap target. Subtle border
  // hints at affordance without competing with the value-bet ★ row accent.
  tipPressable: {
    paddingVertical: 6,
    paddingHorizontal: 6,
    borderRadius: 6,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: 'transparent',
    justifyContent: 'center',
    alignItems: 'flex-start',
  },
  valueAccent: {
    position: 'absolute',
    left: 0,
    top: 0,
    bottom: 0,
    width: 2,
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
  // Thin header strip that separates Over/Under pairs in totals markets.
  // Carries the total value so each pair below doesn't have to repeat it
  // ("Üst" / "Alt" alone in the label cell is enough).
  totalDivider: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderTopWidth: StyleSheet.hairlineWidth,
    alignItems: 'flex-start',
  },
  totalDividerText: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.5,
    fontVariant: ['tabular-nums'],
  },
});
