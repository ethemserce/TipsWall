import { StyleSheet, View } from 'react-native';
import Svg, { Circle } from 'react-native-svg';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

export interface CircularGaugeProps {
  /** Numeric value to render in the centre. */
  value: number | null;
  /** Maximum reference for the arc length (default 100). Values above are clamped. */
  max?: number;
  /** Filled arc colour. Defaults to brand green. */
  color?: string;
  /** Display text override; defaults to value formatted as number. */
  label?: string;
  /** Outer diameter in px. */
  size?: number;
  /** Stroke width of both rings. */
  strokeWidth?: number;
}

export function CircularGauge({
  value,
  max = 100,
  color,
  label,
  size = 38,
  strokeWidth = 3,
}: CircularGaugeProps) {
  const c = useTheme();
  const radius = (size - strokeWidth) / 2;
  const circumference = 2 * Math.PI * radius;
  const safeMax = max === 0 ? 1 : max;
  const progress =
    value == null ? 0 : Math.min(1, Math.max(0, Math.abs(value) / safeMax));
  const dashOffset = circumference * (1 - progress);
  const arcColor = color ?? c.brand;
  const display = label ?? (value == null ? '-' : formatValue(value));

  return (
    <View style={[styles.wrap, { width: size, height: size }]}>
      <Svg width={size} height={size}>
        <Circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          stroke={c.border}
          strokeWidth={strokeWidth}
          fill="transparent"
        />
        {value != null ? (
          <Circle
            cx={size / 2}
            cy={size / 2}
            r={radius}
            stroke={arcColor}
            strokeWidth={strokeWidth}
            fill="transparent"
            strokeDasharray={`${circumference}`}
            strokeDashoffset={`${dashOffset}`}
            strokeLinecap="round"
            transform={`rotate(-90 ${size / 2} ${size / 2})`}
          />
        ) : null}
      </Svg>
      <View style={styles.labelWrap} pointerEvents="none">
        <ThemedText style={[styles.labelText, { color: c.text }]} numberOfLines={1}>
          {display}
        </ThemedText>
      </View>
    </View>
  );
}

function formatValue(value: number): string {
  // Match the legacy app: signed, one decimal, no trailing zeros.
  const abs = Math.abs(value);
  const decimals = abs >= 100 ? 0 : abs >= 10 ? 1 : 2;
  return value.toFixed(decimals);
}

const styles = StyleSheet.create({
  wrap: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  labelWrap: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    alignItems: 'center',
    justifyContent: 'center',
  },
  labelText: {
    fontSize: 9,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
});
