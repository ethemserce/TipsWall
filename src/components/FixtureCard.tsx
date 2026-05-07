import { format, parseISO } from 'date-fns';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import type { FixtureSummary } from '@/src/types/fixture';

interface FixtureCardProps {
  fixture: FixtureSummary;
}

export function FixtureCard({ fixture }: FixtureCardProps) {
  const kickoff = fixture.starting_at
    ? format(parseISO(fixture.starting_at), 'HH:mm')
    : '--:--';

  const [home, away] = splitName(fixture.name);

  return (
    <ThemedView style={styles.card}>
      <View style={styles.timeColumn}>
        <ThemedText style={styles.time}>{kickoff}</ThemedText>
        {fixture.has_odds ? <View style={styles.oddsDot} /> : null}
      </View>
      <View style={styles.teams}>
        <ThemedText style={styles.team} numberOfLines={1}>
          {home}
        </ThemedText>
        <ThemedText style={styles.team} numberOfLines={1}>
          {away}
        </ThemedText>
      </View>
      <View style={styles.meta}>
        <ThemedText style={styles.metaText}>
          League #{fixture.league_id}
        </ThemedText>
      </View>
    </ThemedView>
  );
}

function splitName(name: string | null): [string, string] {
  if (!name) return ['TBD', 'TBD'];
  const parts = name.split(/\s+vs\.?\s+/i);
  if (parts.length === 2) return [parts[0], parts[1]];
  return [name, ''];
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: '#2a2a2a',
    gap: 12,
  },
  timeColumn: {
    width: 56,
    alignItems: 'center',
    gap: 4,
  },
  time: {
    fontSize: 15,
    fontWeight: '600',
  },
  oddsDot: {
    width: 6,
    height: 6,
    borderRadius: 3,
    backgroundColor: '#22c55e',
  },
  teams: {
    flex: 1,
    gap: 4,
  },
  team: {
    fontSize: 15,
  },
  meta: {
    width: 80,
    alignItems: 'flex-end',
  },
  metaText: {
    fontSize: 11,
    opacity: 0.6,
  },
});
