import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Image } from 'expo-image';
import { router } from 'expo-router';
import { useTranslation } from 'react-i18next';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { Country } from '@/src/types/country';
import type { League } from '@/src/types/league';

interface LeagueHeaderProps {
  leagueId: number;
  league?: League;
  country?: Country;
  fixtureCount: number;
  /** When provided the row becomes a tappable expand/collapse control. */
  onToggle?: () => void;
  collapsed?: boolean;
  /** True if the section has at least one in-play fixture. */
  hasLive?: boolean;
}

export function LeagueHeader({
  leagueId,
  league,
  country,
  fixtureCount,
  onToggle,
  collapsed = false,
  hasLive = false,
}: LeagueHeaderProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const title = league?.name ?? `League #${leagueId}`;
  const subtitle = country?.name ?? league?.short_code ?? null;

  const handleNavigate = () => {
    router.push(`/league/${leagueId}` as never);
  };

  // Only the logo is the league-detail tap target — taps on the title
  // text or anywhere else fall through to the outer row's collapse
  // toggle. Inner Pressable wins over outer in RN's gesture system, so
  // tapping the logo doesn't double-fire the toggle.
  const Logo = (
    <Pressable
      onPress={handleNavigate}
      hitSlop={6}
      accessibilityRole="button"
      accessibilityLabel={title}
      style={({ pressed }) => [
        styles.logoWrap,
        { backgroundColor: c.surface },
        pressed && { opacity: 0.6 },
      ]}>
      {league?.image_path ? (
        <Image
          source={{ uri: league.image_path }}
          style={styles.logo}
          contentFit="contain"
          transition={150}
        />
      ) : country?.image_path ? (
        <Image
          source={{ uri: country.image_path }}
          style={styles.flag}
          contentFit="cover"
          transition={150}
        />
      ) : (
        <View style={[styles.logoPlaceholder, { backgroundColor: c.border }]} />
      )}
    </Pressable>
  );

  const Body = (
    <View style={styles.row}>
      {Logo}

      <View style={styles.titleBlock}>
        <View style={styles.titleRow}>
          <ThemedText
            style={[styles.title, { color: c.text }]}
            numberOfLines={1}>
            {title}
          </ThemedText>
          {hasLive ? (
            <View style={[styles.livePill, { backgroundColor: c.live }]}>
              <View style={styles.liveDot} />
              <ThemedText style={styles.liveText}>CANLI</ThemedText>
            </View>
          ) : null}
        </View>
        {subtitle ? (
          <View style={styles.subtitleRow}>
            {country?.image_path ? (
              <Image
                source={{ uri: country.image_path }}
                style={styles.flagSmall}
                contentFit="cover"
                transition={150}
              />
            ) : null}
            <ThemedText
              style={[styles.subtitle, { color: c.textMuted }]}
              numberOfLines={1}>
              {subtitle}
            </ThemedText>
          </View>
        ) : null}
      </View>

      <View
        style={[
          styles.badge,
          { backgroundColor: c.bg, borderColor: c.borderSoft },
        ]}>
        <ThemedText style={[styles.badgeText, { color: c.textMuted }]}>
          {fixtureCount}
        </ThemedText>
      </View>

      {onToggle ? (
        <MaterialCommunityIcons
          name={collapsed ? 'chevron-down' : 'chevron-up'}
          size={20}
          color={c.textMuted}
        />
      ) : null}
    </View>
  );

  if (!onToggle) {
    return <View style={[styles.surface, { backgroundColor: c.surfaceElevated, borderBottomColor: c.borderSoft }]}>{Body}</View>;
  }

  return (
    <Pressable
      onPress={onToggle}
      hitSlop={4}
      accessibilityRole="button"
      accessibilityLabel={
        collapsed
          ? t('league.toggleExpand', { name: title })
          : t('league.toggleCollapse', { name: title })
      }
      accessibilityState={{ expanded: !collapsed }}
      style={({ pressed }) => [
        styles.surface,
        {
          backgroundColor: pressed ? c.brandSoft : c.surfaceElevated,
          borderBottomColor: collapsed ? 'transparent' : c.borderSoft,
        },
      ]}>
      {Body}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  surface: {
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 10,
    gap: 10,
  },
  logoWrap: {
    width: 32,
    height: 32,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
  },
  flag: {
    width: 22,
    height: 16,
    borderRadius: 2,
  },
  logo: {
    width: 22,
    height: 22,
  },
  logoPlaceholder: {
    width: 22,
    height: 22,
    borderRadius: 4,
  },
  titleBlock: {
    flex: 1,
  },
  titleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  title: {
    flex: 1,
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0.3,
    textTransform: 'uppercase',
  },
  livePill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 999,
  },
  liveDot: {
    width: 5,
    height: 5,
    borderRadius: 3,
    backgroundColor: '#ffffff',
  },
  liveText: {
    fontSize: 9,
    fontWeight: '900',
    letterSpacing: 0.6,
    color: '#ffffff',
  },
  subtitle: {
    fontSize: 11,
    flexShrink: 1,
  },
  subtitleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 5,
    marginTop: 2,
  },
  flagSmall: {
    width: 12,
    height: 9,
    borderRadius: 1,
  },
  badge: {
    paddingHorizontal: 9,
    paddingVertical: 3,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
    minWidth: 28,
    alignItems: 'center',
  },
  badgeText: {
    fontSize: 11,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
});
