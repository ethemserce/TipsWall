import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Image } from 'expo-image';
import { router } from 'expo-router';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { PlayerStatsCard } from '@/src/components/PlayerStatsCard';
import { usePlayer, usePlayerSeasonStats } from '@/src/hooks/usePlayer';
import { useTheme } from '@/src/lib/useTheme';

interface PlayerPeekSheetProps {
  visible: boolean;
  playerId: number | null;
  onClose: () => void;
  // Match-level numbers shipped by SportMonks lineup_details — shown
  // at the top of the sheet as "Bu maçta" before the season totals.
  // All fields optional; the card renders only what's available.
  matchSnapshot?: {
    rating?: number | null;
    goals?: number | null;
    assists?: number | null;
    yellowCards?: number | null;
    redCards?: number | null;
    minutesPlayed?: number | null;
  } | null;
}

/**
 * Bottom-sheet preview that opens when a lineup row is tapped. Same
 * visual rhythm as AnalysisFiltersSheet — slide-up panel, backdrop
 * dismisses, "go to detail" button routes to the full player screen.
 */
export function PlayerPeekSheet({
  visible,
  playerId,
  onClose,
  matchSnapshot,
}: PlayerPeekSheetProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();

  const playerQuery = usePlayer(visible ? playerId : null);
  const statsQuery = usePlayerSeasonStats(visible ? playerId : null);
  const player = playerQuery.data;
  const seasonStats = statsQuery.data ?? [];

  const positionLabel = player?.position_code
    ? t(`fixture.lineups.positions.${player.position_code}`, {
        defaultValue: player.position_code,
      })
    : null;

  const handleNavigateToDetail = () => {
    if (playerId == null) return;
    onClose();
    // Defer the route push by a tick so the modal can finish its
    // close animation — pushing inside the same frame stacks the
    // route under the modal and the back button leaves the user
    // staring at an empty sheet.
    setTimeout(() => {
      router.push(`/player/${playerId}` as never);
    }, 80);
  };

  return (
    <Modal
      visible={visible}
      transparent
      animationType="slide"
      onRequestClose={onClose}>
      <Pressable style={styles.backdrop} onPress={onClose}>
        <Pressable
          // Inner stops backdrop dismiss when the user taps inside.
          onPress={(e) => e.stopPropagation()}
          style={[
            styles.sheet,
            {
              backgroundColor: c.surface,
              borderColor: c.border,
              paddingBottom: Math.max(12, insets.bottom + 8),
            },
          ]}>
          <View style={styles.handle}>
            <View style={[styles.handleBar, { backgroundColor: c.border }]} />
          </View>

          {playerQuery.isLoading && !player ? (
            <View style={styles.loading}>
              <ActivityIndicator color={c.brand} />
            </View>
          ) : !player ? (
            <View style={styles.loading}>
              <ThemedText style={[styles.errorText, { color: c.textMuted }]}>
                {t('player.notFound', { defaultValue: 'Oyuncu bulunamadı.' })}
              </ThemedText>
            </View>
          ) : (
            <ScrollView contentContainerStyle={styles.body}>
              <View style={styles.header}>
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
                      size={28}
                      color={c.textMuted}
                    />
                  </View>
                )}
                <View style={styles.headerInfo}>
                  <ThemedText
                    style={[styles.name, { color: c.text }]}
                    numberOfLines={2}>
                    {player.display_name ?? player.name}
                  </ThemedText>
                  <View style={styles.chipsRow}>
                    {positionLabel ? (
                      <View style={[styles.chip, { backgroundColor: c.brandSoft, borderColor: c.brand }]}>
                        <ThemedText style={[styles.chipText, { color: c.brand }]}>
                          {positionLabel}
                        </ThemedText>
                      </View>
                    ) : null}
                    {player.current_jersey_number != null ? (
                      <View style={[styles.chip, { backgroundColor: c.bg, borderColor: c.border }]}>
                        <ThemedText style={[styles.chipText, { color: c.text }]}>
                          #{player.current_jersey_number}
                        </ThemedText>
                      </View>
                    ) : null}
                  </View>
                </View>
              </View>

              {/* Match-level snapshot from lineup_details, if the
                  caller provided one. Sits above the season totals so
                  the user gets "what did this player do in *this*
                  match" at a glance. */}
              {matchSnapshot &&
              ((matchSnapshot.goals ?? 0) > 0 ||
                (matchSnapshot.assists ?? 0) > 0 ||
                (matchSnapshot.yellowCards ?? 0) > 0 ||
                (matchSnapshot.redCards ?? 0) > 0 ||
                (matchSnapshot.minutesPlayed ?? 0) > 0 ||
                matchSnapshot.rating != null) ? (
                <View
                  style={[
                    styles.matchCard,
                    { backgroundColor: c.surfaceElevated, borderColor: c.brand },
                  ]}>
                  <ThemedText style={[styles.matchTitle, { color: c.brand }]}>
                    {t('player.peek.thisMatch', { defaultValue: 'BU MAÇTA' })}
                  </ThemedText>
                  <View style={styles.matchRow}>
                    {matchSnapshot.rating != null ? (
                      <MatchTile label="RATING" value={matchSnapshot.rating.toFixed(1)} c={c} />
                    ) : null}
                    <MatchTile label="DK" value={`${matchSnapshot.minutesPlayed ?? 0}`} c={c} />
                    <MatchTile label="GOL" value={`${matchSnapshot.goals ?? 0}`} c={c} />
                    <MatchTile label="ASİST" value={`${matchSnapshot.assists ?? 0}`} c={c} />
                    <MatchTile
                      label="SK/KK"
                      value={`${matchSnapshot.yellowCards ?? 0}/${matchSnapshot.redCards ?? 0}`}
                      c={c}
                    />
                  </View>
                </View>
              ) : null}

              {/* Latest season stats — first row only inside the
                  sheet to keep the height manageable; the full
                  multi-season list lives on PlayerDetailScreen. */}
              {seasonStats.length > 0 ? (
                <PlayerStatsCard stats={seasonStats[0]} />
              ) : statsQuery.isLoading ? (
                <View style={styles.loading}>
                  <ActivityIndicator color={c.brand} />
                </View>
              ) : (
                <ThemedText style={[styles.emptyHint, { color: c.textMuted }]}>
                  {t('player.peek.noStats', {
                    defaultValue: 'Bu sezon için istatistik yok.',
                  })}
                </ThemedText>
              )}

              <Pressable
                onPress={handleNavigateToDetail}
                style={({ pressed }) => [
                  styles.detailBtn,
                  {
                    backgroundColor: c.brand,
                    opacity: pressed ? 0.8 : 1,
                  },
                ]}>
                <ThemedText style={[styles.detailBtnText, { color: c.textInverse }]}>
                  {t('player.peek.openDetail', { defaultValue: 'Detaya git' })}
                </ThemedText>
                <MaterialCommunityIcons name="arrow-right" size={18} color={c.textInverse} />
              </Pressable>
            </ScrollView>
          )}
        </Pressable>
      </Pressable>
    </Modal>
  );
}

