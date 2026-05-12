import { format, parseISO } from 'date-fns';
import { enUS, tr as trLocale } from 'date-fns/locale';
import { useTranslation } from 'react-i18next';
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
  const { t, i18n } = useTranslation();
  const locale = i18n.language.startsWith('tr') ? trLocale : enUS;
  const rows: { label: string; value: string }[] = [];

  if (league?.name) {
    rows.push({ label: t('fixture.info.tournament'), value: league.name });
  }
  if (country?.name) {
    rows.push({ label: t('fixture.info.country'), value: country.name });
  }
  if (league?.type) {
    rows.push({
      label: t('fixture.info.type'),
      value: humanize(league.type),
    });
  }
  if (fixture.starting_at) {
    rows.push({
      label: t('fixture.info.kickOff'),
      value: format(parseISO(fixture.starting_at), 'EEE, d MMM • HH:mm', { locale }),
    });
  }
  if (fixture.length_minutes != null) {
    rows.push({
      label: t('fixture.info.length'),
      value: t('fixture.info.lengthMinutes', { count: fixture.length_minutes }),
    });
  }
  if (fixture.leg) {
    rows.push({ label: t('fixture.info.leg'), value: humanize(fixture.leg) });
  }
  if (fixture.venue_name) {
    rows.push({ label: t('fixture.info.venue'), value: fixture.venue_name });
  }
  if (fixture.referee_name) {
    rows.push({ label: t('fixture.info.referee'), value: fixture.referee_name });
  }
  if (fixture.result_info) {
    rows.push({ label: t('fixture.info.result'), value: fixture.result_info });
  }

  if (rows.length === 0) return null;

  return <InfoCard rows={rows} />;
}

function humanize(value: string): string {
  return value.replace(/[_-]+/g, ' ').replace(/\b\w/g, (m) => m.toUpperCase());
}

interface InfoCardProps {
  rows: { label: string; value: string }[];
}

function InfoCard({ rows }: InfoCardProps) {
  const c = useTheme();
  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
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
