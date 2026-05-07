import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { getStateBucket, getStateLabel } from '@/src/lib/fixtureState';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureSummary } from '@/src/types/fixture';

interface FixtureDetailHeroProps {
  fixture: FixtureSummary;
}

export function FixtureDetailHero({ fixture }: FixtureDetailHeroProps) {
  const c = useTheme();
  const bucket = getStateBucket(fixture.state_id);
  const live = bucket === 'live';
  const finished = bucket === 'finished';
  const scored = live || finished;

  const stateLabel = getStateLabel(fixture.state_id);
  const kickoff = fixture.starting_at
    ? format(parseISO(fixture.starting_at), 'EEE, d MMM • HH:mm')
    : null;

  return (
    <View style={[styles.container, { backgroundColor: c.surface }]}>
      <View style={styles.statusRow}>
        {live ? (
          <View style={[styles.statusPill, { backgroundColor: c.live }]}>
            <View style={[styles.dot, { backgroundColor: c.textInverse }]} />
            <ThemedText style={[styles.statusText, { color: c.textInverse }]}>
              {stateLabel || 'LIVE'}
            </ThemedText>
          </View>
        ) : finished ? (
          <View style={[styles.statusPill, { backgroundColor: c.border }]}>
            <ThemedText style={[styles.statusText, { color: c.text }]}>
              {stateLabel || 'FT'}
            </ThemedText>
          </View>
        ) : kickoff ? (
          <ThemedText style={[styles.kickoff, { color: c.textMuted }]}>
            {kickoff}
          </ThemedText>
        ) : null}
      </View>

      <View style={styles.teamsRow}>
        <TeamColumn
          name={fixture.home_team_name}
          imagePath={fixture.home_team_image_path}
        />

        <View style={styles.scoreColumn}>
          {scored ? (
            <ThemedText style={[styles.scoreText, { color: live ? c.live : c.text }]}>
              {fixture.home_score ?? 0}
              <ThemedText style={[styles.scoreDash, { color: c.textMuted }]}>
                {' - '}
              </ThemedText>
              {fixture.away_score ?? 0}
            </ThemedText>
          ) : (
            <ThemedText style={[styles.vsText, { color: c.textMuted }]}>vs</ThemedText>
          )}
        </View>

        <TeamColumn
          name={fixture.away_team_name}
          imagePath={fixture.away_team_image_path}
        />
      </View>
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

const styles = StyleSheet.create({
  container: {
    paddingVertical: 24,
    paddingHorizontal: 16,
    gap: 16,
  },
  statusRow: {
    alignItems: 'center',
  },
  statusPill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 999,
  },
  dot: {
    width: 6,
    height: 6,
    borderRadius: 3,
  },
  statusText: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.4,
  },
  kickoff: {
    fontSize: 13,
  },
  teamsRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  teamColumn: {
    flex: 1,
    alignItems: 'center',
    gap: 8,
  },
  teamLogo: {
    width: 64,
    height: 64,
  },
  teamLogoPlaceholder: {
    width: 64,
    height: 64,
    borderRadius: 8,
  },
  teamName: {
    fontSize: 14,
    fontWeight: '600',
    textAlign: 'center',
  },
  scoreColumn: {
    minWidth: 80,
    alignItems: 'center',
  },
  scoreText: {
    fontSize: 36,
    fontWeight: '700',
  },
  scoreDash: {
    fontSize: 28,
    fontWeight: '500',
  },
  vsText: {
    fontSize: 18,
    fontWeight: '600',
    letterSpacing: 1,
  },
});
