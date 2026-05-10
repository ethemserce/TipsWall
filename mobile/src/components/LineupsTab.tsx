import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Image } from 'expo-image';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { PitchView } from '@/src/components/PitchView';
import { TabEmpty, TabError, TabLoading } from '@/src/components/TabFeedback';
import { useTheme } from '@/src/lib/useTheme';
import type {
  FixtureEvent,
  FixtureLineupPlayer,
  FixtureLineups,
  FixtureTeamLineup,
} from '@/src/types/fixtureDetailExtras';

interface LineupsTabProps {
  loading: boolean;
  error?: unknown;
  lineups: FixtureLineups | null;
  events?: FixtureEvent[];
  homeName?: string | null;
  awayName?: string | null;
  homeImagePath?: string | null;
  awayImagePath?: string | null;
}

type Side = 'home' | 'away';

export function LineupsTab({
  loading,
  error,
  lineups,
  events,
  homeName,
  awayName,
  homeImagePath,
  awayImagePath,
}: LineupsTabProps) {
  const { t } = useTranslation();
  const [side, setSide] = useState<Side>('home');

  if (error && !lineups) return <TabError error={error} />;
  if (loading && !lineups) return <TabLoading />;
  if (!lineups || (!lineups.home && !lineups.away))
    return <TabEmpty icon="account-group-outline" message={t('fixture.lineups.notAvailable')} />;

  const hasHome = lineups.home != null;
  const hasAway = lineups.away != null;
  const effective: Side =
    side === 'home' && !hasHome
      ? 'away'
      : side === 'away' && !hasAway
        ? 'home'
        : side;
  const activeTeam =
    effective === 'home' ? lineups.home ?? null : lineups.away ?? null;

  return (
    <>
      <FormationSummary home={lineups.home} away={lineups.away} />

      <PitchView home={lineups.home} away={lineups.away} events={events} />

      <SideToggle
        side={effective}
        onSelect={setSide}
        homeImage={homeImagePath ?? null}
        awayImage={awayImagePath ?? null}
        homeName={homeName ?? t('fixture.lineups.home')}
        awayName={awayName ?? t('fixture.lineups.away')}
        homeEnabled={hasHome}
        awayEnabled={hasAway}
      />

      {activeTeam && activeTeam.bench.length > 0 ? (
        <BenchCard team={activeTeam} events={events} />
      ) : null}
    </>
  );
}

function FormationSummary({
  home,
  away,
}: {
  home: FixtureTeamLineup | null | undefined;
  away: FixtureTeamLineup | null | undefined;
}) {
  const c = useTheme();
  if (!home?.formation && !away?.formation) return null;
  return (
    <View style={[styles.formationRow, { backgroundColor: c.surface, borderColor: c.border }]}>
      <FormationCell label="Home" formation={home?.formation ?? null} />
      <View style={[styles.formationDivider, { backgroundColor: c.border }]} />
      <FormationCell label="Away" formation={away?.formation ?? null} />
    </View>
  );
}

function FormationCell({
  label,
  formation,
}: {
  label: string;
  formation: string | null;
}) {
  const c = useTheme();
  return (
    <View style={styles.formationCell}>
      <ThemedText style={[styles.formationLabel, { color: c.textMuted }]}>
        {label.toUpperCase()}
      </ThemedText>
      <ThemedText style={[styles.formationValue, { color: c.text }]}>
        {formation ?? '—'}
      </ThemedText>
    </View>
  );
}

function SideToggle({
  side,
  onSelect,
  homeImage,
  awayImage,
  homeName,
  awayName,
  homeEnabled,
  awayEnabled,
}: {
  side: Side;
  onSelect: (s: Side) => void;
  homeImage: string | null;
  awayImage: string | null;
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
        image={homeImage}
        label={homeName}
        onPress={() => homeEnabled && onSelect('home')}
      />
      <ToggleButton
        active={side === 'away'}
        disabled={!awayEnabled}
        image={awayImage}
        label={awayName}
        onPress={() => awayEnabled && onSelect('away')}
      />
    </View>
  );
}

