import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureValueBet } from '@/src/types/fixtureDetailExtras';

interface AiPicksCardProps {
  bets: FixtureValueBet[] | undefined;
  homeName?: string | null;
  awayName?: string | null;
}

// SportMonks "value bets" reframed as AI-suggested picks. Tip text leans
// left to mirror the FixtureTopPicksCard layout; a compact percent on the
// right carries the model's confidence (stake reinterpreted as 0-100%).
// Odd values stay hidden — visibility of odds is a backend-driven concern.
export function AiPicksCard({ bets, homeName, awayName }: AiPicksCardProps) {
  const c = useTheme();
  const { t } = useTranslation();

  const items = useMemo(() => {
    if (!bets || bets.length === 0) return [];
    return [...bets]
      .sort((a, b) => (b.stake ?? 0) - (a.stake ?? 0))
      .slice(0, 3);
  }, [bets]);

  if (items.length === 0) return null;

  // Mirror the header's badge to whatever's inside the card — if any of
  // the visible picks is flagged as a value bet, surface that on the
  // title row too so the user doesn't have to scan the chips to know.
  const hasValuePick = items.some((b) => b.is_value);

  return (
    <View
      style={[
        styles.card,
        c.shadowCard,
        { backgroundColor: c.surfaceElevated, borderColor: c.brand },
      ]}>
      <View style={[styles.header, { backgroundColor: c.brandSoft }]}>
        <MaterialCommunityIcons name="robot" size={15} color={c.brand} />
        <ThemedText style={[styles.title, { color: c.brand }]}>
          {t('fixture.aiPicks.title')}
        </ThemedText>
        {hasValuePick ? (
          <View style={[styles.headerBadge, { backgroundColor: c.brand }]}>
            <MaterialCommunityIcons
              name="star-four-points"
              size={11}
              color={c.textInverse}
            />
            <ThemedText
              style={[styles.headerBadgeText, { color: c.textInverse }]}>
              {t('fixture.aiPicks.valueBadge')}
            </ThemedText>
          </View>
        ) : null}
      </View>
      {items.map((b, i) => {
        const confidence = Math.round(clamp01(b.stake ?? 0) * 100);
        return (
          <View
            key={b.id}
            style={[
              styles.row,
              i > 0 && {
                borderTopWidth: StyleSheet.hairlineWidth,
                borderTopColor: c.border,
              },
            ]}>
            <View style={styles.info}>
              <ThemedText
                style={[styles.tip, { color: c.brand }]}
                numberOfLines={2}>
                {formatTip(b.bet, homeName, awayName)}
              </ThemedText>
              <ThemedText
                style={[styles.subtitle, { color: c.textMuted }]}
                numberOfLines={1}>
                {b.is_value
                  ? t('fixture.aiPicks.valueLine')
                  : t('fixture.aiPicks.modelLine')}
              </ThemedText>
            </View>
            <View
              style={[
                styles.confidenceChip,
                { borderColor: c.brand },
                b.is_value ? { backgroundColor: c.brand } : null,
              ]}>
              <ThemedText
                style={[
                  styles.confidenceText,
                  { color: b.is_value ? c.textInverse : c.brand },
                ]}>
                {confidence}%
              </ThemedText>
            </View>
          </View>
        );
      })}
    </View>
  );
}

function clamp01(v: number): number {
  if (v <= 0) return 0;
  if (v >= 1) return 1;
  return v;
}

function formatTip(
  bet: string | null,
  homeName?: string | null,
  awayName?: string | null,
): string {
  if (!bet) return '—';
  const trimmed = bet.trim();
  if (trimmed === '1' && homeName) return homeName;
  if (trimmed === '2' && awayName) return awayName;
  if (trimmed.toUpperCase() === 'X') return 'X';
  return trimmed;
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
    gap: 6,
    paddingHorizontal: 12,
    paddingVertical: 8,
  },
  title: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.4,
    flex: 1,
  },
  headerBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: 8,
    paddingVertical: 3,
    borderRadius: 999,
  },
  headerBadgeText: {
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 14,
    gap: 12,
  },
  info: {
    flex: 1,
    gap: 2,
  },
  tip: {
    fontSize: 14,
    fontWeight: '700',
  },
  subtitle: {
    fontSize: 11,
  },
  confidenceChip: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
    minWidth: 56,
    alignItems: 'center',
  },
  confidenceText: {
    fontSize: 13,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
});
