import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import {
  getMatchPhase,
  getStateBucket,
  getStateLabel,
  phaseShowsFulltimeSub,
  phaseShowsHalfTime,
  phaseShowsPenaltyScore,
  type MatchPhase,
} from '@/src/lib/fixtureState';
import { useTheme } from '@/src/lib/useTheme';
import type { Country } from '@/src/types/country';
import type { FixtureSummary } from '@/src/types/fixture';
import type { FixtureScore } from '@/src/types/fixtureDetail';
import type {
  FixtureEvent,
  FixtureWeather,
} from '@/src/types/fixtureDetailExtras';
import type { League } from '@/src/types/league';

interface FixtureDetailHeroProps {
  fixture: FixtureSummary;
  league?: League;
  country?: Country;
  scores?: FixtureScore[];
  events?: FixtureEvent[];
  weather?: FixtureWeather | null;
}

const GOAL_TYPE_CODES = new Set([
  'GOAL',
  'PENALTY',
  'OWNGOAL',
  'GOAL_AWARDED',
]);

export function FixtureDetailHero({
  fixture,
  league,
  country,
  scores,
  events,
  weather,
}: FixtureDetailHeroProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const bucket = getStateBucket(fixture.state_id);
  const live = bucket === 'live';
  const finished = bucket === 'finished';
  const scored = live || finished;

  const stateLabel = getStateLabel(fixture.state_id);
  const kickoffPill = fixture.starting_at
    ? format(parseISO(fixture.starting_at), 'dd.MM.yyyy • HH:mm')
    : null;
  const kickoffTime = fixture.starting_at
    ? format(parseISO(fixture.starting_at), 'HH:mm')
    : null;

  // Live-second counter. SportMonks publishes live_minute as an integer
  // (~1/min push). We tick locally between minute updates so the clock
  // doesn't sit on the same value for 60 seconds. Each new live_minute
  // resets the counter to 0.
  //
  // Originally the display capped seconds at 59 (`Math.min(59, tick)`),
  // which froze the clock at `MM:59` whenever the backend's live_minute
  // update lagged (e.g. worker OOM swallowed a couple of pushes). The
  // user-visible symptom: "the clock sits on 15:59 for 2+ minutes".
  // Now we let tick roll the displayed minute forward as well —
  // worst-case drift is bounded by the next backend push, which resets
  // tick to 0 and snaps the display back to truth.
  const minute = fixture.live_minute;
  const [tick, setTick] = useState(0);
  useEffect(() => {
    setTick(0);
  }, [minute]);
  useEffect(() => {
    if (!live || minute == null) return undefined;
    const id = setInterval(() => setTick((s) => s + 1), 1000);
    return () => clearInterval(id);
  }, [live, minute]);
  const liveMinuteLabel =
    live && minute != null
      ? `${minute + Math.floor(tick / 60)}:${(tick % 60)
          .toString()
          .padStart(2, '0')}`
      : null;

  // Match phase derived once + branched against everywhere — was a
  // dozen inline `stateId === N || stateId === M` chains before, with
  // overlap that caused real bugs (22 conflated with 19, full-time
  // sub-score leaking into 2nd-half live). See lib/fixtureState.ts
  // for the canonical state-id-to-phase mapping.
  const phase: MatchPhase = getMatchPhase(fixture.state_id);
  const inShootout = phase === 'pen' || phase === 'pen_break' || phase === 'ft_pen';

  // Halftime score: visible from HT onwards (not during 1st half itself
  // — the line score IS the 1H score at that point).
  const firstHalfPart =
    scored && phaseShowsHalfTime(phase)
      ? findHalfScore(scores, '1ST_HALF')
      : null;
  // Regulation-90 result, only shown once extra time / penalties kick in.
  // SportMonks ships this under description "FULLTIME" (or "NORMAL_TIME"
  // on some plans); try both so the row appears regardless of feed shape.
  const fullTimePart = phaseShowsFulltimeSub(phase)
    ? findHalfScore(scores, 'FULLTIME') ??
      findHalfScore(scores, 'NORMAL_TIME') ??
      findHalfScore(scores, '2ND_HALF')
    : null;
  // Penalty shootout running count, when the feed publishes one.
  const penaltyPart = phaseShowsPenaltyScore(phase)
    ? findHalfScore(scores, 'PENALTY_SHOOTOUT') ??
      findHalfScore(scores, 'PENALTIES')
    : null;

  const goals = (events ?? []).filter((e) =>
    GOAL_TYPE_CODES.has((e.type_code ?? '').toUpperCase()),
  );
  const homeGoals = goals.filter((g) => g.participant_location === 'home');
  const awayGoals = goals.filter((g) => g.participant_location === 'away');

  // Per-side penalty shootout attempts. SportMonks emits events with
  // type_code "PENALTY_SHOOTOUT" / "PENALTY_SHOOTOUT_GOAL" /
  // "PENALTY_SHOOTOUT_MISSED" during state 19; we accept both compact
  // forms and split outcomes by the event's `result` field where
  // available, falling back to type_code text.
  const homeShootout = inShootout
    ? collectShootoutAttempts(events, 'home')
    : [];
  const awayShootout = inShootout
    ? collectShootoutAttempts(events, 'away')
    : [];

  return (
    <View
      style={[
        styles.container,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      {kickoffPill ? (
        <View style={styles.kickoffRow}>
          <WeatherSide weather={weather} side="left" />
          <View style={[styles.kickoffPill, { backgroundColor: c.bg, borderColor: c.border }]}>
            <ThemedText style={[styles.kickoffText, { color: c.text }]}>
              {kickoffPill}
            </ThemedText>
          </View>
          <WeatherSide weather={weather} side="right" />
        </View>
      ) : null}

      <View style={styles.mainRow}>
        <TeamColumn
          teamId={fixture.home_team_id}
          name={fixture.home_team_name}
          imagePath={fixture.home_team_image_path}
          penaltyAttempts={inShootout ? homeShootout : null}
        />

        <View style={styles.centerColumn}>
          {scored ? (
            <View style={styles.scoreRow}>
              <ThemedText
                style={[
                  styles.scoreText,
                  { color: live ? c.live : c.text },
                ]}>
                {fixture.home_score ?? 0}
              </ThemedText>
              <ThemedText style={[styles.scoreSeparator, { color: c.textMuted }]}>
                -
              </ThemedText>
              <ThemedText
                style={[
                  styles.scoreText,
                  { color: live ? c.live : c.text },
                ]}>
                {fixture.away_score ?? 0}
              </ThemedText>
            </View>
          ) : (
            <ThemedText style={[styles.kickoffTime, { color: c.text }]}>
              {kickoffTime ?? '--:--'}
            </ThemedText>
          )}

          {/* Sub-scores come BEFORE the minute label now — user feedback:
              "üstte skor, altında MS, altında İY, en altta dakika". For
              a 1st-half live match firstHalfPart is null (state == 2),
              so only the minute shows; for 2nd-half live we get score +
              halftime + minute; for full-time we drop the minute and
              just show score + halftime; ET shows score + regulation +
              halftime + minute / state label. Penalty shootout adds
              its own row when the feed publishes one. */}
          {scored && fullTimePart ? (
            <ThemedText
              style={[styles.subScore, { color: c.textMuted }]}
              numberOfLines={1}>
              {t('fixture.hero.fullTimeShort', {
                home: fullTimePart.home,
                away: fullTimePart.away,
              })}
            </ThemedText>
          ) : null}
          {scored && firstHalfPart ? (
            <ThemedText
              style={[styles.subScore, { color: c.textMuted }]}
              numberOfLines={1}>
              {t('fixture.hero.halfTimeShort', {
                home: firstHalfPart.home,
                away: firstHalfPart.away,
              })}
            </ThemedText>
          ) : null}
          {penaltyPart ? (
            <ThemedText
              style={[styles.subScore, { color: c.textMuted }]}
              numberOfLines={1}>
              {t('fixture.hero.penaltyShort', {
                home: penaltyPart.home,
                away: penaltyPart.away,
              })}
            </ThemedText>
          ) : null}
          {scored && (live || phase === 'aet' || phase === 'ft_pen') ? (
            <ThemedText
              style={[
                styles.statusText,
                { color: live ? c.live : c.textMuted },
              ]}>
              {liveMinuteLabel ?? stateLabel ?? null}
            </ThemedText>
          ) : null}
        </View>

        <TeamColumn
          teamId={fixture.away_team_id}
          name={fixture.away_team_name}
          imagePath={fixture.away_team_image_path}
          penaltyAttempts={inShootout ? awayShootout : null}
        />
      </View>

      {homeGoals.length + awayGoals.length > 0 ? (
        <View style={styles.goalSplit}>
          <View style={styles.goalSideHome}>
            {homeGoals.map((g) => (
              <GoalLine key={g.id} goal={g} side="home" />
            ))}
          </View>
          <View style={styles.goalGap} />
          <View style={styles.goalSideAway}>
            {awayGoals.map((g) => (
              <GoalLine key={g.id} goal={g} side="away" />
            ))}
          </View>
        </View>
      ) : null}
    </View>
  );
}

function GoalLine({
  goal,
  side,
}: {
  goal: FixtureEvent;
  side: 'home' | 'away';
}) {
  const c = useTheme();
  const code = (goal.type_code ?? '').toUpperCase();
  const cancelled = goal.cancelled === true;
  const minuteStr =
    goal.minute != null
      ? goal.extra_minute && goal.extra_minute > 0
        ? `${goal.minute}+${goal.extra_minute}'`
        : `${goal.minute}'`
      : '';
  const tag =
    code === 'PENALTY'
      ? '(Pen.)'
      : code === 'OWNGOAL'
        ? '(OG)'
        : null;
  const text = `${goal.player_name ?? '—'} ${minuteStr}${tag ? ` ${tag}` : ''}`;

  return (
    <View
      style={
        side === 'home' ? styles.goalRowHome : styles.goalRowAway
      }>
      {side === 'away' ? <ThemedText style={styles.goalBall}>⚽</ThemedText> : null}
      <ThemedText
        style={[
          styles.goalText,
          { color: c.textMuted },
          side === 'home' ? styles.goalTextHome : styles.goalTextAway,
          // VAR-cancelled goals: strikethrough so the user can see what
          // happened on the pitch (goal scored, then overturned) without
          // mistaking the missing line for a sync bug.
          cancelled && { textDecorationLine: 'line-through' },
        ]}
        numberOfLines={1}>
        {text}
      </ThemedText>
      {side === 'home' ? <ThemedText style={styles.goalBall}>⚽</ThemedText> : null}
    </View>
  );
}

function TeamColumn({
  teamId,
  name,
  imagePath,
  penaltyAttempts,
}: {
  teamId: number | null | undefined;
  name: string | null | undefined;
  imagePath: string | null | undefined;
  // Boolean array of per-side shootout outcomes (true = scored). Null
  // when the match isn't in a shootout — the dot row is hidden then.
  penaltyAttempts: boolean[] | null;
}) {
  const c = useTheme();
  const { t } = useTranslation();
  // Tap on a team logo (or its placeholder) opens the team detail.
  // Wrapping the logo only — not the surrounding star + name — keeps the
  // affordance focused so the rest of the column doesn't accidentally
  // navigate when the user is just reading the score.
  const handlePress = () => {
    if (teamId != null) router.push(`/team/${teamId}` as never);
  };
  return (
    <View style={styles.teamColumn}>
      <ThemedText style={[styles.star, { color: c.textMuted }]}>☆</ThemedText>
      <Pressable
        onPress={handlePress}
        disabled={teamId == null}
        hitSlop={8}
        accessibilityRole="button"
        accessibilityLabel={name ?? t('fixture.hero.tbd')}
        style={({ pressed }) => [pressed && { opacity: 0.6 }]}>
        {imagePath ? (
          <Image
            source={{ uri: imagePath }}
            style={styles.teamLogo}
            contentFit="contain"
            transition={200}
          />
        ) : (
          <View style={[styles.teamLogoPlaceholder, { backgroundColor: c.border }]} />
        )}
      </Pressable>
      <ThemedText
        style={[styles.teamName, { color: c.text }]}
        numberOfLines={2}>
        {name ?? t('fixture.hero.tbd')}
      </ThemedText>
      {penaltyAttempts ? (
        <PenaltyDots attempts={penaltyAttempts} />
      ) : null}
    </View>
  );
}

function PenaltyDots({ attempts }: { attempts: boolean[] }) {
  const c = useTheme();
  // Regulation shootout is 5 spot kicks per side; sudden-death rounds
  // beyond that extend the row inline so a 6th attempt simply pushes
  // the trailing empty slots out of the way.
  const slots = Math.max(5, attempts.length);
  return (
    <View style={styles.penaltyRow}>
      {Array.from({ length: slots }).map((_, i) => {
        const attempt = i < attempts.length ? attempts[i] : null;
        const bg =
          attempt === true
            ? c.success
            : attempt === false
              ? c.danger
              : c.borderSoft;
        return (
          <View
            key={i}
            style={[styles.penaltyDot, { backgroundColor: bg }]}
          />
        );
      })}
    </View>
  );
}

// Per-side penalty attempts during a shootout. Returns boolean array
// (true = scored, false = missed) in chronological order, capped at
// the 5-spot regulation length the UI renders.
function collectShootoutAttempts(
  events: FixtureEvent[] | undefined,
  side: 'home' | 'away',
): boolean[] {
  if (!events) return [];
  const out: boolean[] = [];
  for (const e of events) {
    if (e.participant_location !== side) continue;
    const code = (e.type_code ?? '').toUpperCase();
    if (!code.includes('PENALTY_SHOOTOUT')) continue;
    // Three flavors observed in the wild — accept any of them. Default
    // to "missed" if we can't tell, so a stray event doesn't fake a
    // shootout goal.
    const scored =
      code.includes('GOAL') ||
      (e.result ?? '').toUpperCase() === 'GOAL' ||
      (e.result ?? '').toUpperCase() === 'SCORED';
    out.push(scored);
  }
  return out;
}

function findHalfScore(
  scores: FixtureScore[] | undefined,
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

// Two-up weather strip flanking the kickoff pill. Left side carries the
// human-readable conditions (temp + description), right side the dynamic
// readings (wind + humidity) — kept short so the row stays a single line
// on a 360dp screen even when the description runs long.
function WeatherSide({
  weather,
  side,
}: {
  weather: FixtureWeather | null | undefined;
  side: 'left' | 'right';
}) {
  const c = useTheme();
  if (!weather) {
    return <View style={[styles.weatherSide, side === 'left' ? styles.weatherSideLeft : styles.weatherSideRight]} />;
  }
  if (side === 'left') {
    const temp = weather.temperature_evening ?? weather.temperature_day;
    const unit = weather.metric === 'fahrenheit' ? '°F' : '°C';
    const tempText = temp != null ? `${Math.round(temp)}${unit}` : null;
    return (
      <View style={[styles.weatherSide, styles.weatherSideLeft]}>
        <View style={styles.weatherTempRow}>
          {weather.icon ? (
            <Image
              source={{ uri: weather.icon }}
              style={styles.weatherIcon}
              contentFit="contain"
            />
          ) : null}
          {tempText ? (
            <ThemedText
              style={[styles.weatherTemp, { color: c.text }]}
              numberOfLines={1}>
              {tempText}
            </ThemedText>
          ) : null}
        </View>
      </View>
    );
  }
  const wind = weather.wind_speed != null ? `${weather.wind_speed.toFixed(1)} m/s` : null;
  const humidity = weather.humidity ?? null;
  return (
    <View style={[styles.weatherSide, styles.weatherSideRight]}>
      <ThemedText
        style={[styles.weatherText, { color: c.textMuted, textAlign: 'right' }]}
        numberOfLines={1}>
        {[wind, humidity].filter(Boolean).join(' • ')}
      </ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    paddingTop: 12,
    paddingBottom: 20,
    paddingHorizontal: 16,
    gap: 20,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  kickoffRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 8,
  },
  weatherSide: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  weatherSideLeft: {
    justifyContent: 'flex-start',
  },
  weatherSideRight: {
    justifyContent: 'flex-end',
  },
  weatherIcon: {
    width: 22,
    height: 22,
  },
  weatherTempRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  weatherTemp: {
    fontSize: 16,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  weatherText: {
    fontSize: 11,
    fontWeight: '500',
    flexShrink: 1,
  },
  kickoffPill: {
    paddingHorizontal: 14,
    paddingVertical: 6,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
  },
  kickoffText: {
    fontSize: 13,
    fontWeight: '600',
    letterSpacing: 0.3,
    fontVariant: ['tabular-nums'],
  },
  mainRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
  },
  teamColumn: {
    flex: 1,
    alignItems: 'center',
    gap: 8,
  },
  star: {
    fontSize: 18,
    lineHeight: 18,
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
    fontSize: 14,
    fontWeight: '600',
    textAlign: 'center',
  },
  centerColumn: {
    minWidth: 110,
    alignItems: 'center',
    paddingTop: 24,
    gap: 4,
  },
  scoreRow: {
    flexDirection: 'row',
    alignItems: 'baseline',
    gap: 8,
  },
  scoreText: {
    fontSize: 36,
    lineHeight: 40,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  scoreSeparator: {
    fontSize: 28,
    lineHeight: 40,
    fontWeight: '500',
  },
  kickoffTime: {
    fontSize: 26,
    lineHeight: 30,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
  },
  halfScore: {
    fontSize: 11,
    fontWeight: '600',
    letterSpacing: 0.3,
    fontVariant: ['tabular-nums'],
  },
  statusText: {
    fontSize: 12,
    fontWeight: '600',
    letterSpacing: 0.4,
    marginTop: 2,
  },
  subScore: {
    fontSize: 11,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
    letterSpacing: 0.3,
    marginTop: 1,
  },
  penaltyRow: {
    flexDirection: 'row',
    gap: 4,
    marginTop: 6,
    justifyContent: 'center',
  },
  penaltyDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
  },
  goalSplit: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    marginTop: -4,
  },
  goalGap: {
    width: 24,
  },
  goalSideHome: {
    flex: 1,
    alignItems: 'flex-end',
    gap: 3,
  },
  goalSideAway: {
    flex: 1,
    alignItems: 'flex-start',
    gap: 3,
  },
  goalRowHome: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    maxWidth: '100%',
  },
  goalRowAway: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    maxWidth: '100%',
  },
  goalText: {
    fontSize: 11,
    fontWeight: '500',
    flexShrink: 1,
  },
  goalTextHome: {
    textAlign: 'right',
  },
  goalTextAway: {
    textAlign: 'left',
  },
  goalBall: {
    fontSize: 11,
  },
});
