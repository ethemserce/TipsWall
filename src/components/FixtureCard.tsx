import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { useRouter } from 'expo-router';
import { Pressable, StyleSheet, View } from 'react-native';

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
          backgroundColor: pressed ? c.surface : c.bg,
          borderBottomColor: c.border,
        },
      ]}>
      <View style={styles.timeColumn}>
        {live ? (
          <View style={styles.liveRow}>
            <View style={[styles.liveDot, { backgroundColor: c.live }]} />
            <ThemedText style={[styles.timeStrong, { color: c.live }]}>
              {stateLabel || 'LIVE'}
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
        <TeamRow team={home} dimmed={finished && !homeWinner} />
        <TeamRow team={away} dimmed={finished && !awayWinner} />
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
        ) : fixture.has_odds ? (
          <View style={[styles.oddsBadge, { backgroundColor: c.brand }]}>
            <ThemedText style={[styles.oddsBadgeText, { color: c.textInverse }]}>
              ODDS
            </ThemedText>
          </View>
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

function TeamRow({ team, dimmed }: { team: TeamInfo; dimmed: boolean }) {
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
    </View>
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
    borderBottomWidth: StyleSheet.hairlineWidth,
    gap: 12,
  },
  timeColumn: {
    width: 56,
    alignItems: 'flex-start',
    justifyContent: 'center',
  },
  timeStrong: {
    fontSize: 13,
    fontWeight: '600',
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
  oddsBadge: {
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 4,
  },
  oddsBadgeText: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
});
