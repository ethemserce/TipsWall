import { Image } from 'expo-image';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { Country } from '@/src/types/country';
import type { League } from '@/src/types/league';

interface LeagueHeaderProps {
  leagueId: number;
  league?: League;
  country?: Country;
  fixtureCount: number;
}

export function LeagueHeader({
  leagueId,
  league,
  country,
  fixtureCount,
}: LeagueHeaderProps) {
  const c = useTheme();
  const title = league?.name ?? `League #${leagueId}`;
  const subtitle = country?.name ?? league?.short_code ?? null;

  return (
    <View style={[styles.row, { backgroundColor: c.surface, borderColor: c.border }]}>
      {country?.image_path ? (
        <Image
          source={{ uri: country.image_path }}
          style={styles.flag}
          contentFit="cover"
          transition={150}
        />
      ) : league?.image_path ? (
        <Image
          source={{ uri: league.image_path }}
          style={styles.logo}
          contentFit="contain"
          transition={150}
        />
      ) : (
        <View style={[styles.logoPlaceholder, { backgroundColor: c.border }]} />
      )}

      <View style={styles.titleBlock}>
        <ThemedText style={[styles.title, { color: c.text }]} numberOfLines={1}>
          {title}
        </ThemedText>
        {subtitle ? (
          <ThemedText style={[styles.subtitle, { color: c.textMuted }]} numberOfLines={1}>
            {subtitle}
          </ThemedText>
        ) : null}
      </View>

      <View style={[styles.badge, { backgroundColor: c.surfaceElevated, borderColor: c.border }]}>
        <ThemedText style={[styles.badgeText, { color: c.textMuted }]}>{fixtureCount}</ThemedText>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
    gap: 10,
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
  title: {
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0.3,
    textTransform: 'uppercase',
  },
  subtitle: {
    fontSize: 11,
    marginTop: 2,
  },
  badge: {
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
    minWidth: 24,
    alignItems: 'center',
  },
  badgeText: {
    fontSize: 11,
    fontWeight: '600',
  },
});
