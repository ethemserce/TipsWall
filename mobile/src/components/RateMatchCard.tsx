import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { useRouter } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';

import { useOddsHidden } from '@/src/lib/settings/settingsStore';

import { ThemedText } from '@/components/themed-text';
import { CircularGauge } from '@/src/components/CircularGauge';
import { useTryAddSelection } from '@/src/hooks/useTryAddSelection';
import { useCouponStore } from '@/src/lib/coupons/store';
import { getStateBucket } from '@/src/lib/fixtureState';
import { outcomeLiveStatus } from '@/src/lib/liveOutcome';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureDetail } from '@/src/types/fixtureDetail';
import type { Market } from '@/src/types/market';
import type { RateResult } from '@/src/types/rateResult';

// Softer than the saturated 500-series — keeps wins/losses readable
// without dominating the row.
const WIN_COLOR = '#4ade80';
const LOSS_COLOR = '#f87171';

interface RateMatchCardProps {
  fixtureId: number;
  fixture: FixtureDetail | undefined;
  signals: RateResult[];
  marketLookup: Map<number, Market>;
  primaryMetric: 'winning_percent' | 'earning_percent';
  gaugeColor: string;
}

const VBET_COLOR = '#f59e0b';
const DSO_COLOR = '#22c55e';
const IKO_COLOR = '#3b82f6';
const STAR_COLOR = '#f59e0b';

