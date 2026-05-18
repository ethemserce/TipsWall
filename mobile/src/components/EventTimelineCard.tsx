import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useTranslation } from 'react-i18next';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureEvent } from '@/src/types/fixtureDetailExtras';

const SUB_IN_COLOR = '#22c55e';
const SUB_OUT_COLOR = '#ef4444';
const INJURY_IN_COLOR = '#3b82f6';
const INJURY_OUT_COLOR = '#ef4444';

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
  const { t } = useTranslation();
  const isHome = event.participant_location === 'home';
  const minuteLabel = formatMinute(event);
  const code = (event.type_code ?? '').toUpperCase();
  const isSubstitution = code === 'SUBSTITUTION';
  const cancelled = event.cancelled === true;
  const icon = isSubstitution ? null : iconFor(event.type_code);
  const title = primaryLabel(event);
  const subtitle = secondaryLabel(event);

  // SofaScore / FotMob convention: a VAR-overturned goal stays in the
  // timeline with strikethrough text, muted colour, and an "İPTAL" /
  // "CANCELLED" badge next to the scorer name. Removing it entirely
  // would leave the user wondering where the goal they just watched
  // went; keeping it transparent matches every major sports app.
  const cancelledStyle = cancelled
    ? { textDecorationLine: 'line-through' as const }
    : null;
  const textColor = cancelled ? c.textMuted : c.text;

  // Substitutions render as "{in} {out}" inline with a swap-icon in
  // between (matching the legacy fixture-card style). Other events keep
  // their text-block layout with the type icon flush to the touchline.
  const renderSide = () => {
    if (isSubstitution) {
      return (
        <SubstitutionRow
          incoming={event.player_name}
          outgoing={event.related_player_name}
          injured={event.injured === true}
          align={isHome ? 'right' : 'left'}
          textColor={c.text}
          mutedColor={c.textMuted}
        />
      );
    }
    const cancelBadge = cancelled ? (
      <View
        style={[
          styles.cancelBadge,
          { backgroundColor: c.danger ?? '#ef4444' },
        ]}>
        <ThemedText style={[styles.cancelBadgeText, { color: c.textInverse }]}>
          {t('fixture.events.cancelledBadge', { defaultValue: 'İPTAL' })}
        </ThemedText>
      </View>
    ) : null;
    const block = (
      <View style={isHome ? styles.textBlockHome : styles.textBlockAway}>
        <ThemedText
          style={[styles.player, cancelledStyle, { color: textColor }]}
          numberOfLines={1}>
          {title}
        </ThemedText>
        {subtitle ? (
          <ThemedText
            style={[styles.subtitle, cancelledStyle, { color: c.textMuted }]}
            numberOfLines={1}>
            {subtitle}
          </ThemedText>
        ) : null}
      </View>
    );
    return isHome ? (
      <View style={styles.homeContent}>
        <ThemedText style={styles.icon}>{icon}</ThemedText>
        {block}
        {cancelBadge}
      </View>
    ) : (
      <View style={styles.awayContent}>
        {cancelBadge}
        {block}
        <ThemedText style={styles.icon}>{icon}</ThemedText>
      </View>
    );
  };

  return (
    <View style={[styles.row, { borderTopColor: c.border }]}>
      <View style={styles.homeColumn}>{isHome ? renderSide() : null}</View>
      <ThemedText style={[styles.minute, { color: c.textMuted }]}>
        {minuteLabel}
      </ThemedText>
      <View style={styles.awayColumn}>{!isHome ? renderSide() : null}</View>
    </View>
  );
}

function SubstitutionRow({
  incoming,
  outgoing,
  injured,
  align,
  textColor,
  mutedColor,
}: {
  incoming: string | null;
  outgoing: string | null;
  injured: boolean;
  align: 'left' | 'right';
  textColor: string;
  mutedColor: string;
}) {
  // SportMonks: player_name = ON, related_player_name = OFF. Render as
  // a 2-line stack — incoming player on top (regular size), outgoing
  // below (smaller, muted). Up-arrow next to in, down-arrow next to
  // out — coloured by injury vs regular swap.
  const inColor = injured ? INJURY_IN_COLOR : SUB_IN_COLOR;
  const outColor = injured ? INJURY_OUT_COLOR : SUB_OUT_COLOR;
  const inRow = (
    <View style={align === 'right' ? styles.subLineRight : styles.subLineLeft}>
      <MaterialCommunityIcons name="arrow-up-bold" size={12} color={inColor} />
      <ThemedText style={[styles.subInText, { color: textColor }]} numberOfLines={1}>
        {abbreviateName(incoming)}
      </ThemedText>
    </View>
  );
  const outRow = (
    <View style={align === 'right' ? styles.subLineRight : styles.subLineLeft}>
      <MaterialCommunityIcons name="arrow-down-bold" size={10} color={outColor} />
      <ThemedText style={[styles.subOutText, { color: mutedColor }]} numberOfLines={1}>
        {abbreviateName(outgoing)}
      </ThemedText>
    </View>
  );

  return (
    <View style={align === 'right' ? styles.subStackRight : styles.subStackLeft}>
      {inRow}
      {outRow}
    </View>
  );
}

