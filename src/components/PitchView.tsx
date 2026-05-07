import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useMemo } from 'react';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type {
  FixtureEvent,
  FixtureLineupPlayer,
  FixtureTeamLineup,
} from '@/src/types/fixtureDetailExtras';

interface PitchViewProps {
  home: FixtureTeamLineup | null | undefined;
  away: FixtureTeamLineup | null | undefined;
  events?: FixtureEvent[];
}

interface PlayerMarkers {
  goals: number;
  assists: number;
}

const GOAL_TYPE_CODES = new Set([
  'GOAL',
  'PENALTY',
  'OWNGOAL',
  'GOAL_AWARDED',
]);

interface ScorerKey {
  id: number | null;
  name: string | null;
}

function buildPlayerMarkers(events: FixtureEvent[] | undefined) {
  const goalEvents: ScorerKey[] = [];
  const assistEvents: ScorerKey[] = [];
  if (events) {
    for (const e of events) {
      if (!GOAL_TYPE_CODES.has((e.type_code ?? '').toUpperCase())) continue;
      goalEvents.push({
        id: e.player_id,
        name: e.player_name?.trim().toLowerCase() ?? null,
      });
      if (e.related_player_name) {
        assistEvents.push({
          id: null,
          name: e.related_player_name.trim().toLowerCase(),
        });
      }
    }
  }
  return (player: FixtureLineupPlayer): PlayerMarkers => {
    const pId = player.player_id ?? null;
    const pName = player.player_name?.trim().toLowerCase() ?? null;
    const matches = (k: ScorerKey) =>
      (k.id != null && pId != null && k.id === pId) ||
      (k.name != null && pName != null && k.name === pName);
    return {
      goals: goalEvents.filter(matches).length,
      assists: assistEvents.filter(matches).length,
    };
  };
}

const PITCH_BG = '#1f3a2a';
const PITCH_BG_DARK = '#16291e';
const PITCH_LINE = 'rgba(255,255,255,0.25)';

export function PitchView({ home, away, events }: PitchViewProps) {
  const getMarkers = useMemo(() => buildPlayerMarkers(events), [events]);

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
          <TeamLayer team={home} side="home" getMarkers={getMarkers} />
        ) : null}
        {away?.starters?.length ? (
          <TeamLayer team={away} side="away" getMarkers={getMarkers} />
        ) : null}
      </View>
    </View>
  );
}

function TeamLayer({
  team,
  side,
  getMarkers,
}: {
  team: FixtureTeamLineup;
  side: 'home' | 'away';
  getMarkers: (p: FixtureLineupPlayer) => PlayerMarkers;
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

  // Inset the player tokens close to the edges so wide players sit near
  // the touchline. Y inset is a touch larger so GKs aren't glued to the
  // back-line. The HALF_RANGE leaves a 4% buffer below the halfway line so
  // strikers stop short of crossing into the opposite half. Name captions
  // flip side per team (home below jersey, away above) so they always
  // point toward the centre and never run off.
  const X_INSET = 0.02;
  const Y_INSET = 0.05;
  const STRIKER_BUFFER = 0.06;
  const X_RANGE = 1 - 2 * X_INSET;
  const HALF_RANGE = 0.5 - Y_INSET - STRIKER_BUFFER;

  return (
    <>
      {positioned.map(({ player, row, col }) => {
        const colCount = colsByRow.get(row) ?? 1;
        // Distribute evenly: each player sits in the middle of its slot,
        // so a 2-player row lands at 25% / 75% (centered) instead of 0% / 100%.
        const xRatio = (col - 0.5) / colCount;
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
            side={side}
            markers={getMarkers(player)}
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
  side,
  markers,
}: {
  player: FixtureLineupPlayer;
  leftPercent: number;
  topPercent: number;
  side: 'home' | 'away';
  markers: PlayerMarkers;
}) {
  const c = useTheme();
  const isAway = side === 'away';

  const jersey = (
    <View style={styles.jerseyWrap}>
      <View style={[styles.jerseyCircle, { backgroundColor: c.bg, borderColor: c.brand }]}>
        <ThemedText style={[styles.jerseyNumber, { color: c.text }]}>
          {player.jersey_number ?? '–'}
        </ThemedText>
      </View>
      {markers.goals > 0 || markers.assists > 0 ? (
        <View style={styles.badgeStack}>
          {markers.goals > 0 ? (
            <View style={[styles.badge, { backgroundColor: c.text }]}>
              <MaterialCommunityIcons name="soccer" size={10} color={c.bg} />
              {markers.goals > 1 ? (
                <ThemedText style={[styles.badgeText, { color: c.bg }]}>
                  {markers.goals}
                </ThemedText>
              ) : null}
            </View>
          ) : null}
          {markers.assists > 0 ? (
            <View style={[styles.badge, { backgroundColor: c.brand }]}>
              <MaterialCommunityIcons name="shoe-cleat" size={10} color={c.textInverse} />
              {markers.assists > 1 ? (
                <ThemedText style={[styles.badgeText, { color: c.textInverse }]}>
                  {markers.assists}
                </ThemedText>
              ) : null}
            </View>
          ) : null}
        </View>
      ) : null}
    </View>
  );

  const name = (
    <View style={styles.nameWrap}>
      <ThemedText style={styles.nameText} numberOfLines={1}>
        {shortName(player.player_name)}
      </ThemedText>
    </View>
  );

  return (
    <View
      style={[
        styles.token,
        {
          left: `${leftPercent}%`,
          top: `${topPercent}%`,
          marginTop: isAway ? -31 : -15,
        },
      ]}>
      {isAway ? name : jersey}
      {isAway ? jersey : name}
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
    alignItems: 'center',
  },
  jerseyWrap: {
    position: 'relative',
    width: 30,
    height: 30,
  },
  jerseyCircle: {
    width: 30,
    height: 30,
    borderRadius: 15,
    borderWidth: 2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  badgeStack: {
    position: 'absolute',
    top: -6,
    right: -10,
    flexDirection: 'row',
    gap: 2,
  },
  badge: {
    minWidth: 14,
    height: 14,
    paddingHorizontal: 3,
    borderRadius: 7,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 1,
  },
  badgeText: {
    fontSize: 9,
    lineHeight: 11,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  jerseyNumber: {
    fontSize: 12,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  nameWrap: {
    marginVertical: 2,
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
