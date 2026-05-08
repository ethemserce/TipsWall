import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { useRouter } from 'expo-router';
import { useEffect, useRef } from 'react';
import { Animated, Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { getStateBucket, getStateLabel } from '@/src/lib/fixtureState';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureSummary } from '@/src/types/fixture';

interface FixtureCardProps {
  fixture: FixtureSummary;
}

export function FixtureCard({ fixture }: FixtureCardProps) {
  const c = useTheme();
  const router = useRouter();
  const bucket = getStateBucket(fixture.state_id);
  const live = bucket === 'live';
  const finished = bucket === 'finished';
  const showScore = live || finished;

  const kickoff = fixture.starting_at
    ? format(parseISO(fixture.starting_at), 'HH:mm')
    : '--:--';
  const stateLabel = getStateLabel(fixture.state_id);

  const home = teamFromSide(fixture, 'home');
  const away = teamFromSide(fixture, 'away');

  const homeWinner =
    showScore &&
    fixture.home_score != null &&
    fixture.away_score != null &&
    fixture.home_score > fixture.away_score;
  const awayWinner =
    showScore &&
    fixture.home_score != null &&
    fixture.away_score != null &&
    fixture.away_score > fixture.home_score;

  return (
    <Pressable
      onPress={() => router.push(`/fixture/${fixture.id}` as never)}
      style={({ pressed }) => [
        styles.card,
        {
          backgroundColor: pressed ? c.brandSoft : 'transparent',
        },
      ]}>
      <View style={styles.timeColumn}>
        {live ? (
          <View style={styles.liveRow}>
            <View style={[styles.liveDot, { backgroundColor: c.live }]} />
            <ThemedText style={[styles.timeStrong, { color: c.live }]}>
              {fixture.live_minute != null
                ? `${fixture.live_minute}'`
                : stateLabel || 'LIVE'}
            </ThemedText>
          </View>
        ) : finished ? (
          <ThemedText style={[styles.timeStrong, { color: c.textMuted }]}>
            {stateLabel || 'FT'}
          </ThemedText>
        ) : (
          <ThemedText style={[styles.timeStrong, { color: c.text }]}>
            {kickoff}
          </ThemedText>
        )}
      </View>

      <View style={styles.teams}>
        <TeamRow
          team={home}
          dimmed={finished && !homeWinner}
          redCards={fixture.home_red_cards ?? 0}
          varActive={fixture.home_var_active === true}
        />
        <TeamRow
          team={away}
          dimmed={finished && !awayWinner}
          redCards={fixture.away_red_cards ?? 0}
          varActive={fixture.away_var_active === true}
        />
      </View>

      <View style={styles.scoreColumn}>
        {showScore ? (
          <>
            <ScoreText
              value={fixture.home_score}
              live={live}
              winner={homeWinner}
              c={c}
            />
            <ScoreText
              value={fixture.away_score}
              live={live}
              winner={awayWinner}
              c={c}
            />
          </>
        ) : null}
      </View>
    </Pressable>
  );
}

interface TeamInfo {
  name: string;
  imagePath: string | null;
}

function teamFromSide(fixture: FixtureSummary, side: 'home' | 'away'): TeamInfo {
  if (side === 'home') {
    return {
      name: fixture.home_team_name ?? fallbackName(fixture.name, 0),
      imagePath: fixture.home_team_image_path ?? null,
    };
  }
  return {
    name: fixture.away_team_name ?? fallbackName(fixture.name, 1),
    imagePath: fixture.away_team_image_path ?? null,
  };
}

function fallbackName(name: string | null, index: number): string {
  if (!name) return 'TBD';
  const parts = name.split(/\s+vs\.?\s+/i);
  return parts[index] ?? 'TBD';
}

function TeamRow({
  team,
  dimmed,
  redCards,
  varActive,
}: {
  team: TeamInfo;
  dimmed: boolean;
  redCards: number;
  varActive: boolean;
}) {
  const c = useTheme();
  return (
    <View style={styles.teamRow}>
      {team.imagePath ? (
        <Image
          source={{ uri: team.imagePath }}
          style={styles.logo}
          contentFit="contain"
          transition={150}
        />
      ) : (
        <View style={[styles.logoPlaceholder, { backgroundColor: c.border }]} />
      )}
      <ThemedText
        style={[styles.teamName, { color: dimmed ? c.textMuted : c.text }]}
        numberOfLines={1}>
        {team.name}
      </ThemedText>
      {redCards > 0 ? <RedCardBadge count={redCards} /> : null}
      {varActive ? <VarBadge /> : null}
    </View>
  );
}

function RedCardBadge({ count }: { count: number }) {
  return (
    <View style={styles.redCardWrap}>
      <View style={styles.redCard} />
      {count > 1 ? (
        <ThemedText style={styles.redCardCount}>{count}</ThemedText>
      ) : null}
    </View>
  );
}

function VarBadge() {
  const opacity = useRef(new Animated.Value(1)).current;
  useEffect(() => {
    const loop = Animated.loop(
      Animated.sequence([
        Animated.timing(opacity, {
          toValue: 0.25,
          duration: 500,
          useNativeDriver: true,
        }),
        Animated.timing(opacity, {
          toValue: 1,
          duration: 500,
          useNativeDriver: true,
        }),
      ]),
    );
    loop.start();
    return () => loop.stop();
  }, [opacity]);
  return (
    <Animated.View style={[styles.varBadge, { opacity }]}>
      <ThemedText style={styles.varText}>VAR</ThemedText>
    </Animated.View>
  );
}

function ScoreText({
  value,
  live,
  winner,
  c,
}: {
  value: number | null | undefined;
  live: boolean;
  winner: boolean;
  c: ReturnType<typeof useTheme>;
}) {
  const color = live ? c.live : winner ? c.text : c.textMuted;
  return (
    <ThemedText style={[styles.scoreText, { color }]}>
      {value ?? '-'}
    </ThemedText>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 16,
    gap: 12,
  },
  timeColumn: {
    width: 40,
    alignItems: 'flex-start',
    justifyContent: 'center',
  },
  timeStrong: {
    fontSize: 12,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
  },
  liveRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  liveDot: {
    width: 6,
    height: 6,
    borderRadius: 3,
  },
  teams: {
    flex: 1,
    gap: 6,
  },
  teamRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  logo: {
    width: 18,
    height: 18,
  },
  logoPlaceholder: {
    width: 18,
    height: 18,
    borderRadius: 3,
  },
  teamName: {
    fontSize: 14,
    flexShrink: 1,
  },
  redCardWrap: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 2,
  },
  redCard: {
    width: 8,
    height: 11,
    borderRadius: 1,
    backgroundColor: '#E53935',
  },
  redCardCount: {
    fontSize: 11,
    fontWeight: '700',
    color: '#E53935',
  },
  varBadge: {
    backgroundColor: '#E53935',
    paddingHorizontal: 5,
    paddingVertical: 1,
    borderRadius: 3,
  },
  varText: {
    fontSize: 10,
    fontWeight: '800',
    color: '#FFFFFF',
    letterSpacing: 0.5,
  },
  scoreColumn: {
    width: 36,
    alignItems: 'flex-end',
    justifyContent: 'center',
    gap: 6,
  },
  scoreText: {
    fontSize: 15,
    fontWeight: '600',
    minHeight: 18,
  },
});