function MatchTile({
  label,
  value,
  c,
}: {
  label: string;
  value: string;
  c: ReturnType<typeof useTheme>;
}) {
  return (
    <View style={styles.matchTile}>
      <ThemedText style={[styles.matchTileValue, { color: c.text }]}>
        {value}
      </ThemedText>
      <ThemedText style={[styles.matchTileLabel, { color: c.textMuted }]}>
        {label}
      </ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.55)',
    justifyContent: 'flex-end',
  },
  sheet: {
    maxHeight: '85%',
    borderTopLeftRadius: 16,
    borderTopRightRadius: 16,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  handle: {
    alignItems: 'center',
    paddingTop: 6,
    paddingBottom: 4,
  },
  handleBar: {
    width: 36,
    height: 3,
    borderRadius: 2,
  },
  loading: {
    minHeight: 220,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  errorText: {
    fontSize: 13,
  },
  body: {
    paddingBottom: 16,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 14,
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 12,
  },
  avatar: {
    width: 60,
    height: 60,
    borderRadius: 30,
  },
  avatarFallback: {
    borderWidth: StyleSheet.hairlineWidth,
    alignItems: 'center',
    justifyContent: 'center',
  },
  headerInfo: {
    flex: 1,
    gap: 6,
  },
  name: {
    fontSize: 16,
    fontWeight: '800',
  },
  chipsRow: {
    flexDirection: 'row',
    gap: 6,
  },
  chip: {
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
  },
  chipText: {
    fontSize: 10,
    fontWeight: '700',
  },
  matchCard: {
    marginHorizontal: 16,
    marginTop: 4,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    paddingHorizontal: 12,
    paddingVertical: 10,
    gap: 8,
  },
  matchTitle: {
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
  matchRow: {
    flexDirection: 'row',
    gap: 4,
  },
  matchTile: {
    flex: 1,
    alignItems: 'center',
    gap: 1,
  },
  matchTileValue: {
    fontSize: 16,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  matchTileLabel: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.4,
  },
  emptyHint: {
    fontSize: 12,
    textAlign: 'center',
    paddingVertical: 18,
  },
  detailBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    marginHorizontal: 16,
    marginTop: 14,
    paddingVertical: 12,
    borderRadius: 10,
  },
  detailBtnText: {
    fontSize: 14,
    fontWeight: '800',
    letterSpacing: 0.4,
  },
});