function abbreviateName(name: string | null | undefined): string {
  if (!name) return '—';
  const trimmed = name.trim();
  // SportMonks ships "<TBD>" as a placeholder string while a goal is
  // under VAR review or the scorer hasn't been confirmed yet. We
  // never want to render that literal — fall back to em-dash so the
  // row stays clean and the user reads minute + score line instead.
  if (!trimmed || trimmed === '<TBD>') return '—';
  const parts = trimmed.split(/\s+/);
  if (parts.length === 1) return parts[0];
  // "Andreas Cornelius" → "A. Cornelius"
  const first = parts[0];
  const last = parts[parts.length - 1];
  return `${first.charAt(0).toUpperCase()}. ${last}`;
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
  // SportMonks substitution payload: player_name = player coming ON,
  // related_player_name = player coming OFF. Show the new player as the
  // primary line and the outgoing player below.
  const code = (e.type_code ?? '').toUpperCase();
  if (code === 'SUBSTITUTION') {
    return e.player_name ?? e.related_player_name ?? '—';
  }
  // VAR rows: the icon column already prints the literal "VAR" badge,
  // and SportMonks ships type_name = "VAR" too. Without a special-case
  // the row reads "VAR VAR / Red card" because primaryLabel returns
  // the type_name and the info ("Red card", "Penalty") lands on the
  // secondary line. Promote the info to the primary slot so the row
  // reads "VAR Red card" without the duplicate.
  if (code === 'VAR' || code === 'VAR_CARD') {
    return e.info ?? e.type_name ?? 'VAR';
  }
  // Goals: keep the running score on the same line as the scorer so we
  // don't burn a second row just to show "0-1".
  const name = e.player_name ?? e.type_name ?? '—';
  if ((code === 'GOAL' || code === 'OWNGOAL' || code === 'PENALTY') && e.result) {
    return `${name} ${e.result}`;
  }
  return name;
}

function secondaryLabel(e: FixtureEvent): string | null {
  const code = (e.type_code ?? '').toUpperCase();
  if (code === 'SUBSTITUTION' && e.related_player_name) {
    return `↓ ${e.related_player_name}`;
  }
  // Goal: show the assister directly under the scorer name (SportMonks
  // ships the assist in `related_player_name`). Own goals don't have
  // an assist; penalties are explicit so we skip them too.
  if (code === 'GOAL' && e.related_player_name) {
    return `🅰 ${e.related_player_name}`;
  }
  if (code === 'OWNGOAL' || code === 'PENALTY') return null;
  // VAR events already promote the info ("Red card" / "Penalty") to
  // the primary line — no second line needed.
  if (code === 'VAR' || code === 'VAR_CARD') return null;
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
    width: 44,
    fontSize: 12,
    fontWeight: '600',
    fontVariant: ['tabular-nums'],
    textAlign: 'center',
  },
  homeColumn: {
    flex: 1,
    alignItems: 'flex-start',
  },
  awayColumn: {
    flex: 1,
    alignItems: 'flex-end',
  },
  homeContent: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    maxWidth: '100%',
  },
  awayContent: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    maxWidth: '100%',
  },
  textBlockHome: {
    alignItems: 'flex-start',
    flexShrink: 1,
  },
  textBlockAway: {
    alignItems: 'flex-end',
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
  // VAR-overturned goal badge — small red pill next to the strikethrough
  // scorer line. Sits between the icon and the text block (or after,
  // mirrored to the away side) so it reads alongside the player name.
  cancelBadge: {
    paddingHorizontal: 5,
    paddingVertical: 1,
    borderRadius: 4,
  },
  cancelBadgeText: {
    fontSize: 9,
    fontWeight: '800',
    letterSpacing: 0.5,
  },
  // Vertical stack: incoming player on top (regular weight), outgoing
  // underneath in a smaller, muted size. Right-side substitutions align
  // the rows + their arrow icon to the right edge.
  subStackLeft: {
    flexDirection: 'column',
    alignItems: 'flex-start',
    flexShrink: 1,
    gap: 1,
  },
  subStackRight: {
    flexDirection: 'column',
    alignItems: 'flex-end',
    flexShrink: 1,
    gap: 1,
  },
  subLineLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    flexShrink: 1,
  },
  subLineRight: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    flexShrink: 1,
    justifyContent: 'flex-end',
  },
  subInText: {
    fontSize: 13,
    fontWeight: '700',
    flexShrink: 1,
  },
  subOutText: {
    fontSize: 11,
    fontWeight: '500',
    flexShrink: 1,
  },
});
