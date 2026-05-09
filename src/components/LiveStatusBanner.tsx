import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Animated, Easing, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useLiveStatus } from '@/src/lib/liveConnection';
import { useNetStatus } from '@/src/lib/netStatus';
import { useTheme } from '@/src/lib/useTheme';

interface BannerState {
  icon: 'wifi-off' | 'sync' | 'wifi-strength-1-alert';
  message: string;
  tone: 'warning' | 'danger';
}

/**
 * Single banner that covers two failure modes the user needs to see:
 *  - `offline`: device can't reach the internet at all (NetInfo).
 *  - `connecting/disconnected`: SignalR live channel is down even though
 *    the device might still have HTTP — live scores will lag.
 *
 * Offline takes priority: if there's no internet, the SignalR state is
 * meaningless. Only one banner is shown at a time.
 */
export function LiveStatusBanner() {
  const c = useTheme();
  const { t } = useTranslation();
  const liveStatus = useLiveStatus();
  const netStatus = useNetStatus();

  let state: BannerState | null = null;
  if (netStatus === 'offline') {
    state = {
      icon: 'wifi-strength-1-alert',
      message: t('common.offline'),
      tone: 'danger',
    };
  } else if (liveStatus === 'connecting') {
    state = {
      icon: 'sync',
      message: t('common.liveReconnecting'),
      tone: 'warning',
    };
  } else if (liveStatus === 'disconnected') {
    state = {
      icon: 'wifi-off',
      message: t('common.liveDisconnected'),
      tone: 'danger',
    };
  }

  const visible = state != null;
  const translate = useRef(new Animated.Value(visible ? 0 : -36)).current;

  useEffect(() => {
    Animated.timing(translate, {
      toValue: visible ? 0 : -36,
      duration: 200,
      easing: Easing.out(Easing.cubic),
      useNativeDriver: true,
    }).start();
  }, [visible, translate]);

  if (!state) return null;

  const accent = state.tone === 'warning' ? c.warning : c.danger;
  const accentSoft = state.tone === 'warning' ? c.warningSoft : c.dangerSoft;

  return (
    <Animated.View
      pointerEvents="none"
      accessible
      accessibilityRole="alert"
      accessibilityLabel={state.message}
      style={[
        styles.banner,
        {
          transform: [{ translateY: translate }],
          backgroundColor: accentSoft,
          borderBottomColor: accent,
        },
      ]}>
      <MaterialCommunityIcons name={state.icon} size={14} color={accent} />
      <ThemedText style={[styles.text, { color: accent }]}>{state.message}</ThemedText>
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