function ToggleButton({
  active,
  disabled,
  image,
  label,
  onPress,
}: {
  active: boolean;
  disabled: boolean;
  image: string | null;
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
        active && { backgroundColor: c.bg },
      ]}>
      {image ? (
        <Image source={{ uri: image }} style={styles.toggleLogo} contentFit="contain" />
      ) : (
        <View style={[styles.toggleLogo, { backgroundColor: c.border, borderRadius: 12 }]} />
      )}
      <ThemedText
        numberOfLines={1}
        style={[
          styles.toggleText,
          { color: active ? c.text : disabled ? c.border : c.textMuted },
        ]}>
        {label}
      </ThemedText>
    </Pressable>
  );
}

interface SubInInfo {
  minute: number | null;
  extraMinute: number | null;
  replaced: string | null;
  injured: boolean;
}

function BenchCard({
  team,
  events,
}: {
  team: FixtureTeamLineup;
  events: FixtureEvent[] | undefined;
}) {
  const c = useTheme();
  const { t } = useTranslation();
  // Index substitution events by the player who came ON so each bench row
  // can find its own entry in O(1). SportMonks: player_name = on, related = off.
  const subInByPlayer = new Map<string, SubInInfo>();
  for (const e of events ?? []) {
    if ((e.type_code ?? '').toUpperCase() !== 'SUBSTITUTION') continue;
    const key = subKey(e.player_id, e.player_name);
    if (!key) continue;
    subInByPlayer.set(key, {
      minute: e.minute ?? null,
      extraMinute: e.extra_minute ?? null,
      replaced: e.related_player_name ?? null,
      injured: e.injured === true,
    });
  }

  // Bench order: players who came on first (sorted by minute), then the
  // unused subs grouped by position GK → DEF → MID → FWD.
  const sorted = [...team.bench].sort((a, b) => {
    const sa = subInByPlayer.get(subKey(a.player_id, a.player_name) ?? '');
    const sb = subInByPlayer.get(subKey(b.player_id, b.player_name) ?? '');
    if (sa && !sb) return -1;
    if (sb && !sa) return 1;
    if (sa && sb) {
      const ma = sa.minute ?? Number.MAX_SAFE_INTEGER;
      const mb = sb.minute ?? Number.MAX_SAFE_INTEGER;
      if (ma !== mb) return ma - mb;
      return (sa.extraMinute ?? 0) - (sb.extraMinute ?? 0);
    }
    const pa = positionRank(a.position_code);
    const pb = positionRank(b.position_code);
    if (pa !== pb) return pa - pb;
    return (a.jersey_number ?? 999) - (b.jersey_number ?? 999);
  });

  return (
    <View style={[styles.card, { backgroundColor: c.surface, borderColor: c.border }]}>
      <ThemedText style={[styles.cardTitle, { color: c.textMuted }]}>
        {t('fixture.lineups.bench').toUpperCase()}
      </ThemedText>
      {sorted.map((p, i) => (
        <BenchRow
          key={`${p.player_id ?? p.player_name ?? i}-${i}`}
          player={p}
          subIn={subInByPlayer.get(subKey(p.player_id, p.player_name) ?? '') ?? null}
        />
      ))}
    </View>
  );
}

function positionRank(code: string | null | undefined): number {
  switch ((code ?? '').toUpperCase()) {
    case 'GOALKEEPER':
      return 0;
    case 'DEFENDER':
      return 1;
    case 'MIDFIELDER':
      return 2;
    case 'ATTACKER':
      return 3;
    default:
      return 4;
  }
}

function subKey(id: number | null | undefined, name: string | null | undefined) {
  if (id != null) return `id:${id}`;
  if (name) return `name:${name.trim().toLowerCase()}`;
  return null;
}

