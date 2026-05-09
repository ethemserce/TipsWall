import { useRef, useState } from 'react';
import {
  type GestureResponderEvent,
  StyleSheet,
  View,
  type ViewStyle,
} from 'react-native';

import { useTheme } from '@/src/lib/useTheme';

interface SliderProps {
  min: number;
  max: number;
  step?: number;
  value: number;
  onChange: (next: number) => void;
  /** Optional accent color for the active track + knob. */
  color?: string;
  style?: ViewStyle;
}

const KNOB_SIZE = 18;
const TRACK_HEIGHT = 4;

/**
 * Lightweight slider — touch-to-position + drag, no native module needed.
 * Kept intentionally minimal; if we need precise scrubbing later we can
 * swap in @react-native-community/slider, but the API stays the same.
 */
export function Slider({
  min,
  max,
  step = 1,
  value,
  onChange,
  color,
  style,
}: SliderProps) {
  const c = useTheme();
  const [width, setWidth] = useState(0);
  const lastValue = useRef(value);

  const accent = color ?? c.brand;
  const ratio = max === min ? 0 : (value - min) / (max - min);
  const clampedRatio = Math.max(0, Math.min(1, ratio));

  const update = (touchX: number) => {
    if (width <= 0) return;
    const r = Math.max(0, Math.min(1, touchX / width));
    const raw = min + r * (max - min);
    const stepped = Math.round(raw / step) * step;
    const next = Math.max(min, Math.min(max, stepped));
    if (next !== lastValue.current) {
      lastValue.current = next;
      onChange(next);
    }
  };

  const handle = (e: GestureResponderEvent) => update(e.nativeEvent.locationX);

  return (
    <View
      onLayout={(e) => setWidth(e.nativeEvent.layout.width)}
      onStartShouldSetResponder={() => true}
      onMoveShouldSetResponder={() => true}
      onResponderTerminationRequest={() => false}
      onResponderGrant={handle}
      onResponderMove={handle}
      style={[styles.touchArea, style]}>
      <View style={[styles.track, { backgroundColor: c.border }]} />
      <View
        style={[
          styles.fill,
          {
            backgroundColor: accent,
            width: `${clampedRatio * 100}%`,
          },
        ]}
      />
      <View
        style={[
          styles.knob,
          {
            backgroundColor: accent,
            // Position the knob centred on the value point. Use percentage so
            // it tracks layout changes without remeasuring on every render.
            left: `${clampedRatio * 100}%`,
            marginLeft: -KNOB_SIZE / 2,
            borderColor: c.surfaceElevated,
          },
        ]}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  touchArea: {
    height: 32,
    justifyContent: 'center',
  },
  track: {
    height: TRACK_HEIGHT,
    borderRadius: TRACK_HEIGHT,
  },
  fill: {
    position: 'absolute',
    left: 0,
    height: TRACK_HEIGHT,
    borderRadius: TRACK_HEIGHT,
  },
  knob: {
    position: 'absolute',
    top: (32 - KNOB_SIZE) / 2,
    width: KNOB_SIZE,
    height: KNOB_SIZE,
    borderRadius: KNOB_SIZE / 2,
    borderWidth: 2,
    elevation: 2,
    shadowColor: '#000',
    shadowOpacity: 0.15,
    shadowRadius: 2,
    shadowOffset: { width: 0, height: 1 },
  },
});
