import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type {
  FixtureLineupPlayer,
  FixtureTeamLineup,
} from '@/src/types/fixtureDetailExtras';

interface PitchViewProps {
  home: FixtureTeamLineup | null | undefined;
  away: FixtureTeamLineup | null | undefined;
}

const PITCH_BG = '#1f3a2a';
const PITCH_BG_DARK = '#16291e';
const PITCH_LINE = 'rgba(255,255,255,0.25)';

export function PitchView({ home, away }: PitchViewProps) {
  return (
    <View style={styles.wrapper}>
      <View style={styles.pitch}>
        <View style={styles.halfwayLine} />
        <View style={styles.centerCircle} />
        <View style={styles.centerSpot} />
        <View style={[styles.box, styles.boxTop]} />
        <View style={[styles.smallBox, styles.smallBoxTop]} />
        <View style={[styles.box, styles.boxBottom]} />
        <View style={[styles.smallBox, styles.smallBoxBottom]} />

        {home?.starters?.length ? (
          <TeamLayer team={home} side="home" />
        ) : null}
        {away?.starters?.length ? (
          <TeamLayer team={away} side="away" />
        ) : null}
      </View>
    </View>
  );
}

function TeamLayer({
  team,
  side,
}: {
  team: FixtureTeamLineup;
  side: 'home' | 'away';
}) {
  const positioned = team.starters
    .map((p) => parsePosition(p))
    .filter((x): x is { player: FixtureLineupPlayer; row: number; col: number } => x !== null);

  if (positioned.length === 0) return null;

  const maxRow = Math.max(...positioned.map((p) => p.row));
  const colsByRow = new Map<number, number>();
  for (const { row, col } of positioned) {
    colsByRow.set(row, Math.max(colsByRow.get(row) ?? 0, col));
  }

  // Inset the player tokens away from the pitch edges so the jersey circle
  // and the name caption stay fully inside the playing area.
  const X_INSET = 0.14;
  const Y_INSET = 0.08;
  const X_RANGE = 1 - 2 * X_INSET;
  const HALF_RANGE = 0.5 - Y_INSET;

  return (
    <>
      {positioned.map(({ player, row, col }) => {
        const colCount = colsByRow.get(row) ?? 1;
        const xRatio = colCount > 1 ? (col - 1) / (colCount - 1) : 0.5;
        const yRatio = maxRow > 1 ? (row - 1) / (maxRow - 1) : 0;
        // Mirror x for away so col 1 lands on the same visual side as home col 1.
        const xCentered =
          side === 'home'
            ? X_INSET + xRatio * X_RANGE
            : 1 - X_INSET - xRatio * X_RANGE;
        const yCentered =
          side === 'home'
            ? Y_INSET + yRatio * HALF_RANGE
            : 1 - Y_INSET - yRatio * HALF_RANGE;
        return (
          <PlayerToken
            key={`${side}-${player.player_id ?? player.player_name ?? player.jersey_number}-${row}-${col}`}
            player={player}
            leftPercent={xCentered * 100}
            topPercent={yCentered * 100}
          />
        );
      })}
    </>
  );
}

function parsePosition(player: FixtureLineupPlayer) {
  if (!player.formation_field) return null;
  const parts = player.formation_field.split(':').map((s) => Number(s.trim()));
  if (parts.length !== 2 || parts.some((n) => !Number.isFinite(n) || n <= 0)) return null;
  return { player, row: parts[0], col: parts[1] };
}

function PlayerToken({
  player,
  leftPercent,
  topPercent,
}: {
  player: FixtureLineupPlayer;
  leftPercent: number;
  topPercent: number;
}) {
  const c = useTheme();
  return (
    <View
      style={[
        styles.token,
        {
          left: `${leftPercent}%`,
          top: `${topPercent}%`,
        },
      ]}>
      <View style={[styles.jerseyCircle, { backgroundColor: c.bg, borderColor: c.brand }]}>
        <ThemedText style={[styles.jerseyNumber, { color: c.text }]}>
          {player.jersey_number ?? '–'}
        </ThemedText>
      </View>
      <View style={styles.nameWrap}>
        <ThemedText
          style={styles.nameText}
          numberOfLines={1}>
          {shortName(player.player_name)}
        </ThemedText>
      </View>
    </View>
  );
}

function shortName(name: string | null | undefined): string {
  if (!name) return '—';
  const trimmed = name.trim();
  const parts = trimmed.split(/\s+/);
  if (parts.length === 1) return parts[0];
  // "Andreas Cornelius" → "A. Cornelius"
  const first = parts[0];
  const last = parts[parts.length - 1];
  return `${first.charAt(0).toUpperCase()}. ${last}`;
}

const styles = StyleSheet.create({
  wrapper: {
    marginHorizontal: 16,
    marginTop: 12,
    borderRadius: 12,
    overflow: 'hidden',
    backgroundColor: PITCH_BG,
  },
  pitch: {
    width: '100%',
    aspectRatio: 0.65,
    backgroundColor: PITCH_BG,
    position: 'relative',
    overflow: 'hidden',
  },
  halfwayLine: {
    position: 'absolute',
    top: '50%',
    left: 0,
    right: 0,
    height: 1,
    backgroundColor: PITCH_LINE,
  },
  centerCircle: {
    position: 'absolute',
    top: '50%',
    left: '50%',
    width: 88,
    height: 88,
    marginLeft: -44,
    marginTop: -44,
    borderRadius: 44,
    borderWidth: 1,
    borderColor: PITCH_LINE,
  },
  centerSpot: {
    position: 'absolute',
    top: '50%',
    left: '50%',
    width: 4,
    height: 4,
    marginLeft: -2,
    marginTop: -2,
    borderRadius: 2,
    backgroundColor: PITCH_LINE,
  },
  box: {
    position: 'absolute',
    left: '20%',
    right: '20%',
    height: '14%',
    borderWidth: 1,
    borderColor: PITCH_LINE,
    backgroundColor: PITCH_BG_DARK,
  },
  boxTop: {
    top: 0,
    borderTopWidth: 0,
  },
  boxBottom: {
    bottom: 0,
    borderBottomWidth: 0,
  },
  smallBox: {
    position: 'absolute',
    left: '34%',
    right: '34%',
    height: '6%',
    borderWidth: 1,
    borderColor: PITCH_LINE,
  },
  smallBoxTop: {
    top: 0,
    borderTopWidth: 0,
  },
  smallBoxBottom: {
    bottom: 0,
    borderBottomWidth: 0,
  },
  token: {
    position: 'absolute',
    width: 60,
    marginLeft: -30,
    marginTop: -22,
    alignItems: 'center',
    overflow: 'hidden',
  },
  jerseyCircle: {
    width: 30,
    height: 30,
    borderRadius: 15,
    borderWidth: 2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  jerseyNumber: {
    fontSize: 12,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  nameWrap: {
    marginTop: 2,
    width: '100%',
  },
  nameText: {
    color: '#ffffff',
    fontSize: 10,
    fontWeight: '600',
    textAlign: 'center',
    textShadowColor: 'rgba(0,0,0,0.6)',
    textShadowOffset: { width: 0, height: 1 },
    textShadowRadius: 2,
  },
});
