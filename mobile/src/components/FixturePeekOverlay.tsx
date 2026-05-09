import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { format, parseISO } from 'date-fns';
import { useEffect, useMemo, useRef } from 'react';
import {
  Animated,
  Modal,
  PanResponder,
  Pressable,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';

import { EventTimelineCard } from '@/src/components/EventTimelineCard';
import { FixtureDetailHero } from '@/src/components/FixtureDetailHero';
import { useCountryLookup } from '@/src/hooks/useCountryLookup';
import { useFixtureEvents } from '@/src/hooks/useFixtureExtras';
import { useFixtures } from '@/src/hooks/useFixtures';
import { useLeagueLookup } from '@/src/hooks/useLeagueLookup';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureSummary } from '@/src/types/fixture';

interface Props {
  fixture: FixtureSummary | null;
  // Lock starts unset; the home screen flips it after a 2s hold so the
  // peek pins open. While locked the X is the only exit and the timeline
  // becomes scrollable; before that the user is still pressing the source
  // row and we have to keep pointerEvents off so press-out fires.
  locked: boolean;
  onClose: () => void;
  // Called when a horizontal swipe inside the locked peek lands on a
  // sibling fixture from the same league + matchday — lets the parent
  // flip the peeked fixture without dismissing the overlay.
  onChangeFixture?: (next: FixtureSummary) => void;
}

const SWIPE_DOMINANCE = 1.5;
const SWIPE_TRIGGER_DISTANCE = 50;
const SWIPE_RECOGNITION_THRESHOLD = 12;

export function FixturePeekOverlay({
  fixture,
  locked,
  onClose,
  onChangeFixture,
}: Props) {
  const opacity = useRef(new Animated.Value(0)).current;
  const visible = fixture != null;

  useEffect(() => {
    Animated.timing(opacity, {
      toValue: visible ? 1 : 0,
      duration: visible ? 140 : 100,
      useNativeDriver: true,
    }).start();
  }, [opacity, visible]);

  return (
    <Modal
      visible={visible}
      transparent
      animationType="none"
      onRequestClose={onClose}
      statusBarTranslucent>
      <Animated.View
        // While unlocked we have to be transparent to touches so the
        // user's still-active long-press registers onPressOut on the
        // source row when they lift. Once locked we own touches.
        pointerEvents={locked ? 'auto' : 'none'}
        style={[styles.backdrop, { opacity }]}>
        {fixture ? (
          <PeekContent
            fixture={fixture}
            locked={locked}
            onClose={onClose}
            onChangeFixture={onChangeFixture}
          />
        ) : null}
      </Animated.View>
    </Modal>
  );
}

