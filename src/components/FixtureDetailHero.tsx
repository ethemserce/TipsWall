import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { router } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { getStateBucket, getStateLabel } from '@/src/lib/fixtureState';
import { useTheme } from '@/src/lib/useTheme';
import type { Country } from '@/src/types/country';
import type { FixtureSummary } from '@/src/types/fixture';
import type { FixtureScore } from '@/src/types/fixtureDetail';
import type { FixtureEvent } from '@/src/types/fixtureDetailExtras';
import type { League } from '@/src/types/league';

interface FixtureDetailHeroProps {
  fixture: FixtureSummary;
  league?: League;
  country?: Country;
  scores?: FixtureScore[];
  events?: FixtureEvent[];
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

  // Halftime score: only show once 1st half is over (state_id 2 == in 1H).
  const firstHalfPart =
    scored && fixture.state_id !== 2
      ? findHalfScore(scores, '1ST_HALF')
      : null;

  const goals = (events ?? []).filter((e) =>
    GOAL_TYPE_CODES.has((e.type_code ?? '').toUpperCase()),
  );
  const homeGoals = goals.filter((g) => g.participant_location === 'home');
  const awayGoals = goals.filter((g) => g.participant_location === 'away');

  return (
    <View
      style={[
        styles.container,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      {kickoffPill ? (
        <View style={styles.kickoffRow}>
          <View style={[styles.kickoffPill, { backgroundColor: c.bg, borderColor: c.border }]}>
            <ThemedText style={[styles.kickoffText, { color: c.text }]}>
              {kickoffPill}
            </ThemedText>
          </View>
        </View>
      ) : null}

      <View style={styles.mainRow}>
        <TeamColumn
          teamId={fixture.home_team_id}
          name={fixture.home_team_name}
          imagePath={fixture.home_team_image_path}
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

          {scored ? (
            <ThemedText
              style={[
                styles.statusText,
                { color: live ? c.live : c.textMuted },
              ]}>
              {live && fixture.live_minute != null
                ? `${fixture.live_minute}'`
                : firstHalfPart
                  ? t('fixture.hero.halfTimeShort', {
                      home: firstHalfPart.home,
                      away: firstHalfPart.away,
                    })
                  : stateLabel}
            </ThemedText>
          ) : null}
        </View>

        <TeamColumn
          teamId={fixture.away_team_id}
          name={fixture.away_team_name}
          imagePath={fixture.away_team_image_path}
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
}: {
  teamId: number | null | undefined;
  name: string | null | undefined;
  imagePath: string | null | undefined;
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
    </View>
  );
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

const styles = StyleSheet.create({
  container: {
    paddingTop: 12,
    paddingBottom: 20,
    paddingHorizontal: 16,
    gap: 20,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  kickoffRow: {
    alignItems: 'center',
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
