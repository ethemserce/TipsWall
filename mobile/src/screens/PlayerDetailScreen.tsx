import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Image } from 'expo-image';
import { router } from 'expo-router';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { PlayerStatsCard } from '@/src/components/PlayerStatsCard';
import { TabEmpty } from '@/src/components/TabFeedback';
import { usePlayer, usePlayerSeasonStats } from '@/src/hooks/usePlayer';
import { useTheme } from '@/src/lib/useTheme';

interface PlayerDetailScreenProps {
  playerId: number;
}

export function PlayerDetailScreen({ playerId }: PlayerDetailScreenProps) {
  const c = useTheme();
  const { t } = useTranslation();

  const playerQuery = usePlayer(playerId);
  const statsQuery = usePlayerSeasonStats(playerId);
  const player = playerQuery.data;
  const stats = statsQuery.data ?? [];

  const age = ageFromDob(player?.date_of_birth);
  const positionLabel = player?.position_code
    ? t(`fixture.lineups.positions.${player.position_code}`, {
        defaultValue: player.position_code,
      })
    : null;

  const handleBack = () => {
    if (router.canGoBack()) router.back();
    else router.replace('/');
  };

  const handleTeamPress = () => {
    if (player?.current_team_id) {
      router.push(`/team/${player.current_team_id}` as never);
    }
  };

  return (
    <SafeAreaView
      style={[styles.flex, { backgroundColor: c.bg }]}
      edges={['top']}>
      <View style={[styles.headerBar, { borderBottomColor: c.border }]}>
        <Pressable
          onPress={handleBack}
          hitSlop={12}
          style={styles.headerBack}>
          <MaterialCommunityIcons
            name="chevron-left"
            size={24}
            color={c.text}
          />
        </Pressable>
        <ThemedText
          style={[styles.headerTitle, { color: c.text }]}
          numberOfLines={1}>
          {player?.display_name ?? player?.name ?? `Player #${playerId}`}
        </ThemedText>
        <View style={styles.headerBack} />
      </View>

      {playerQuery.isLoading && !player ? (
        <View style={styles.center}>
          <ActivityIndicator color={c.brand} />
        </View>
      ) : !player ? (
        <View style={styles.center}>
          <ThemedText style={[styles.notFound, { color: c.text }]}>
            {t('player.notFound', { defaultValue: 'Oyuncu bulunamadı.' })}
          </ThemedText>
        </View>
      ) : (
        <ScrollView
          contentContainerStyle={styles.scrollContent}
          refreshControl={
            <RefreshControl
              refreshing={playerQuery.isFetching || statsQuery.isFetching}
              onRefresh={() => {
                playerQuery.refetch();
                statsQuery.refetch();
              }}
              tintColor={c.brand}
            />
          }>
          {/* Hero block: avatar + name + position chip + current team */}
          <View
            style={[
              styles.hero,
              { backgroundColor: c.surface, borderColor: c.border },
            ]}>
            {player.image_path ? (
              <Image
                source={{ uri: player.image_path }}
                style={styles.avatar}
                contentFit="cover"
              />
            ) : (
              <View
                style={[
                  styles.avatar,
                  styles.avatarFallback,
                  { backgroundColor: c.bg, borderColor: c.border },
                ]}>
                <MaterialCommunityIcons
                  name="account"
                  size={42}
                  color={c.textMuted}
                />
              </View>
            )}

            <ThemedText
              style={[styles.heroName, { color: c.text }]}
              numberOfLines={2}>
              {player.display_name ?? player.name}
            </ThemedText>

            <View style={styles.heroChips}>
              {positionLabel ? (
                <Chip
                  icon="account-tie"
                  label={positionLabel}
                  c={c}
                  emphasised
                />
              ) : null}
              {player.current_jersey_number != null ? (
                <Chip
                  icon="tshirt-crew-outline"
                  label={`#${player.current_jersey_number}`}
                  c={c}
                />
              ) : null}
              {player.current_captain ? (
                <Chip icon="star" label="Kaptan" c={c} />
              ) : null}
            </View>

            {player.current_team_id ? (
              <Pressable
                onPress={handleTeamPress}
                style={({ pressed }) => [
                  styles.teamRow,
                  { borderColor: c.border, backgroundColor: c.bg },
                  pressed && { opacity: 0.7 },
                ]}>
                {player.current_team_image_path ? (
                  <Image
                    source={{ uri: player.current_team_image_path }}
                    style={styles.teamLogo}
                    contentFit="contain"
                  />
                ) : null}
                <View style={styles.teamTextBlock}>
                  <ThemedText
                    style={[styles.teamLabel, { color: c.textMuted }]}>
                    {t('player.currentTeam', { defaultValue: 'MEVCUT TAKIM' })}
                  </ThemedText>
                  <ThemedText
                    style={[styles.teamName, { color: c.text }]}
                    numberOfLines={1}>
                    {player.current_team_name ?? '—'}
                  </ThemedText>
                </View>
                <MaterialCommunityIcons
                  name="chevron-right"
                  size={20}
                  color={c.textMuted}
                />
              </Pressable>
            ) : null}
          </View>

          {/* Bio: age + height + weight + dob */}
          <View
            style={[
              styles.bioCard,
              { backgroundColor: c.surface, borderColor: c.border },
            ]}>
            <ThemedText style={[styles.bioTitle, { color: c.textMuted }]}>
              {t('player.bio.title', { defaultValue: 'BİLGİLER' })}
            </ThemedText>
            <View style={styles.bioRow}>
              <BioCell
                value={age != null ? `${age}` : '—'}
                label={t('player.bio.age', { defaultValue: 'Yaş' })}
                c={c}
              />
              <BioCell
                value={player.height != null ? `${player.height} cm` : '—'}
                label={t('player.bio.height', { defaultValue: 'Boy' })}
                c={c}
              />
              <BioCell
                value={player.weight != null ? `${player.weight} kg` : '—'}
                label={t('player.bio.weight', { defaultValue: 'Kilo' })}
                c={c}
              />
            </View>
          </View>

          {/* Stats per (league × season × team) — latest first */}
          {statsQuery.isLoading && stats.length === 0 ? (
            <View style={styles.center}>
              <ActivityIndicator color={c.brand} />
            </View>
          ) : stats.length === 0 ? (
            <TabEmpty
              icon="chart-bar"
              message={t('player.stats.empty', {
                defaultValue: 'Bu oyuncu için sezon istatistiği henüz hesaplanmadı.',
              })}
            />
          ) : (
            stats.map((s) => (
              <PlayerStatsCard
                key={`${s.league_id}-${s.season_id}-${s.team_id}`}
                stats={s}
              />
            ))
          )}
        </ScrollView>
      )}
    </SafeAreaView>
  );
}