export function RateMatchCard({
  fixtureId,
  fixture,
  signals,
  marketLookup,
  primaryMetric,
}: RateMatchCardProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const router = useRouter();
  const tryAdd = useTryAddSelection();
  // Settings → "Oranları göster" reveals the ORAN column. The signals
  // endpoint doesn't ship odd_value as its own field but encodes it in
  // outcome_key as the last colon-separated part (label:total:handicap:
  // odd_value with 4 decimals), so we parse it back here.
  const showOdd = !useOddsHidden();

  const homeName = fixture?.fixture.home_team_name ?? null;
  const awayName = fixture?.fixture.away_team_name ?? null;
  const homeImg = fixture?.fixture.home_team_image_path ?? null;
  const awayImg = fixture?.fixture.away_team_image_path ?? null;
  const startingAt = fixture?.fixture.starting_at ?? null;
  const dateLine = startingAt ? format(parseISO(startingAt), 'dd.MM.yyyy') : null;
  const timeLine = startingAt ? format(parseISO(startingAt), 'HH:mm') : null;
  const bucket = getStateBucket(fixture?.fixture.state_id ?? null);
  const finished = bucket === 'finished';
  const live = bucket === 'live';
  const upcoming = !live && !finished;
  const homeScore = fixture?.fixture.home_score ?? null;
  const awayScore = fixture?.fixture.away_score ?? null;
  const hasScore = homeScore != null && awayScore != null;
  const showScore = (finished || live) && hasScore;
  const liveMinute = fixture?.fixture.live_minute ?? null;
  // Whenever a score exists (live OR final) recolour each signal based on
  // it. bet_winning is only used as a fallback for markets the resolver
  // doesn't know how to evaluate (e.g. half-only markets).
  const scoreForOutcome =
    (live || finished) && hasScore
      ? { home: homeScore as number, away: awayScore as number }
      : null;

  // Half-time score, only relevant once 1H is over (state_id 2 = in 1H).
  // Replaces the "FT" label below the score so finished matches still
  // surface useful context.
  const halfScore =
    fixture && fixture.fixture.state_id !== 2
      ? findHalfScore(fixture.scores, '1ST_HALF')
      : null;

  const stars = computeStars(signals, primaryMetric);

  // Draft selections — a Set for O(1) "is this signal already in the basket?"
  // Keyed by the tip identity (fixture/market/label/total/handicap) since
  // we no longer surface the odd value.
  const draftSelections = useCouponStore((s) => s.draft.selections);
  const draftKeys = new Set(
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
  // True when any selection on this fixture is already in the draft. The
  // one-pick-per-fixture rule then disables every other outcome on this
  // card until the existing pick is removed.
  const fixtureTaken = draftSelections.some((sel) => sel.fixtureId === fixtureId);
  // Coupon picks are only allowed for not-yet-started matches.
  const couponLocked = !upcoming;

  const homeNameForCoupon = fixture?.fixture.home_team_name ?? '';
  const awayNameForCoupon = fixture?.fixture.away_team_name ?? '';
  const fixtureNameForCoupon =
    homeNameForCoupon || awayNameForCoupon
      ? `${homeNameForCoupon} - ${awayNameForCoupon}`
      : `Maç #${fixtureId}`;

  // Left-edge accent strip. State coloring (green/red/grey) read too
  // loud next to the lighter top + bottom edges of the card; user feedback
  // was that the saturated stripe felt heavy. Match it to the outer
  // borderSoft tone so the strip whispers along with the rest of the
  // border instead of standing out as a colored bar. State identity
  // still comes from the headerWash up top.
  const accentColor = c.borderSoft;
  // Soft brand wash on the top strip when the match is still upcoming,
  // tinted live red while in-play. Finished matches get a neutral header
  // so the historic data underneath stays the focus.
  const headerWash = live
    ? 'rgba(217, 112, 112, 0.10)'
    : finished
      ? c.borderSoft
      : c.brandSoft;

  return (
    <View
      style={[
        styles.card,
        c.shadowCard,
        { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
      ]}>
      <View
        style={[styles.accentStrip, { backgroundColor: accentColor }]}
        pointerEvents="none"
      />
      <Pressable
        onPress={() => router.push(`/fixture/${fixtureId}` as never)}
        style={({ pressed }) => [pressed && { opacity: 0.7 }]}>
        <View style={[styles.metaStrip, { backgroundColor: headerWash }]}>
          {dateLine ? (
            <ThemedText style={[styles.metaDate, { color: c.text }]}>
              {dateLine}
            </ThemedText>
          ) : <View />}
          <View style={styles.starsRow}>
            {Array.from({ length: 3 }).map((_, i) => (
              <ThemedText
                key={i}
                style={[
                  styles.star,
                  { color: i < stars ? STAR_COLOR : c.border },
                ]}>
                ★
              </ThemedText>
            ))}
          </View>
          {timeLine && !showScore ? (
            <ThemedText style={[styles.metaTime, { color: c.textMuted }]}>
              {timeLine}
            </ThemedText>
          ) : live ? (
            <View style={[styles.livePill, { backgroundColor: c.live }]}>
              <View style={styles.liveDot} />
              <ThemedText style={styles.livePillText}>
                {liveMinute != null ? `${liveMinute}'` : 'CANLI'}
              </ThemedText>
            </View>
          ) : finished ? (
            <ThemedText style={[styles.metaFt, { color: c.textMuted }]}>
              {halfScore ? `İY ${halfScore.home}-${halfScore.away}` : 'FT'}
            </ThemedText>
          ) : <View />}
        </View>

        <View style={styles.matchInfo}>
          <View style={styles.teamsRow}>
            <TeamColumn name={homeName} imagePath={homeImg} />
            <View style={styles.scoreBlock}>
              {showScore ? (
                <View
                  style={[
                    styles.scorePill,
                    {
                      backgroundColor: live ? 'rgba(217, 112, 112, 0.12)' : c.bg,
                      borderColor: live ? c.live : c.borderSoft,
                    },
                  ]}>
                  <ThemedText
                    style={[
                      styles.scoreText,
                      { color: live ? c.live : c.text },
                    ]}>
                    {homeScore}
                    <ThemedText style={[styles.scoreSep, { color: c.textMuted }]}>
                      {' - '}
                    </ThemedText>
                    {awayScore}
                  </ThemedText>
                </View>
              ) : (
                <ThemedText style={[styles.vs, { color: c.textMuted }]}>VS</ThemedText>
              )}
            </View>
            <TeamColumn name={awayName} imagePath={awayImg} />
          </View>
        </View>
      </Pressable>

      <View style={[styles.divider, { backgroundColor: c.borderSoft }]} />

      <View style={styles.headerRow}>
        <ThemedText
          style={[
            styles.headerCell,
            styles.cellLabel,
            styles.headerLabelLeft,
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

      {signals.map((s) => {
        const market = marketLookup.get(s.market_id);
        // Server-side İKO — already computed against the full market
        // context (Σ(1/oran) over every outcome on this line).
        const iko = s.iko ?? null;
        const sample = s.win_count + s.lost_count;
        const hasSample = sample > 0;
        // Win/loss colour only applies once a match is in-play or finished.
        // Upcoming matches stay neutral even if bet_winning leaks through
        // from a stale prematch_odds_current row.
        const status = !upcoming && scoreForOutcome
          ? outcomeLiveStatus(
              { market_id: s.market_id, label: s.label, total: s.total, handicap: s.handicap },
              scoreForOutcome,
            )
          : null;
        const won = upcoming ? false : status ? status === 'win' : s.bet_winning === true;
        const lost = upcoming ? false : status ? status === 'loss' : s.bet_winning === false;
        // With the ORAN column gone, the win/loss colour now lives on the
        // tip text itself.
        const tipLiveColor = won ? WIN_COLOR : lost ? LOSS_COLOR : null;

        // Coupon membership — keyed by tip identity (no odd value).
        const oddKey = [
          fixtureId,
          s.market_id,
          s.label.toLowerCase(),
          s.total ?? '-',
          s.handicap ?? '-',
        ].join('|');
        const inCoupon = draftKeys.has(oddKey);
        // Another outcome from this fixture already in the basket → block
        // (one pick per fixture). Untapping the existing one re-opens the rest.
        const tapDisabled = couponLocked || (fixtureTaken && !inCoupon);
        const handleAddToCoupon = () => {
          if (tapDisabled) return;
          const rawLabel = s.label || '';
          void tryAdd({
            fixtureId,
            fixtureName: fixtureNameForCoupon,
            startingAt: fixture?.fixture.starting_at ?? null,
            bookmakerId: 2,
            marketId: s.market_id,
            marketShort: MARKET_SHORT[s.market_id] ?? `M${s.market_id}`,
            // Store raw label (matches bookmaker feed) so settlement and
            // duplicate detection round-trip cleanly.
            outcomeLabel: rawLabel,
            outcomeDisplay: shortenOutcome(rawLabel, s.market_id),
            total: s.total,
            handicap: s.handicap,
            oddValue: 0,
            dso: s.winning_percent,
            vbet: s.earning_percent,
            iko: iko,
            sampleCount: hasSample ? sample : null,
          });
        };

        return (
          <View
            key={s.id}
            style={[styles.signalRow, { borderTopColor: c.border }]}>
            <Pressable
              onPress={handleAddToCoupon}
              disabled={tapDisabled && !inCoupon}
              accessibilityRole="button"
              accessibilityState={{ selected: inCoupon, disabled: tapDisabled && !inCoupon }}
              style={[
                styles.cellLabel,
                styles.tipPressable,
                inCoupon && { backgroundColor: c.brandSoft, borderColor: c.brand },
                // Only dim when the one-pick-per-fixture rule is blocking
                // (another row in this card has been picked). Finished
                // matches stay at full opacity so the historic tip + the
                // green/red live colour stay readable.
                fixtureTaken && !inCoupon && { opacity: 0.4 },
              ]}>
              <ThemedText
                style={[
                  styles.label,
                  {
                    color: tipLiveColor ?? (inCoupon ? c.brand : c.text),
                    fontWeight: inCoupon || tipLiveColor ? '700' : '500',
                  },
                ]}
                numberOfLines={2}>
                {inCoupon ? '✓ ' : ''}
                {formatLabel(s, market)}
              </ThemedText>
            </Pressable>
            {showOdd ? (
              <ThemedText
                style={[styles.cell, styles.cellOdd, styles.numberValue, { color: c.text }]}>
                {parseOddFromOutcomeKey(s.outcome_key) ?? '-'}
              </ThemedText>
            ) : null}
            <View style={styles.cellGauge}>
              <CircularGauge
                value={hasSample ? s.earning_percent : null}
                color={VBET_COLOR}
              />
            </View>
            <View style={styles.cellGauge}>
              <CircularGauge
                value={hasSample ? s.winning_percent : null}
                color={DSO_COLOR}
              />
            </View>
            <View style={styles.cellGauge}>
              <CircularGauge value={iko} color={IKO_COLOR} />
            </View>
            <ThemedText
              style={[styles.cell, styles.cellNarrow, styles.numberValue, { color: c.textMuted }]}>
              {hasSample ? s.win_count : '-'}
            </ThemedText>
            <ThemedText
              style={[styles.cell, styles.cellNarrow, styles.numberValue, { color: c.textMuted }]}>
              {hasSample ? s.lost_count : '-'}
            </ThemedText>
          </View>
        );
      })}
    </View>
  );
}

function TeamColumn({
  name,
  imagePath,
}: {
  name: string | null;
  imagePath: string | null;
}) {
  const c = useTheme();
  return (
    <View style={styles.teamColumn}>
      {imagePath ? (
        <Image
          source={{ uri: imagePath }}
          style={styles.teamLogo}
          contentFit="contain"
          transition={150}
        />
      ) : (
        <View style={[styles.teamLogoPlaceholder, { backgroundColor: c.border }]} />
      )}
      <ThemedText
        style={[styles.teamName, { color: c.text }]}
        numberOfLines={2}>
        {(name ?? 'TBD').toUpperCase()}
      </ThemedText>
    </View>
  );
}

// Compact Turkish bet-slip shortcodes per market — displayed before the
// outcome label so "MS 1" reads naturally even when 8 markets are stacked.
const MARKET_SHORT: Record<number, string> = {
  1: 'MS',    // Fulltime Result
  10: 'DNB',  // Draw No Bet
  14: 'KG',   // Both Teams To Score
  18: 'EV',   // Home Team Exact Goals
  19: 'DEP',  // Away Team Exact Goals
  33: 'İY',   // First Half Exact Goals
  38: '2Y',   // Second Half Exact Goals
  44: 'T/Ç',  // Odd / Even
};

function shortenOutcome(label: string, marketId: number): string {
  // "AGF Aarhus - 3+ Goals" → "3+", "1 Goal" / "5+ Goals" → "1" / "5+"
  if (marketId === 18 || marketId === 19) {
    const dash = label.lastIndexOf(' - ');
    const tail = dash >= 0 ? label.slice(dash + 3) : label;
    return tail.replace(/\s+Goals?$/i, '');
  }
  if (marketId === 33 || marketId === 38) {
    return label.replace(/\s+Goals?$/i, '');
  }
  if (marketId === 1) {
    if (label === 'Home') return '1';
    if (label === 'Draw') return 'X';
    if (label === 'Away') return '2';
  }
  if (marketId === 14) {
    if (label === 'Yes') return 'Var';
    if (label === 'No') return 'Yok';
  }
  if (marketId === 44) {
    if (label === 'Odd') return 'Tek';
    if (label === 'Even') return 'Çift';
  }
  return label;
}

function formatLabel(s: RateResult, market: Market | undefined): string {
  const short = MARKET_SHORT[s.market_id] ?? market?.name ?? `M${s.market_id}`;
  const outcome = shortenOutcome(s.label || '', s.market_id);
  if (s.total != null) return `${short} ${outcome} ${s.total}`.trim();
  if (s.handicap != null) return `${short} ${outcome} ${s.handicap}`.trim();
  return `${short} ${outcome}`.trim();
}

function computeStars(
  signals: RateResult[],
  primaryMetric: 'winning_percent' | 'earning_percent',
): number {
  if (signals.length === 0) return 0;
  const best = signals.reduce<number>((acc, s) => {
    const v = s[primaryMetric];
    return v != null && v > acc ? v : acc;
  }, -Infinity);
  if (best === -Infinity) return 0;

  if (primaryMetric === 'winning_percent') {
    if (best >= 70) return 3;
    if (best >= 60) return 2;
    if (best >= 50) return 1;
    return 0;
  }
  // earning_percent (ROI)
  if (best >= 30) return 3;
  if (best >= 15) return 2;
  if (best >= 5) return 1;
  return 0;
}

function findHalfScore(
  scores: { description: string | null; participant_location: string | null; goals: number | null }[] | undefined,
  description: string,
): { home: number; away: number } | null {
  if (!scores) return null;
  const home = scores.find(
    (s) => s.description === description && s.participant_location === 'home',
  );
  const away = scores.find(
    (s) => s.description === description && s.participant_location === 'away',
  );
  if (home?.goals == null || away?.goals == null) return null;
  return { home: home.goals, away: away.goals };
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 12,
    marginTop: 12,
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
  },
  // 3px state-coloured accent strip on the left edge — kept at 0.45
  // opacity (applied inline) so it whispers rather than shouts. Solid
  // colour read as a hard alert badge; faded reads as a soft border.
  accentStrip: {
    position: 'absolute',
    left: 0,
    top: 0,
    bottom: 0,
    width: 3,
    zIndex: 1,
  },
  // Top header strip: date · stars · time-or-state pill. Brand-tinted
  // background gives the card identity vs. the older flat layout.
  metaStrip: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingLeft: 16,
    paddingRight: 12,
    paddingVertical: 7,
    gap: 8,
  },
  metaDate: {
    fontSize: 11,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
    letterSpacing: 0.3,
  },
  metaTime: {
    fontSize: 11,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
  },
  metaFt: {
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.5,
  },
  livePill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: 7,
    paddingVertical: 2,
    borderRadius: 999,
  },
  liveDot: {
    width: 5,
    height: 5,
    borderRadius: 3,
    backgroundColor: '#ffffff',
  },
  livePillText: {
    fontSize: 9,
    fontWeight: '900',
    letterSpacing: 0.6,
    color: '#ffffff',
  },
  starsRow: {
    flexDirection: 'row',
    gap: 3,
  },
  star: {
    fontSize: 13,
  },
  divider: {
    height: StyleSheet.hairlineWidth,
  },
  matchInfo: {
    paddingTop: 12,
    paddingBottom: 10,
    paddingHorizontal: 12,
  },
  teamsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    width: '100%',
  },
  teamColumn: {
    flex: 1,
    alignItems: 'center',
    gap: 8,
  },
  teamLogo: {
    width: 52,
    height: 52,
  },
  teamLogoPlaceholder: {
    width: 52,
    height: 52,
    borderRadius: 8,
  },
  teamName: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.4,
    textAlign: 'center',
  },
  vs: {
    fontSize: 13,
    fontWeight: '800',
    letterSpacing: 1.5,
    paddingHorizontal: 10,
    paddingVertical: 6,
  },
  scoreBlock: {
    paddingHorizontal: 8,
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 52,
  },
  scorePill: {
    paddingHorizontal: 14,
    paddingVertical: 6,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
  },
  scoreText: {
    fontSize: 22,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  scoreSep: {
    fontSize: 18,
    fontWeight: '500',
  },
  scoreFt: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 8,
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
  signalRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 2,
  },
  cell: {
    fontSize: 12,
  },
  cellLabel: {
    // Was 2.4 — the pick column was stealing too much width vs. the
    // numeric metric columns. 1.6 still fits "1.5+ Goals" without
    // wrapping but leaves room for ROI/HIT/IMP/KZ/KY to breathe.
    flex: 1.6,
    paddingLeft: 6,
  },
  // Tip cell doubles as the coupon add/remove tap target now (was the odd
  // cell). Subtle border so the affordance is visible without being noisy.
  tipPressable: {
    paddingVertical: 6,
    paddingHorizontal: 8,
    borderRadius: 6,
    borderWidth: StyleSheet.hairlineWidth,
    borderColor: 'transparent',
    justifyContent: 'center',
  },
  label: {
    fontSize: 12,
    fontWeight: '600',
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
  cellOdd: {
    flex: 0.7,
    textAlign: 'center',
  },
  numberValue: {
    fontVariant: ['tabular-nums'],
    fontWeight: '600',
  },
});

// outcome_key carries "label:total:handicap:odd_value(4dp)" — split out
// the 4-decimal odd at the tail so the ORAN column can show it without
// a separate API field. Returns a tidy "1.61" string or null when the
// key shape isn't what we expect.
function parseOddFromOutcomeKey(key: string | null | undefined): string | null {
  if (!key) return null;
  const parts = key.split(':');
  if (parts.length < 4) return null;
  const v = parseFloat(parts[parts.length - 1]);
  if (!Number.isFinite(v)) return null;
  return v.toFixed(2);
}
