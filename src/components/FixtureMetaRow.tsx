import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { Country } from '@/src/types/country';
import type { FixtureSummary } from '@/src/types/fixture';
import type { League } from '@/src/types/league';

interface FixtureMetaRowProps {
  fixture: FixtureSummary;
  league?: League;
  country?: Country;
}

export function FixtureMetaRow({ fixture, league, country }: FixtureMetaRowProps) {
  const c = useTheme();

  const kickoffDate = fixture.starting_at
    ? format(parseISO(fixture.starting_at), 'EEEE, d MMMM yyyy')
    : null;
  const kickoffTime = fixture.starting_at
    ? format(parseISO(fixture.starting_at), 'HH:mm')
    : null;

  return (
    <View style={[styles.container, { backgroundColor: c.bg, borderColor: c.border }]}>
      <View style={[styles.row, styles.firstRow]}>
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
        <View style={styles.rowText}>
          <ThemedText style={[styles.value, { color: c.text }]} numberOfLines={1}>
            {league?.name ?? `League #${fixture.league_id}`}
          </ThemedText>
          {country?.name ? (
            <ThemedText style={[styles.label, { color: c.textMuted }]} numberOfLines={1}>
              {country.name}
            </ThemedText>
          ) : null}
        </View>
      </View>

      {kickoffDate ? (
        <View style={[styles.row, { borderTopColor: c.border }]}>
          <ThemedText style={[styles.label, { color: c.textMuted }]}>
            Kick-off
          </ThemedText>
          <ThemedText style={[styles.value, { color: c.text }]}>
            {kickoffDate} • {kickoffTime}
          </ThemedText>
        </View>
      ) : null}

      {fixture.length_minutes != null ? (
        <View style={[styles.row, { borderTopColor: c.border }]}>
          <ThemedText style={[styles.label, { color: c.textMuted }]}>
            Length
          </ThemedText>
          <ThemedText style={[styles.value, { color: c.text }]}>
            {fixture.length_minutes} min
          </ThemedText>
        </View>
      ) : null}

      {fixture.result_info ? (
        <View style={[styles.row, { borderTopColor: c.border }]}>
          <ThemedText style={[styles.label, { color: c.textMuted }]}>
            Result
          </ThemedText>
          <ThemedText style={[styles.value, { color: c.text }]} numberOfLines={2}>
            {fixture.result_info}
          </ThemedText>
        </View>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 10,
    overflow: 'hidden',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 14,
    paddingVertical: 12,
    gap: 12,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  firstRow: {
    borderTopWidth: 0,
  },
  rowText: {
    flex: 1,
    minWidth: 0,
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
  label: {
    fontSize: 12,
    flexShrink: 0,
  },
  value: {
    fontSize: 14,
    fontWeight: '500',
    flexShrink: 1,
    textAlign: 'right',
  },
});
