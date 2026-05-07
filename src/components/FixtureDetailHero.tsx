import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { getStateBucket, getStateLabel } from '@/src/lib/fixtureState';
import { useTheme } from '@/src/lib/useTheme';
import type { Country } from '@/src/types/country';
import type { FixtureSummary } from '@/src/types/fixture';
import type { League } from '@/src/types/league';

interface FixtureDetailHeroProps {
  fixture: FixtureSummary;
  league?: League;
  country?: Country;
}

export function FixtureDetailHero({
  fixture,
  league,
  country,
}: FixtureDetailHeroProps) {
  const c = useTheme();
  const bucket = getStateBucket(fixture.state_id);
  const live = bucket === 'live';
  const finished = bucket === 'finished';
  const scored = live || finished;

  const stateLabel = getStateLabel(fixture.state_id);
  const kickoffDate = fixture.starting_at
    ? format(parseISO(fixture.starting_at), 'd MMM yyyy')
    : null;
  const kickoffTime = fixture.starting_at
    ? format(parseISO(fixture.starting_at), 'HH:mm')
    : null;

  return (
    <View style={[styles.container, { backgroundColor: c.surface, borderColor: c.border }]}>
      <View style={styles.leagueRow}>
        {country?.image_path ? (
          <Image
            source={{ uri: country.image_path }}
            style={styles.leagueFlag}
            contentFit="cover"
          />
        ) : league?.image_path ? (
          <Image
            source={{ uri: league.image_path }}
            style={styles.leagueLogo}
            contentFit="contain"
          />
        ) : null}
        <ThemedText
          style={[styles.leagueName, { color: c.textMuted }]}
          numberOfLines={1}>
          {league?.name ?? `League #${fixture.league_id}`}
        </ThemedText>
      </View>

      <View style={styles.teamsRow}>
        <TeamColumn
          name={fixture.home_team_name}
          imagePath={fixture.home_team_image_path}
        />

        <View style={styles.scoreColumn}>
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
                :
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
        </View>

        <TeamColumn
          name={fixture.away_team_name}
          imagePath={fixture.away_team_image_path}
        />
      </View>

      <View style={styles.statusRow}>
        {live ? (
          <View style={[styles.statusPill, { backgroundColor: c.live }]}>
            <View style={[styles.dot, { backgroundColor: c.textInverse }]} />
            <ThemedText style={[styles.statusText, { color: c.textInverse }]}>
              {stateLabel || 'LIVE'}
            </ThemedText>
          </View>
        ) : finished ? (
          <View style={[styles.statusPillSubtle, { backgroundColor: c.bg }]}>
            <ThemedText style={[styles.statusText, { color: c.text }]}>
              {stateLabel || 'FT'}
            </ThemedText>
          </View>
        ) : kickoffDate ? (
          <ThemedText style={[styles.kickoffDate, { color: c.textMuted }]}>
            {kickoffDate}
          </ThemedText>
        ) : null}
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
    paddingTop: 16,
    paddingBottom: 20,
    paddingHorizontal: 16,
    gap: 16,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  leagueRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
  },
  leagueFlag: {
    width: 18,
    height: 12,
    borderRadius: 2,
  },
  leagueLogo: {
    width: 16,
    height: 16,
  },
  leagueName: {
    fontSize: 12,
    fontWeight: '600',
    letterSpacing: 0.3,
    textTransform: 'uppercase',
  },
  teamsRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
  },
  teamColumn: {
    flex: 1,
    alignItems: 'center',
    gap: 10,
  },
  teamLogo: {
    width: 72,
    height: 72,
  },
  teamLogoPlaceholder: {
    width: 72,
    height: 72,
    borderRadius: 8,
  },
  teamName: {
    fontSize: 15,
    fontWeight: '600',
    textAlign: 'center',
  },
  scoreColumn: {
    minWidth: 100,
    alignItems: 'center',
    justifyContent: 'center',
    paddingTop: 12,
  },
  scoreRow: {
    flexDirection: 'row',
    alignItems: 'baseline',
    gap: 8,
  },
  scoreText: {
    fontSize: 38,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  scoreSeparator: {
    fontSize: 32,
    fontWeight: '500',
  },
  kickoffTime: {
    fontSize: 28,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
  },
  kickoffDate: {
    fontSize: 13,
    fontWeight: '500',
  },
  statusRow: {
    alignItems: 'center',
    minHeight: 22,
  },
  statusPill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 999,
  },
  statusPillSubtle: {
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
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
});
