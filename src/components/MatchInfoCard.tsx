import { format, parseISO } from 'date-fns';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { Country } from '@/src/types/country';
import type { FixtureSummary } from '@/src/types/fixture';
import type { League } from '@/src/types/league';

interface MatchInfoCardProps {
  fixture: FixtureSummary;
  league?: League;
  country?: Country;
}

export function MatchInfoCard({ fixture, league, country }: MatchInfoCardProps) {
  const rows: { label: string; value: string }[] = [];

  if (league?.name) {
    rows.push({ label: 'Tournament', value: league.name });
  }
  if (country?.name) {
    rows.push({ label: 'Country', value: country.name });
  }
  if (league?.type) {
    rows.push({
      label: 'Type',
      value: humanize(league.type),
    });
  }
  if (fixture.starting_at) {
    rows.push({
      label: 'Kick-off',
      value: format(parseISO(fixture.starting_at), 'EEE, d MMM • HH:mm'),
    });
  }
  if (fixture.length_minutes != null) {
    rows.push({ label: 'Length', value: `${fixture.length_minutes} min` });
  }
  if (fixture.leg) {
    rows.push({ label: 'Leg', value: humanize(fixture.leg) });
  }
  if (fixture.result_info) {
    rows.push({ label: 'Result', value: fixture.result_info });
  }

  if (rows.length === 0) return null;

  return <InfoCard title="Match Info" rows={rows} />;
}

function humanize(value: string): string {
  return value.replace(/[_-]+/g, ' ').replace(/\b\w/g, (m) => m.toUpperCase());
}

interface InfoCardProps {
  title: string;
  rows: { label: string; value: string }[];
}

function InfoCard({ title, rows }: InfoCardProps) {
  const c = useTheme();
  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <ThemedText style={[styles.title, { color: c.textMuted }]}>
        {title.toUpperCase()}
      </ThemedText>
      {rows.map((r, i) => (
        <View
          key={r.label}
          style={[
            styles.row,
            i > 0 && { borderTopWidth: StyleSheet.hairlineWidth, borderTopColor: c.border },
          ]}>
          <ThemedText style={[styles.label, { color: c.textMuted }]}>
            {r.label}
          </ThemedText>
          <ThemedText
            style={[styles.value, { color: c.text }]}
            numberOfLines={2}>
            {r.value}
          </ThemedText>
        </View>
      ))}
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
  title: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.5,
    paddingHorizontal: 14,
    paddingTop: 12,
    paddingBottom: 6,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 12,
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  label: {
    fontSize: 13,
    flexShrink: 0,
  },
  value: {
    fontSize: 14,
    fontWeight: '500',
    flexShrink: 1,
    textAlign: 'right',
  },
});
