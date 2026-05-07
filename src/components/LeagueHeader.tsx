import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { League } from '@/src/types/league';

interface LeagueHeaderProps {
  leagueId: number;
  league?: League;
  fixtureCount: number;
}

export function LeagueHeader({ leagueId, league, fixtureCount }: LeagueHeaderProps) {
  const c = useTheme();
  const title = league?.name ?? `League #${leagueId}`;
  const subtitle = league?.short_code ?? league?.type ?? null;

  return (
    <View style={[styles.row, { backgroundColor: c.surface, borderColor: c.border }]}>
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
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
    gap: 8,
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
