import { useTranslation } from 'react-i18next';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureEvent } from '@/src/types/fixtureDetailExtras';

interface EventTimelineCardProps {
  events: FixtureEvent[];
}

export function EventTimelineCard({ events }: EventTimelineCardProps) {
  const c = useTheme();
  const visible = events.filter((e) => isUserFacing(e));

  if (visible.length === 0) return null;

  const firstHalf: FixtureEvent[] = [];
  const secondHalf: FixtureEvent[] = [];
  for (const e of visible) {
    const m = e.minute ?? 0;
    if (m <= 45) firstHalf.push(e);
    else secondHalf.push(e);
  }
  // Latest minute on top: reverse within half, render 2nd half first.
  firstHalf.reverse();
  secondHalf.reverse();

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      {secondHalf.length > 0 ? (
        <Section i18nKey="fixture.events.secondHalf" events={secondHalf} first />
      ) : null}
      {firstHalf.length > 0 ? (
        <Section
          i18nKey="fixture.events.firstHalf"
          events={firstHalf}
          first={secondHalf.length === 0}
        />
      ) : null}
    </View>
  );
}

function Section({
  i18nKey,
  events,
  first,
}: {
  i18nKey: string;
  events: FixtureEvent[];
  first: boolean;
}) {
  const c = useTheme();
  const { t } = useTranslation();
  return (
    <>
      <ThemedText
        style={[
          styles.sectionLabel,
          {
            color: c.textMuted,
            borderTopColor: c.border,
            borderTopWidth: first ? 0 : StyleSheet.hairlineWidth,
          },
        ]}>
        {t(i18nKey).toUpperCase()}
      </ThemedText>
      {events.map((e) => (
        <EventRow key={e.id} event={e} />
      ))}
    </>
  );
}

function EventRow({ event }: { event: FixtureEvent }) {
  const c = useTheme();
  const isHome = event.participant_location === 'home';
  const minuteLabel = formatMinute(event);
  const icon = iconFor(event.type_code);
  const title = primaryLabel(event);
  const subtitle = secondaryLabel(event);

  return (
    <View style={[styles.row, { borderTopColor: c.border }]}>
      <ThemedText style={[styles.minute, { color: c.textMuted }]}>
        {minuteLabel}
      </ThemedText>
      {isHome ? (
        <View style={styles.sideHome}>
          <View style={styles.textBlockHome}>
            <ThemedText style={[styles.player, { color: c.text }]} numberOfLines={1}>
              {title}
            </ThemedText>
            {subtitle ? (
              <ThemedText style={[styles.subtitle, { color: c.textMuted }]} numberOfLines={1}>
                {subtitle}
              </ThemedText>
            ) : null}
          </View>
          <ThemedText style={styles.icon}>{icon}</ThemedText>
        </View>
      ) : (
        <View style={styles.sideAway}>
          <ThemedText style={styles.icon}>{icon}</ThemedText>
          <View style={styles.textBlockAway}>
            <ThemedText style={[styles.player, { color: c.text }]} numberOfLines={1}>
              {title}
            </ThemedText>
            {subtitle ? (
              <ThemedText style={[styles.subtitle, { color: c.textMuted }]} numberOfLines={1}>
                {subtitle}
              </ThemedText>
            ) : null}
          </View>
        </View>
      )}
    </View>
  );
}

function isUserFacing(e: FixtureEvent): boolean {
  const code = (e.type_code ?? '').toUpperCase();
  // Skip noisy event types like shot indicators.
  return [
    'GOAL',
    'OWNGOAL',
    'PENALTY',
    'GOAL_AWARDED',
    'GOAL_DISALLOWED',
    'YELLOWCARD',
    'REDCARD',
    'YELLOWREDCARD',
    'CARD_ADJUSTED',
    'SUBSTITUTION',
    'VAR',
    'VAR_CARD',
    'PENALTY_MISSED',
  ].includes(code);
}

function iconFor(code: string | null | undefined): string {
  switch ((code ?? '').toUpperCase()) {
    case 'GOAL':
    case 'GOAL_AWARDED':
    case 'PENALTY':
      return '⚽';
    case 'OWNGOAL':
      return '🥅';
    case 'GOAL_DISALLOWED':
    case 'PENALTY_MISSED':
      return '✗';
    case 'YELLOWCARD':
    case 'CARD_ADJUSTED':
      return '🟨';
    case 'REDCARD':
    case 'YELLOWREDCARD':
      return '🟥';
    case 'SUBSTITUTION':
      return '↻';
    case 'VAR':
    case 'VAR_CARD':
      return 'VAR';
    default:
      return '·';
  }
}

function primaryLabel(e: FixtureEvent): string {
  if (e.type_code?.toUpperCase() === 'SUBSTITUTION') {
    return e.related_player_name ?? e.player_name ?? '—';
  }
  return e.player_name ?? e.type_name ?? '—';
}

function secondaryLabel(e: FixtureEvent): string | null {
  const code = (e.type_code ?? '').toUpperCase();
  if (code === 'SUBSTITUTION' && e.player_name) {
    return `for ${e.player_name}`;
  }
  if (code === 'GOAL' || code === 'OWNGOAL' || code === 'PENALTY') {
    return e.result ?? null;
  }
  return e.info ?? null;
}

function formatMinute(e: FixtureEvent): string {
  if (e.minute == null) return '';
  if (e.extra_minute && e.extra_minute > 0) return `${e.minute}+${e.extra_minute}'`;
  return `${e.minute}'`;
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    overflow: 'hidden',
  },
  sectionLabel: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.5,
    paddingHorizontal: 14,
    paddingTop: 12,
    paddingBottom: 6,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  minute: {
    width: 40,
    fontSize: 12,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
    textAlign: 'center',
  },
  sideHome: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'flex-end',
    gap: 8,
  },
  sideAway: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'flex-start',
    gap: 8,
  },
  textBlockHome: {
    alignItems: 'flex-end',
    flexShrink: 1,
  },
  textBlockAway: {
    alignItems: 'flex-start',
    flexShrink: 1,
  },
  player: {
    fontSize: 13,
    fontWeight: '500',
  },
  subtitle: {
    fontSize: 11,
    marginTop: 2,
  },
  icon: {
    fontSize: 14,
  },
});
