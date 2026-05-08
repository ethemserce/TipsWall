import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { useRouter } from 'expo-router';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { CircularGauge } from '@/src/components/CircularGauge';
import {
  isInDraft,
  toggleSelection,
  useCouponStore,
} from '@/src/lib/coupons/store';
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
  const router = useRouter();

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

  // Compute per-signal İKO across the same market (no-vig probability).
  const ikoByMarket = computeIkoByMarket(signals);

  // Draft selections — a Set for O(1) "is this signal already in the basket?"
  const draftSelections = useCouponStore((s) => s.draft.selections);
  const draftKeys = new Set(
    draftSelections.map((sel) =>
      [
        sel.fixtureId,
        sel.marketId,
        sel.outcomeLabel.toLowerCase(),
        sel.total ?? '-',
        sel.handicap ?? '-',
        sel.oddValue.toFixed(4),
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

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <Pressable
        onPress={() => router.push(`/fixture/${fixtureId}` as never)}
        style={({ pressed }) => [styles.matchInfo, pressed && { opacity: 0.7 }]}>
        {dateLine || timeLine ? (
          <View style={styles.topRow}>
            {dateLine ? (
              <ThemedText style={[styles.date, { color: c.text }]}>
                {dateLine}
              </ThemedText>
            ) : null}
            {timeLine ? (
              <ThemedText style={[styles.time, { color: c.textMuted }]}>
                {timeLine}
              </ThemedText>
            ) : null}
          </View>
        ) : null}

        <View style={styles.teamsRow}>
          <TeamColumn name={homeName} imagePath={homeImg} />
          <View style={styles.scoreBlock}>
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
            {showScore ? (
              <>
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
                <ThemedText
                  style={[
                    styles.scoreFt,
                    { color: live ? c.live : c.textMuted },
                  ]}>
                  {live
                    ? liveMinute != null
                      ? `${liveMinute}'`
                      : 'CANLI'
                    : halfScore
                      ? `İY ${halfScore.home}-${halfScore.away}`
                      : 'FT'}
                </ThemedText>
              </>
            ) : (
              <ThemedText style={[styles.vs, { color: c.text }]}>VS</ThemedText>
            )}
          </View>
          <TeamColumn name={awayName} imagePath={awayImg} />
        </View>
      </Pressable>

      <View style={[styles.divider, { backgroundColor: c.border }]} />

      <View style={styles.headerRow}>
        <ThemedText
          style={[
            styles.headerCell,
            styles.cellLabel,
            styles.headerLabelLeft,
            { color: c.textMuted },
          ]}>
          TİP
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNumber, { color: c.textMuted }]}>
          ORAN
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellGauge, { color: c.textMuted }]}>
          VBET
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellGauge, { color: c.textMuted }]}>
          DSO
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellGauge, { color: c.textMuted }]}>
          İKO
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNarrow, { color: c.textMuted }]}>
          KZ
        </ThemedText>
        <ThemedText style={[styles.headerCell, styles.cellNarrow, { color: c.textMuted }]}>
          KY
        </ThemedText>
      </View>

      {signals.map((s) => {
        const market = marketLookup.get(s.market_id);
        // Prefer the server-computed İKO when available — it sees the full
        // market context (Σ(1/oran) over every outcome the bookmaker offers
        // on this line). Client-side compute is a fallback for older API
        // responses; once visible signals are capped (top-N per fixture) it
        // produces wrong values because it only sees the surviving outcomes.
        const iko =
          s.iko != null
            ? s.iko
            : ikoByMarket.get(s.market_id)?.get(s.id) ?? null;
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
        // Tip stays neutral so the row stays calm; only the odd value
        // carries the win/loss colour.
        const oddColor = won ? WIN_COLOR : lost ? LOSS_COLOR : c.textMuted;

        // Coupon membership — odd cell becomes a toggle.
        const oddKey =
          s.odd_value != null
            ? [
                fixtureId,
                s.market_id,
                s.label.toLowerCase(),
                s.total ?? '-',
                s.handicap ?? '-',
                s.odd_value.toFixed(4),
              ].join('|')
            : null;
        const inCoupon = oddKey != null && draftKeys.has(oddKey);
        // Another outcome from this fixture already in the basket → block
        // (one pick per fixture). Untapping the existing one re-opens the rest.
        const tapDisabled = couponLocked || (fixtureTaken && !inCoupon);
        const handleAddToCoupon = () => {
          if (s.odd_value == null) return;
          if (tapDisabled) return;
          const rawLabel = s.label || '';
          toggleSelection({
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
            oddValue: s.odd_value,
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
            <View style={styles.cellLabel}>
              <ThemedText
                style={[styles.label, { color: c.text }]}
                numberOfLines={1}>
                {formatLabel(s, market)}
              </ThemedText>
            </View>
            <Pressable
              onPress={handleAddToCoupon}
              disabled={tapDisabled && !inCoupon}
              style={[
                styles.cellNumber,
                styles.oddCell,
                inCoupon && {
                  backgroundColor: c.brand,
                  borderColor: c.brand,
                },
                !inCoupon && { borderColor: c.border },
                tapDisabled && !inCoupon && { opacity: 0.4 },
              ]}>
              <ThemedText
                style={[
                  styles.cell,
                  styles.numberValue,
                  {
                    color: inCoupon
                      ? c.textInverse
                      : oddColor,
                    fontWeight: inCoupon || won || lost ? '700' : '600',
                  },
                ]}>
                {s.odd_value != null ? s.odd_value.toFixed(2) : '-'}
              </ThemedText>
            </Pressable>
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

function computeIkoByMarket(signals: RateResult[]) {
  // Group signals by market and compute no-vig implied probability per row.
  const byMarket = new Map<number, RateResult[]>();
  for (const s of signals) {
    const list = byMarket.get(s.market_id);
    if (list) list.push(s);
    else byMarket.set(s.market_id, [s]);
  }
  const out = new Map<number, Map<string, number>>();
  for (const [marketId, list] of byMarket.entries()) {
    const totalImplied = list.reduce(
      (acc, s) => (s.odd_value && s.odd_value > 0 ? acc + 1 / s.odd_value : acc),
      0,
    );
    const inner = new Map<string, number>();
    for (const s of list) {
      if (s.odd_value && s.odd_value > 0 && totalImplied > 0) {
        inner.set(s.id, (1 / s.odd_value / totalImplied) * 100);
      }
    }
    out.set(marketId, inner);
  }
  return out;
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 12,
    marginTop: 12,
    borderRadius: 12,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
  },
  starsRow: {
    flexDirection: 'row',
    gap: 3,
    marginBottom: 4,
  },
  star: {
    fontSize: 13,
  },
  divider: {
    height: StyleSheet.hairlineWidth,
  },
  matchInfo: {
    paddingTop: 8,
    paddingBottom: 10,
    paddingHorizontal: 12,
  },
  topRow: {
    flexDirection: 'row',
    alignItems: 'baseline',
    justifyContent: 'center',
    gap: 6,
  },
  date: {
    fontSize: 12,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
  },
  time: {
    fontSize: 12,
    fontWeight: '500',
    fontVariant: ['tabular-nums'],
  },
  teamsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    width: '100%',
    marginTop: 6,
  },
  teamColumn: {
    flex: 1,
    alignItems: 'center',
    gap: 8,
  },
  teamLogo: {
    width: 56,
    height: 56,
  },
  teamLogoPlaceholder: {
    width: 56,
    height: 56,
    borderRadius: 8,
  },
  teamName: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.4,
    textAlign: 'center',
  },
  vs: {
    fontSize: 14,
    fontWeight: '700',
    letterSpacing: 1,
    paddingHorizontal: 8,
  },
  scoreBlock: {
    paddingHorizontal: 8,
    alignItems: 'center',
    gap: 2,
  },
  scoreText: {
    fontSize: 24,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  scoreSep: {
    fontSize: 20,
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
    flex: 1.3,
    paddingLeft: 6,
  },
  label: {
    fontSize: 12,
    fontWeight: '600',
  },
  cellNumber: {
    flex: 0.7,
    textAlign: 'center',
  },
  oddCell: {
    paddingVertical: 4,
    borderRadius: 6,
    borderWidth: StyleSheet.hairlineWidth,
    alignItems: 'center',
    justifyContent: 'center',
    marginHorizontal: 2,
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
