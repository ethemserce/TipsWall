import { ActivityIndicator, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type {
  FixtureLineupPlayer,
  FixtureLineups,
  FixtureTeamLineup,
} from '@/src/types/fixtureDetailExtras';

interface LineupsTabProps {
  loading: boolean;
  lineups: FixtureLineups | null;
  homeName?: string | null;
  awayName?: string | null;
}

export function LineupsTab({ loading, lineups, homeName, awayName }: LineupsTabProps) {
  const c = useTheme();

  if (loading && !lineups) {
    return (
      <View style={styles.empty}>
        <ActivityIndicator color={c.brand} />
      </View>
    );
  }

  if (!lineups || (!lineups.home && !lineups.away)) {
    return (
      <View style={styles.empty}>
        <ThemedText style={[styles.emptyText, { color: c.textMuted }]}>
          Lineups not available yet.
        </ThemedText>
      </View>
    );
  }

  return (
    <>
      {lineups.home ? (
        <TeamLineupCard
          team={lineups.home}
          title={homeName ?? 'Home'}
        />
      ) : null}
      {lineups.away ? (
        <TeamLineupCard
          team={lineups.away}
          title={awayName ?? 'Away'}
        />
      ) : null}
    </>
  );
}

function TeamLineupCard({
  team,
  title,
}: {
  team: FixtureTeamLineup;
  title: string;
}) {
  const c = useTheme();
  const hasStarters = team.starters.length > 0;
  const hasBench = team.bench.length > 0;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <View style={styles.header}>
        <ThemedText style={[styles.teamTitle, { color: c.text }]}>
          {title.toUpperCase()}
        </ThemedText>
        {team.formation ? (
          <View style={[styles.formationPill, { backgroundColor: c.bg, borderColor: c.border }]}>
            <ThemedText style={[styles.formationText, { color: c.text }]}>
              {team.formation}
            </ThemedText>
          </View>
        ) : null}
      </View>

      {hasStarters ? (
        <SectionList
          title="Starting XI"
          players={team.starters}
        />
      ) : null}

      {hasBench ? (
        <SectionList title="Bench" players={team.bench} />
      ) : null}
    </View>
  );
}

function SectionList({
  title,
  players,
}: {
  title: string;
  players: FixtureLineupPlayer[];
}) {
  const c = useTheme();
  return (
    <View>
      <ThemedText
        style={[styles.sectionLabel, { color: c.textMuted, borderTopColor: c.border }]}>
        {title.toUpperCase()}
      </ThemedText>
      {players.map((p, i) => (
        <View
          key={`${p.player_id ?? p.player_name ?? i}-${i}`}
          style={[styles.playerRow, { borderTopColor: c.border }]}>
          <ThemedText style={[styles.jersey, { color: c.textMuted }]}>
            {p.jersey_number ?? '–'}
          </ThemedText>
          <ThemedText
            style={[styles.playerName, { color: c.text }]}
            numberOfLines={1}>
            {p.player_name ?? '—'}
          </ThemedText>
          <ThemedText
            style={[styles.position, { color: c.textMuted }]}
            numberOfLines={1}>
            {p.position_code ?? p.formation_field ?? ''}
          </ThemedText>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    overflow: 'hidden',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 14,
    paddingTop: 12,
    paddingBottom: 8,
  },
  teamTitle: {
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0.4,
  },
  formationPill: {
    paddingHorizontal: 10,
    paddingVertical: 3,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
  },
  formationText: {
    fontSize: 12,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
  },
  sectionLabel: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.5,
    paddingHorizontal: 14,
    paddingTop: 10,
    paddingBottom: 6,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  playerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 12,
  },
  jersey: {
    width: 28,
    fontSize: 13,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
    textAlign: 'center',
  },
  playerName: {
    flex: 1,
    fontSize: 14,
  },
  position: {
    fontSize: 11,
    fontWeight: '600',
  },
  empty: {
    paddingVertical: 64,
    alignItems: 'center',
  },
  emptyText: {
    fontSize: 14,
  },
});
