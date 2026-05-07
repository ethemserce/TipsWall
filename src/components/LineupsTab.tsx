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
    return <TabEmpty message={t('fixture.lineups.notAvailable')} />;

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
        <BenchCard team={activeTeam} />
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

function BenchCard({ team }: { team: FixtureTeamLineup }) {
  const c = useTheme();
  const { t } = useTranslation();
  return (
    <View style={[styles.card, { backgroundColor: c.surface, borderColor: c.border }]}>
      <ThemedText style={[styles.cardTitle, { color: c.textMuted }]}>
        {t('fixture.lineups.bench').toUpperCase()}
      </ThemedText>
      {team.bench.map((p, i) => (
        <BenchRow
          key={`${p.player_id ?? p.player_name ?? i}-${i}`}
          player={p}
        />
      ))}
    </View>
  );
}

function BenchRow({ player }: { player: FixtureLineupPlayer }) {
  const c = useTheme();
  return (
    <View style={[styles.benchRow, { borderTopColor: c.border }]}>
      <View style={[styles.benchJersey, { backgroundColor: c.bg, borderColor: c.border }]}>
        <ThemedText style={[styles.benchJerseyText, { color: c.text }]}>
          {player.jersey_number ?? '–'}
        </ThemedText>
      </View>
      <ThemedText
        style={[styles.benchName, { color: c.text }]}
        numberOfLines={1}>
        {player.player_name ?? '—'}
      </ThemedText>
      <ThemedText
        style={[styles.benchPosition, { color: c.textMuted }]}
        numberOfLines={1}>
        {player.position_code ?? ''}
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
  benchName: {
    flex: 1,
    fontSize: 14,
  },
  benchPosition: {
    fontSize: 11,
    fontWeight: '600',
  },
});