function BenchRow({
  player,
  subIn,
}: {
  player: FixtureLineupPlayer;
  subIn: SubInInfo | null;
}) {
  const c = useTheme();
  const { t } = useTranslation();
  const minuteLabel = subIn?.minute != null
    ? subIn.extraMinute && subIn.extraMinute > 0
      ? `${subIn.minute}+${subIn.extraMinute}'`
      : `${subIn.minute}'`
    : null;
  const positionCode = (player.position_code ?? '').toUpperCase();
  // Translate the SportMonks position enum so the bench row reads in the
  // user's language. Unknown codes fall back to the raw upper-case string
  // (better than a broken key like `fixture.lineups.positions.MISC`).
  const positionLabel = positionCode
    ? t(`fixture.lineups.positions.${positionCode}`, {
        defaultValue: positionCode,
      })
    : '';
  return (
    <View style={[styles.benchRow, { borderTopColor: c.border }]}>
      <View style={[styles.benchJersey, { backgroundColor: c.bg, borderColor: c.border }]}>
        <ThemedText style={[styles.benchJerseyText, { color: c.text }]}>
          {player.jersey_number ?? '–'}
        </ThemedText>
      </View>
      <View style={styles.benchNameBlock}>
        <ThemedText
          style={[styles.benchName, { color: c.text }]}
          numberOfLines={1}>
          {player.player_name ?? '—'}
        </ThemedText>
        {subIn ? (
          <View style={styles.benchSubRow}>
            <View style={styles.swapIcon}>
              <MaterialCommunityIcons
                name="arrow-up-bold"
                size={10}
                color={subIn.injured ? '#3b82f6' : '#22c55e'}
              />
              <MaterialCommunityIcons
                name="arrow-down-bold"
                size={10}
                color="#ef4444"
                style={styles.swapDown}
              />
            </View>
            <ThemedText
              style={[styles.benchSubInfo, { color: c.textMuted }]}
              numberOfLines={1}>
              {minuteLabel ? `${minuteLabel}` : ''}
              {subIn.replaced ? `${minuteLabel ? ' · ' : ''}${subIn.replaced}` : ''}
            </ThemedText>
          </View>
        ) : null}
      </View>
      <ThemedText
        style={[styles.benchPosition, { color: c.textMuted }]}
        numberOfLines={1}>
        {positionLabel}
      </ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  formationRow: {
    marginHorizontal: 16,
    marginTop: 16,
    paddingVertical: 8,
    flexDirection: 'row',
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
  },
  formationCell: {
    flex: 1,
    alignItems: 'center',
    gap: 2,
  },
  formationLabel: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
  formationValue: {
    fontSize: 14,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  formationDivider: {
    width: 1,
    height: '100%',
  },
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
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 999,
  },
  toggleLogo: {
    width: 22,
    height: 22,
  },
  toggleText: {
    fontSize: 13,
    fontWeight: '600',
    flexShrink: 1,
  },
  card: {
    marginHorizontal: 16,
    marginTop: 12,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    overflow: 'hidden',
  },
  cardTitle: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.5,
    paddingHorizontal: 14,
    paddingTop: 12,
    paddingBottom: 6,
  },
  benchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 12,
  },
  benchJersey: {
    width: 30,
    height: 30,
    borderRadius: 15,
    borderWidth: StyleSheet.hairlineWidth,
    alignItems: 'center',
    justifyContent: 'center',
  },
  benchJerseyText: {
    fontSize: 12,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  benchNameBlock: {
    flex: 1,
  },
  benchName: {
    fontSize: 14,
  },
  benchSubRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    marginTop: 2,
  },
  benchSubInfo: {
    fontSize: 11,
    fontWeight: '500',
    fontVariant: ['tabular-nums'],
    flexShrink: 1,
  },
  swapIcon: {
    width: 16,
    height: 12,
    flexDirection: 'row',
    alignItems: 'center',
  },
  swapDown: {
    marginLeft: -4,
  },
  benchPosition: {
    fontSize: 11,
    fontWeight: '600',
  },
});
