import { format, parseISO } from 'date-fns';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { getStateBucket, getStateLabel } from '@/src/lib/fixtureState';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureSummary } from '@/src/types/fixture';

interface FixtureCardProps {
  fixture: FixtureSummary;
}

export function FixtureCard({ fixture }: FixtureCardProps) {
  const c = useTheme();
  const bucket = getStateBucket(fixture.state_id);
  const live = bucket === 'live';
  const finished = bucket === 'finished';

  const kickoff = fixture.starting_at
    ? format(parseISO(fixture.starting_at), 'HH:mm')
    : '--:--';
  const stateLabel = getStateLabel(fixture.state_id);
  const [home, away] = splitName(fixture.name);

  const statusColor = live ? c.live : finished ? c.textMuted : c.text;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.bg, borderBottomColor: c.border },
      ]}>
      <View style={styles.timeColumn}>
        {live ? (
          <View style={styles.liveRow}>
            <View style={[styles.liveDot, { backgroundColor: c.live }]} />
            <ThemedText style={[styles.timeStrong, { color: c.live }]}>{stateLabel || 'LIVE'}</ThemedText>
          </View>
        ) : (
          <ThemedText style={[styles.timeStrong, { color: statusColor }]}>{kickoff}</ThemedText>
        )}
        {finished ? (
          <ThemedText style={[styles.timeSub, { color: c.textMuted }]}>{stateLabel || 'FT'}</ThemedText>
        ) : null}
      </View>

      <View style={styles.teams}>
        <ThemedText
          style={[styles.team, finished && { color: c.textMuted }]}
          numberOfLines={1}>
          {home}
        </ThemedText>
        <ThemedText
          style={[styles.team, finished && { color: c.textMuted }]}
          numberOfLines={1}>
          {away}
        </ThemedText>
      </View>

      <View style={styles.meta}>
        {fixture.has_odds ? (
          <View style={[styles.oddsBadge, { backgroundColor: c.brand }]}>
            <ThemedText style={[styles.oddsBadgeText, { color: c.textInverse }]}>
              ODDS
            </ThemedText>
          </View>
        ) : null}
      </View>
    </View>
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
    gap: 12,
  },
  timeColumn: {
    width: 60,
    alignItems: 'flex-start',
    gap: 2,
  },
  timeStrong: {
    fontSize: 14,
    fontWeight: '600',
  },
  timeSub: {
    fontSize: 11,
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
    gap: 4,
  },
  team: {
    fontSize: 15,
  },
  meta: {
    width: 56,
    alignItems: 'flex-end',
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