function Chip({
  icon,
  label,
  c,
  emphasised,
}: {
  icon: keyof typeof MaterialCommunityIcons.glyphMap;
  label: string;
  c: ReturnType<typeof useTheme>;
  emphasised?: boolean;
}) {
  return (
    <View
      style={[
        styles.chip,
        {
          backgroundColor: emphasised ? c.brandSoft : c.bg,
          borderColor: emphasised ? c.brand : c.border,
        },
      ]}>
      <MaterialCommunityIcons
        name={icon}
        size={12}
        color={emphasised ? c.brand : c.textMuted}
      />
      <ThemedText
        style={[
          styles.chipLabel,
          { color: emphasised ? c.brand : c.text },
        ]}>
        {label}
      </ThemedText>
    </View>
  );
}

function BioCell({
  value,
  label,
  c,
}: {
  value: string;
  label: string;
  c: ReturnType<typeof useTheme>;
}) {
  return (
    <View style={styles.bioCell}>
      <ThemedText style={[styles.bioValue, { color: c.text }]}>{value}</ThemedText>
      <ThemedText style={[styles.bioLabel, { color: c.textMuted }]}>{label}</ThemedText>
    </View>
  );
}

function ageFromDob(dob: string | null | undefined): number | null {
  if (!dob) return null;
  const ts = Date.parse(dob);
  if (Number.isNaN(ts)) return null;
  const years = Math.floor((Date.now() - ts) / (365.25 * 24 * 60 * 60 * 1000));
  if (years < 10 || years > 60) return null;
  return years;
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  center: {
    flex: 1,
    minHeight: 200,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  scrollContent: {
    paddingBottom: 96,
    paddingTop: 12,
  },
  headerBar: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  headerBack: {
    width: 36,
    height: 36,
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerTitle: {
    flex: 1,
    fontSize: 15,
    fontWeight: '700',
    textAlign: 'center',
  },
  notFound: {
    fontSize: 15,
    fontWeight: '600',
  },
  hero: {
    marginHorizontal: 16,
    marginTop: 4,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 14,
    paddingHorizontal: 18,
    paddingVertical: 18,
    alignItems: 'center',
    gap: 10,
  },
  avatar: {
    width: 96,
    height: 96,
    borderRadius: 48,
  },
  avatarFallback: {
    borderWidth: StyleSheet.hairlineWidth,
    alignItems: 'center',
    justifyContent: 'center',
  },
  heroName: {
    fontSize: 19,
    fontWeight: '800',
    textAlign: 'center',
  },
  heroChips: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'center',
    gap: 6,
  },
  chip: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
  },
  chipLabel: {
    fontSize: 11,
    fontWeight: '700',
  },
  teamRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
    width: '100%',
    marginTop: 6,
  },
  teamLogo: {
    width: 28,
    height: 28,
  },
  teamTextBlock: {
    flex: 1,
    gap: 1,
  },
  teamLabel: {
    fontSize: 9,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
  teamName: {
    fontSize: 14,
    fontWeight: '700',
  },
  bioCard: {
    marginHorizontal: 16,
    marginTop: 14,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    paddingHorizontal: 14,
    paddingVertical: 12,
    gap: 10,
  },
  bioTitle: {
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
  bioRow: {
    flexDirection: 'row',
  },
  bioCell: {
    flex: 1,
    alignItems: 'center',
    gap: 2,
  },
  bioValue: {
    fontSize: 17,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  bioLabel: {
    fontSize: 11,
  },
});