function PeekContent({
  fixture,
  locked,
  onClose,
  onChangeFixture,
}: {
  fixture: FixtureSummary;
  locked: boolean;
  onClose: () => void;
  onChangeFixture?: (next: FixtureSummary) => void;
}) {
  const c = useTheme();
  const events = useFixtureEvents(fixture.id, true);

  // Sibling matches in the same league on this date so a horizontal
  // swipe inside the locked peek can flip between them. Cached by the
  // home list's useFixtures call when available — first peek of the
  // day still triggers a network round-trip for the league shard.
  const fixtureDate = fixture.starting_at
    ? format(parseISO(fixture.starting_at), 'yyyy-MM-dd')
    : null;
  const { data: dayFixtures } = useFixtures({
    date: fixtureDate ?? '',
    leagueId: fixture.league_id ?? undefined,
    perPage: 100,
  });
  const leagueFixtures = useMemo(() => {
    if (!dayFixtures?.items) return [];
    return [...dayFixtures.items].sort((a, b) =>
      (a.starting_at ?? '').localeCompare(b.starting_at ?? ''),
    );
  }, [dayFixtures?.items]);
  const leagueFixturesRef = useRef(leagueFixtures);
  useEffect(() => {
    leagueFixturesRef.current = leagueFixtures;
  }, [leagueFixtures]);
  const fixtureIdRef = useRef(fixture.id);
  useEffect(() => {
    fixtureIdRef.current = fixture.id;
  }, [fixture.id]);
  const onChangeRef = useRef(onChangeFixture);
  useEffect(() => {
    onChangeRef.current = onChangeFixture;
  }, [onChangeFixture]);

  const swipeResponder = useRef(
    PanResponder.create({
      onMoveShouldSetPanResponder: (_, g) =>
        Math.abs(g.dx) > Math.abs(g.dy) * SWIPE_DOMINANCE &&
        Math.abs(g.dx) > SWIPE_RECOGNITION_THRESHOLD,
      onPanResponderRelease: (_, g) => {
        if (Math.abs(g.dx) < SWIPE_TRIGGER_DISTANCE) return;
        const list = leagueFixturesRef.current;
        if (list.length < 2) return;
        const idx = list.findIndex((f) => f.id === fixtureIdRef.current);
        if (idx < 0) return;
        const handler = onChangeRef.current;
        if (!handler) return;
        if (g.dx > 0 && idx > 0) handler(list[idx - 1]);
        else if (g.dx < 0 && idx < list.length - 1) handler(list[idx + 1]);
      },
    }),
  ).current;

  const leagueIds = useMemo(
    () => (fixture.league_id ? [fixture.league_id] : []),
    [fixture.league_id],
  );
  const { lookup: leagueLookup } = useLeagueLookup(leagueIds);
  const league = fixture.league_id ? leagueLookup.get(fixture.league_id) : undefined;

  const countryIds = useMemo(
    () => (league?.country_id != null ? [league.country_id] : []),
    [league?.country_id],
  );
  const { lookup: countryLookup } = useCountryLookup(countryIds);
  const country = league?.country_id
    ? countryLookup.get(league.country_id)
    : undefined;

  const hasEvents = (events.data?.length ?? 0) > 0;

  return (
    <View
      // Swipe handlers only matter once locked (otherwise pointerEvents on
      // the backdrop is "none" anyway and the gesture never reaches us),
      // but attaching them unconditionally keeps the JSX clean.
      {...swipeResponder.panHandlers}
      style={[
        styles.card,
        {
          backgroundColor: c.surface,
          borderColor: c.border,
          // Locked card sits at full opacity — it's now a "real" overlay,
          // not a transient peek; making it dim while interactive feels
          // wrong.
          opacity: locked ? 1 : 0.92,
        },
      ]}>
      {locked ? (
        <Pressable
          onPress={onClose}
          hitSlop={12}
          accessibilityRole="button"
          style={({ pressed }) => [
            styles.closeBtn,
            {
              backgroundColor: pressed ? c.brandSoft : c.surface,
              borderColor: c.border,
            },
          ]}>
          <MaterialCommunityIcons name="close" size={18} color={c.text} />
        </Pressable>
      ) : null}

      <View style={styles.heroWrap}>
        <FixtureDetailHero
          fixture={fixture}
          league={league}
          country={country}
          events={events.data}
        />
      </View>

      {hasEvents ? (
        <ScrollView
          style={styles.timelineScroll}
          contentContainerStyle={styles.timelineContent}
          // Only scroll when locked — during the active hold the user's
          // finger is still on the source row, and grabbing scroll here
          // would cancel the press-out gesture.
          scrollEnabled={locked}
          showsVerticalScrollIndicator={locked}>
          <EventTimelineCard events={events.data!} />
        </ScrollView>
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  // Faded backdrop sitting over the home list. Slightly darker than nothing
  // so the card stands out without "covering" the screen — the peek should
  // feel like a hover preview, not a modal takeover.
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.35)',
    justifyContent: 'flex-start',
    paddingTop: 64,
    paddingHorizontal: 12,
    paddingBottom: 24,
  },
  card: {
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
    flexShrink: 1,
    shadowColor: '#000',
    shadowOpacity: 0.18,
    shadowRadius: 12,
    shadowOffset: { width: 0, height: 6 },
    elevation: 6,
  },
  heroWrap: {
    // Hero is a fixed band at the top; the timeline below scrolls.
  },
  timelineScroll: {
    flexShrink: 1,
  },
  timelineContent: {
    paddingBottom: 8,
  },
  closeBtn: {
    position: 'absolute',
    top: 8,
    right: 8,
    width: 32,
    height: 32,
    borderRadius: 16,
    borderWidth: StyleSheet.hairlineWidth,
    alignItems: 'center',
    justifyContent: 'center',
    zIndex: 2,
  },
});
