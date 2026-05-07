import { format, parseISO } from 'date-fns';
import { Image } from 'expo-image';
import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureSummary } from '@/src/types/fixture';

interface H2HTabProps {
  loading: boolean;
  fixtures: FixtureSummary[];
  homeTeamId?: number | null;
  awayTeamId?: number | null;
}

export function H2HTab({ loading, fixtures, homeTeamId, awayTeamId }: H2HTabProps) {
  const c = useTheme();

  if (loading && fixtures.length === 0) {
    return (
      <View style={styles.empty}>
        <ActivityIndicator color={c.brand} />
      </View>
    );
  }

  if (fixtures.length === 0) {
    return (
      <View style={styles.empty}>
        <ThemedText style={[styles.emptyText, { color: c.textMuted }]}>
          No previous matches between these teams yet.
        </ThemedText>
      </View>
    );
  }

  const summary = computeRecord(fixtures, homeTeamId, awayTeamId);

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <ThemedText style={[styles.title, { color: c.textMuted }]}>
        HEAD-TO-HEAD ({fixtures.length})
      </ThemedText>
      {summary ? (
        <View style={[styles.summaryRow, { borderTopColor: c.border }]}>
          <SummaryCell label="Home wins" value={summary.homeWins} color={c.brand} />
          <SummaryCell label="Draws" value={summary.draws} color={c.textMuted} />
          <SummaryCell label="Away wins" value={summary.awayWins} color={c.live} />
        </View>
      ) : null}
      {fixtures.map((f) => (
        <H2HRow key={f.id} fixture={f} />
      ))}
    </View>
  );
}

function SummaryCell({
  label,
  value,
  color,
}: {
  label: string;
  value: number;
  color: string;
}) {
  const c = useTheme();
  return (
    <View style={styles.summaryCell}>
      <ThemedText style={[styles.summaryValue, { color }]}>{value}</ThemedText>
      <ThemedText style={[styles.summaryLabel, { color: c.textMuted }]}>
        {label}
      </ThemedText>
    </View>
  );
}

function H2HRow({ fixture }: { fixture: FixtureSummary }) {
  const c = useTheme();
  const router = useRouter();
  const date = fixture.starting_at
    ? format(parseISO(fixture.starting_at), 'd MMM yyyy')
    : '';
  const showScore =
    fixture.home_score != null && fixture.away_score != null;

  return (
    <Pressable
      onPress={() => router.push(`/fixture/${fixture.id}` as never)}
      style={({ pressed }) => [
        styles.row,
        { borderTopColor: c.border, backgroundColor: pressed ? c.bg : 'transparent' },
      ]}>
      <ThemedText style={[styles.date, { color: c.textMuted }]}>
        {date}
      </ThemedText>
      <View style={styles.teamsBlock}>
        <TeamSide
          name={fixture.home_team_name}
          imagePath={fixture.home_team_image_path}
          alignRight
        />
        <View style={styles.scoreBox}>
          {showScore ? (
            <ThemedText style={[styles.score, { color: c.text }]}>
              {fixture.home_score} - {fixture.away_score}
            </ThemedText>
          ) : (
            <ThemedText style={[styles.score, { color: c.textMuted }]}>vs</ThemedText>
          )}
        </View>
        <TeamSide
          name={fixture.away_team_name}
          imagePath={fixture.away_team_image_path}
        />
      </View>
    </Pressable>
  );
}

function TeamSide({
  name,
  imagePath,
  alignRight,
}: {
  name: string | null | undefined;
  imagePath: string | null | undefined;
  alignRight?: boolean;
}) {
  const c = useTheme();
  const items = [
    imagePath ? (
      <Image
        key="logo"
        source={{ uri: imagePath }}
        style={styles.logo}
        contentFit="contain"
      />
    ) : (
      <View
        key="logo"
        style={[styles.logoPlaceholder, { backgroundColor: c.border }]}
      />
    ),
    <ThemedText
      key="name"
      style={[styles.team, { color: c.text }, alignRight && styles.teamRight]}
      numberOfLines={1}>
      {name ?? 'TBD'}
    </ThemedText>,
  ];
  return (
    <View
      style={[styles.teamSide, alignRight && styles.teamSideRight]}>
      {alignRight ? items.reverse() : items}
    </View>
  );
}

function computeRecord(
  fixtures: FixtureSummary[],
  homeTeamId: number | null | undefined,
  awayTeamId: number | null | undefined,
) {
  if (homeTeamId == null || awayTeamId == null) return null;
  let homeWins = 0;
  let awayWins = 0;
  let draws = 0;
  for (const f of fixtures) {
    if (f.home_score == null || f.away_score == null) continue;
    if (f.home_score === f.away_score) {
      draws++;
      continue;
    }
    const homeWonMatch = f.home_score > f.away_score;
    const winnerTeamId = homeWonMatch ? f.home_team_id : f.away_team_id;
    if (winnerTeamId == null) continue;
    if (winnerTeamId === homeTeamId) homeWins++;
    else if (winnerTeamId === awayTeamId) awayWins++;
  }
  return { homeWins, draws, awayWins };
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
  summaryRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  summaryCell: {
    flex: 1,
    alignItems: 'center',
    gap: 2,
  },
  summaryValue: {
    fontSize: 18,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  summaryLabel: {
    fontSize: 10,
    fontWeight: '600',
    letterSpacing: 0.4,
    textTransform: 'uppercase',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 10,
  },
  date: {
    width: 80,
    fontSize: 11,
  },
  teamsBlock: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
  },
  teamSide: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  teamSideRight: {
    justifyContent: 'flex-end',
  },
  team: {
    flexShrink: 1,
    fontSize: 13,
  },
  teamRight: {
    textAlign: 'right',
  },
  logo: {
    width: 18,
    height: 18,
  },
  logoPlaceholder: {
    width: 18,
    height: 18,
    borderRadius: 3,
  },
  scoreBox: {
    minWidth: 50,
    alignItems: 'center',
  },
  score: {
    fontSize: 13,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  empty: {
    paddingVertical: 64,
    alignItems: 'center',
  },
  emptyText: {
    fontSize: 14,
  },
});
