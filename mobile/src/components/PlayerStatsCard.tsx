import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Image } from 'expo-image';
import { useTranslation } from 'react-i18next';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { PlayerSeasonStats } from '@/src/types/player';

interface PlayerStatsCardProps {
  stats: PlayerSeasonStats;
}

/**
 * Per-season stat block for a player. Compact tile layout that fits
 * inside both the PlayerDetailScreen (full width) and the
 * PlayerPeekSheet bottom-sheet (limited height — section is scrollable
 * upstream so we don't worry about clipping).
 */
export function PlayerStatsCard({ stats }: PlayerStatsCardProps) {
  const c = useTheme();
  const { t } = useTranslation();

  const played = stats.matches_played ?? 0;
  const started = stats.matches_started ?? 0;
  const minutes = stats.minutes_played ?? 0;
  const goals = stats.goals ?? 0;
  const assists = stats.assists ?? 0;
  const yellow = stats.yellow_cards ?? 0;
  const red = stats.red_cards ?? 0;

  return (
    <View style={[styles.card, { backgroundColor: c.surface, borderColor: c.border }]}>
      <View style={styles.header}>
        <View style={styles.headerLeft}>
          {stats.team_image_path ? (
            <Image
              source={{ uri: stats.team_image_path }}
              style={styles.teamLogo}
              contentFit="contain"
            />
          ) : null}
          <View style={styles.headerText}>
            <ThemedText
              style={[styles.teamName, { color: c.text }]}
              numberOfLines={1}>
              {stats.team_name ?? `Team #${stats.team_id}`}
            </ThemedText>
            <ThemedText
              style={[styles.leagueLine, { color: c.textMuted }]}
              numberOfLines={1}>
              {stats.league_name ?? `League #${stats.league_id}`}
              {stats.season_name ? ` · ${stats.season_name}` : ''}
            </ThemedText>
          </View>
        </View>
      </View>

      <View style={styles.tileRow}>
        <Tile
          value={played}
          label={t('player.stats.matches', { defaultValue: 'MAÇ' })}
          c={c}
        />
        <Tile
          value={started}
          label={t('player.stats.started', { defaultValue: 'İLK 11' })}
          c={c}
        />
        <Tile
          value={minutes}
          label={t('player.stats.minutes', { defaultValue: 'DK' })}
          c={c}
        />
      </View>

      <View style={[styles.divider, { backgroundColor: c.borderSoft }]} />

      <View style={styles.tileRow}>
        <Tile
          value={goals}
          label={t('player.stats.goals', { defaultValue: 'GOL' })}
          c={c}
          color={goals > 0 ? c.success : undefined}
          icon="soccer"
        />
        <Tile
          value={assists}
          label={t('player.stats.assists', { defaultValue: 'ASİST' })}
          c={c}
          color={assists > 0 ? c.brand : undefined}
          icon="shoe-cleat"
        />
        <Tile
          value={`${yellow}/${red}`}
          label={t('player.stats.cards', { defaultValue: 'SK/KK' })}
          c={c}
          color={red > 0 ? c.danger : yellow > 0 ? '#f59e0b' : undefined}
          icon="card-text-outline"
        />
      </View>

      {stats.penalties_scored != null && stats.penalties_scored > 0 ? (
        <>
          <View style={[styles.divider, { backgroundColor: c.borderSoft }]} />
          <View style={styles.pkRow}>
            <MaterialCommunityIcons name="bullseye-arrow" size={14} color={c.brand} />
            <ThemedText style={[styles.pkText, { color: c.textMuted }]}>
              {t('player.stats.penaltiesLine', {
                defaultValue: 'Penaltı: {{scored}} gol, {{missed}} kaçırma',
                scored: stats.penalties_scored,
                missed: stats.penalties_missed ?? 0,
              })}
            </ThemedText>
          </View>
        </>
      ) : null}
    </View>
  );
}

function Tile({
  value,
  label,
  c,
  color,
  icon,
}: {
  value: number | string;
  label: string;
  c: ReturnType<typeof useTheme>;
  color?: string;
  icon?: keyof typeof MaterialCommunityIcons.glyphMap;
}) {
  return (
    <View style={styles.tile}>
      {icon ? (
        <MaterialCommunityIcons
          name={icon}
          size={14}
          color={color ?? c.textMuted}
          style={styles.tileIcon}
        />
      ) : null}
      <ThemedText style={[styles.tileValue, { color: color ?? c.text }]}>
        {value}
      </ThemedText>
      <ThemedText style={[styles.tileLabel, { color: c.textMuted }]}>
        {label}
      </ThemedText>
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
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  headerLeft: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
  },
  teamLogo: {
    width: 26,
    height: 26,
  },
  headerText: {
    flex: 1,
    gap: 1,
  },
  teamName: {
    fontSize: 14,
    fontWeight: '700',
  },
  leagueLine: {
    fontSize: 11,
  },
  tileRow: {
    flexDirection: 'row',
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  tile: {
    flex: 1,
    alignItems: 'center',
    gap: 2,
  },
  tileIcon: {
    marginBottom: 2,
  },
  tileValue: {
    fontSize: 22,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  tileLabel: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
  divider: {
    height: StyleSheet.hairlineWidth,
    marginHorizontal: 14,
  },
  pkRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 14,
    paddingVertical: 10,
  },
  pkText: {
    fontSize: 11,
  },
});
