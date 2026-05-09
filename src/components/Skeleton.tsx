import { useEffect, useRef } from 'react';
import { Animated, Easing, StyleSheet, View, type ViewStyle, type StyleProp } from 'react-native';

import { useTheme } from '@/src/lib/useTheme';

interface SkeletonProps {
  width?: number | `${number}%`;
  height?: number;
  borderRadius?: number;
  style?: StyleProp<ViewStyle>;
}

/**
 * One shimmer block. The animated opacity drives an "amber→muted→amber"
 * pulse on a faint surface tint. We use opacity + a fixed background
 * instead of a gradient sweep because Animated drives opacity natively
 * and we don't want to ship a gradient package just for skeletons.
 */
export function Skeleton({
  width,
  height = 14,
  borderRadius = 4,
  style,
}: SkeletonProps) {
  const c = useTheme();
  const opacity = useRef(new Animated.Value(0.4)).current;

  useEffect(() => {
    const loop = Animated.loop(
      Animated.sequence([
        Animated.timing(opacity, {
          toValue: 0.85,
          duration: 750,
          easing: Easing.inOut(Easing.ease),
          useNativeDriver: true,
        }),
        Animated.timing(opacity, {
          toValue: 0.4,
          duration: 750,
          easing: Easing.inOut(Easing.ease),
          useNativeDriver: true,
        }),
      ]),
    );
    loop.start();
    return () => loop.stop();
  }, [opacity]);

  return (
    <Animated.View
      // Shimmer is decorative — hide from screen readers.
      accessibilityElementsHidden
      importantForAccessibility="no"
      style={[
        {
          width: width as number | undefined,
          height,
          borderRadius,
          backgroundColor: c.borderSoft,
          opacity,
        },
        style,
      ]}
    />
  );
}

/**
 * Skeleton row that mimics a FixtureCard while the home list is loading.
 * One time block on the left, two team rows in the middle, no score —
 * matches the rendered card's silhouette so the layout doesn't jump on
 * data arrival.
 */
export function FixtureCardSkeleton() {
  return (
    <View style={skelStyles.fixtureRow}>
      <Skeleton width={32} height={12} />
      <View style={skelStyles.fixtureTeams}>
        <View style={skelStyles.teamRow}>
          <Skeleton width={18} height={18} borderRadius={9} />
          <Skeleton width={120} height={12} />
        </View>
        <View style={skelStyles.teamRow}>
          <Skeleton width={18} height={18} borderRadius={9} />
          <Skeleton width={92} height={12} />
        </View>
      </View>
    </View>
  );
}

/**
 * Section-shaped skeleton: one league header row + N fixture skeletons.
 */
export function LeagueSectionSkeleton({ rows = 3 }: { rows?: number }) {
  const c = useTheme();
  return (
    <View
      style={[
        skelStyles.section,
        {
          backgroundColor: c.surfaceElevated,
          borderColor: c.borderSoft,
        },
      ]}>
      <View style={skelStyles.sectionHeader}>
        <Skeleton width={32} height={32} borderRadius={16} />
        <View style={{ flex: 1, gap: 4 }}>
          <Skeleton width="60%" height={12} />
          <Skeleton width="35%" height={10} />
        </View>
        <Skeleton width={28} height={18} borderRadius={9} />
      </View>
      {Array.from({ length: rows }).map((_, i) => (
        <FixtureCardSkeleton key={i} />
      ))}
    </View>
  );
}

const skelStyles = StyleSheet.create({
  section: {
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
    marginBottom: 12,
  },
  sectionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 10,
    gap: 10,
  },
  fixtureRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 16,
    gap: 12,
  },
  fixtureTeams: {
    flex: 1,
    gap: 6,
  },
  teamRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
});
