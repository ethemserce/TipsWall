import { useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { TabEmpty, TabError, TabLoading } from '@/src/components/TabFeedback';
import { useTheme } from '@/src/lib/useTheme';
import type {
  FixtureLineupPlayer,
  FixtureLineups,
  FixtureTeamLineup,
} from '@/src/types/fixtureDetailExtras';

interface LineupsTabProps {
  loading: boolean;
  error?: unknown;
  lineups: FixtureLineups | null;
  homeName?: string | null;
  awayName?: string | null;
}

type Side = 'home' | 'away';

export function LineupsTab({
  loading,
  error,
  lineups,
  homeName,
  awayName,
}: LineupsTabProps) {
  const [side, setSide] = useState<Side>('home');

  if (error && !lineups) return <TabError error={error} />;
  if (loading && !lineups) return <TabLoading />;
  if (!lineups || (!lineups.home && !lineups.away))
    return <TabEmpty message="Lineups not available yet." />;

  // Default to whichever side has data when one is missing.
  const hasHome = lineups.home != null;
  const hasAway = lineups.away != null;
  const effective: Side = side === 'home' && !hasHome ? 'away' : side === 'away' && !hasAway ? 'home' : side;
  const team = effective === 'home' ? lineups.home : lineups.away;

  return (
    <>
      <SideToggle
        side={effective}
        onSelect={setSide}
        homeName={homeName ?? 'Home'}
        awayName={awayName ?? 'Away'}
        homeEnabled={hasHome}
        awayEnabled={hasAway}
      />
      {team ? <TeamLineupCard team={team} /> : null}
    </>
  );
}

function SideToggle({
  side,
  onSelect,
  homeName,
  awayName,
  homeEnabled,
  awayEnabled,
}: {
  side: Side;
  onSelect: (s: Side) => void;
  homeName: string;
  awayName: string;
  homeEnabled: boolean;
  awayEnabled: boolean;
}) {
  const c = useTheme();
  return (
    <View style={[styles.toggle, { backgroundColor: c.surface, borderColor: c.border }]}>
      <ToggleButton
        active={side === 'home'}
        disabled={!homeEnabled}
        label={homeName}
        onPress={() => homeEnabled && onSelect('home')}
      />
      <ToggleButton
        active={side === 'away'}
        disabled={!awayEnabled}
        label={awayName}
        onPress={() => awayEnabled && onSelect('away')}
      />
    </View>
  );
}

function ToggleButton({
  active,
  disabled,
  label,
  onPress,
}: {
  active: boolean;
  disabled: boolean;
  label: string;
  onPress: () => void;
}) {
  const c = useTheme();
  return (
    <Pressable
      onPress={onPress}
      disabled={disabled}
      style={[
        styles.toggleButton,
        active && { backgroundColor: c.brand },
      ]}>
      <ThemedText
        numberOfLines={1}
        style={[
          styles.toggleText,
          { color: active ? c.textInverse : disabled ? c.border : c.text },
        ]}>
        {label}
      </ThemedText>
    </Pressable>
  );
}

function TeamLineupCard({ team }: { team: FixtureTeamLineup }) {
  const c = useTheme();
  const hasStarters = team.starters.length > 0;
  const hasBench = team.bench.length > 0;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      {team.formation ? (
        <View style={styles.header}>
          <ThemedText style={[styles.headerLabel, { color: c.textMuted }]}>
            FORMATION
          </ThemedText>
          <View style={[styles.formationPill, { backgroundColor: c.bg, borderColor: c.border }]}>
            <ThemedText style={[styles.formationText, { color: c.text }]}>
              {team.formation}
            </ThemedText>
          </View>
        </View>
      ) : null}

      {hasStarters ? <PlayerSection title="Starting XI" players={team.starters} /> : null}
      {hasBench ? <PlayerSection title="Bench" players={team.bench} /> : null}
    </View>
  );
}

function PlayerSection({
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
  toggle: {
    flexDirection: 'row',
    marginHorizontal: 16,
    marginTop: 16,
    padding: 4,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
    gap: 4,
  },
  toggleButton: {
    flex: 1,
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 999,
    alignItems: 'center',
  },
  toggleText: {
    fontSize: 13,
    fontWeight: '600',
  },
  card: {
    marginHorizontal: 16,
    marginTop: 12,
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
  headerLabel: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.5,
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
});
