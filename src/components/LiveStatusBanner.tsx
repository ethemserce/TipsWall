import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useEffect, useRef } from 'react';
import { Animated, Easing, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useLiveStatus } from '@/src/lib/liveConnection';
import { useTheme } from '@/src/lib/useTheme';

/**
 * Subtle banner under the tab bar when the SignalR connection is not in
 * the `connected` state. We don't want the user staring at stale data
 * thinking it's live; the banner makes the disconnect explicit.
 *
 * The `idle` state (no consumer has called ensureLiveConnected yet) is
 * silent — only `connecting` and `disconnected` show.
 */
export function LiveStatusBanner() {
  const c = useTheme();
  const status = useLiveStatus();
  const visible = status === 'connecting' || status === 'disconnected';
  const translate = useRef(new Animated.Value(visible ? 0 : -36)).current;

  useEffect(() => {
    Animated.timing(translate, {
      toValue: visible ? 0 : -36,
      duration: 200,
      easing: Easing.out(Easing.cubic),
      useNativeDriver: true,
    }).start();
  }, [visible, translate]);

  if (status === 'idle' || status === 'connected') {
    if (status !== 'idle' && !visible) return null;
  }

  const isReconnecting = status === 'connecting';
  const accent = isReconnecting ? c.warning : c.danger;
  const accentSoft = isReconnecting ? c.warningSoft : c.dangerSoft;
  const message = isReconnecting
    ? 'Canlı veriye yeniden bağlanılıyor…'
    : 'Canlı veri bağlantısı yok. Skorlar gecikebilir.';

  return (
    <Animated.View
      pointerEvents="none"
      style={[
        styles.banner,
        {
          transform: [{ translateY: translate }],
          backgroundColor: accentSoft,
          borderBottomColor: accent,
        },
      ]}>
      <MaterialCommunityIcons
        name={isReconnecting ? 'sync' : 'wifi-off'}
        size={14}
        color={accent}
      />
      <ThemedText style={[styles.text, { color: accent }]}>{message}</ThemedText>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  banner: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 14,
    paddingVertical: 6,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  text: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
});
