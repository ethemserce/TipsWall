import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { StyleSheet, View } from 'react-native';

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

          {firstHalfPart ? (
            <ThemedText style={[styles.halfScore, { color: c.textMuted }]}>
              HT {firstHalfPart.home}-{firstHalfPart.away}
            </ThemedText>
          ) : null}

          {scored ? (
            <ThemedText
              style={[
                styles.statusText,
                { color: live ? c.live : c.textMuted },
              ]}>
              {stateLabel}
            </ThemedText>
          ) : null}

          {goals.length > 0 ? (
            <View style={styles.goalList}>
              {goals.map((g) => (
                <GoalLine key={g.id} goal={g} />
              ))}
            </View>
          ) : null}
        </View>

        <TeamColumn
          name={fixture.away_team_name}
          imagePath={fixture.away_team_image_path}
        />
      </View>
    </View>
  );
}

function GoalLine({ goal }: { goal: FixtureEvent }) {
  const c = useTheme();
  const code = (goal.type_code ?? '').toUpperCase();
  const dotColor =
    goal.participant_location === 'home' ? c.brand : c.live;
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

  return (
    <View style={styles.goalRow}>
      <View style={[styles.goalDot, { backgroundColor: dotColor }]} />
      <ThemedText
        style={[styles.goalText, { color: c.textMuted }]}
        numberOfLines={1}>
        {goal.player_name ?? '—'} {minuteStr}
        {tag ? ` ${tag}` : ''} ⚽
      </ThemedText>
    </View>
  );
}

function TeamColumn({
  name,
  imagePath,
}: {
  name: string | null | undefined;
  imagePath: string | null | undefined;
}) {
  const c = useTheme();
  return (
    <View style={styles.teamColumn}>
      <ThemedText style={[styles.star, { color: c.textMuted }]}>☆</ThemedText>
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
      <ThemedText
        style={[styles.teamName, { color: c.text }]}
        numberOfLines={2}>
        {name ?? 'TBD'}
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
    gap: 16,
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
    paddingTop: 18,
    gap: 4,
  },
  scoreRow: {
    flexDirection: 'row',
    alignItems: 'baseline',
    gap: 8,
  },
  scoreText: {
    fontSize: 36,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  scoreSeparator: {
    fontSize: 28,
    fontWeight: '500',
  },
  kickoffTime: {
    fontSize: 26,
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
  goalList: {
    marginTop: 6,
    gap: 3,
    alignItems: 'center',
  },
  goalRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    maxWidth: 180,
  },
  goalDot: {
    width: 6,
    height: 6,
    borderRadius: 3,
  },
  goalText: {
    fontSize: 11,
    fontWeight: '500',
    flexShrink: 1,
  },
});
