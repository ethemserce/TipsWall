import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useEffect, useRef, useState } from 'react';
import { Animated, Easing, Pressable, StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { subscribeToasts, type Toast } from '@/src/lib/toasts';
import { useTheme } from '@/src/lib/useTheme';

const AUTO_DISMISS_MS = 4500;
const MAX_VISIBLE = 3;
const ENTER_MS = 220;
const EXIT_MS = 180;

/**
 * Global, foreground-only notification surface. Sits above all screens at
 * the root layout. Each toast slides in from the top with a fade and the
 * stack auto-dismisses oldest-first.
 */
export function ToastHost() {
  const [items, setItems] = useState<Toast[]>([]);
  const [exiting, setExiting] = useState<Set<string>>(new Set());

  useEffect(() => {
    return subscribeToasts((toast) => {
      setItems((prev) => {
        const next = [...prev, toast];
        return next.length > MAX_VISIBLE
          ? next.slice(next.length - MAX_VISIBLE)
          : next;
      });
      setTimeout(() => dismiss(toast.id), AUTO_DISMISS_MS);
    });
  }, []);

  const dismiss = (id: string) => {
    setExiting((prev) => {
      const next = new Set(prev);
      next.add(id);
      return next;
    });
    setTimeout(() => {
      setItems((prev) => prev.filter((t) => t.id !== id));
      setExiting((prev) => {
        const next = new Set(prev);
        next.delete(id);
        return next;
      });
    }, EXIT_MS);
  };

  if (items.length === 0) return null;

  return (
    <SafeAreaView pointerEvents="box-none" style={styles.host} edges={['top']}>
      <View pointerEvents="box-none" style={styles.stack}>
        {items.map((t) => (
          <ToastItem
            key={t.id}
            toast={t}
            isExiting={exiting.has(t.id)}
            onDismiss={() => dismiss(t.id)}
          />
        ))}
      </View>
    </SafeAreaView>
  );
}

function ToastItem({
  toast,
  isExiting,
  onDismiss,
}: {
  toast: Toast;
  isExiting: boolean;
  onDismiss: () => void;
}) {
  const c = useTheme();
  const translateY = useRef(new Animated.Value(-12)).current;
  const opacity = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    Animated.parallel([
      Animated.timing(translateY, {
        toValue: 0,
        duration: ENTER_MS,
        easing: Easing.out(Easing.cubic),
        useNativeDriver: true,
      }),
      Animated.timing(opacity, {
        toValue: 1,
        duration: ENTER_MS,
        easing: Easing.out(Easing.cubic),
        useNativeDriver: true,
      }),
    ]).start();
  }, [translateY, opacity]);

  useEffect(() => {
    if (!isExiting) return;
    Animated.parallel([
      Animated.timing(translateY, {
        toValue: -8,
        duration: EXIT_MS,
        easing: Easing.in(Easing.cubic),
        useNativeDriver: true,
      }),
      Animated.timing(opacity, {
        toValue: 0,
        duration: EXIT_MS,
        easing: Easing.in(Easing.cubic),
        useNativeDriver: true,
      }),
    ]).start();
  }, [isExiting, translateY, opacity]);

  const accent =
    toast.kind === 'win'
      ? c.success
      : toast.kind === 'loss'
        ? c.danger
        : c.brand;
  const accentBg =
    toast.kind === 'win'
      ? c.successSoft
      : toast.kind === 'loss'
        ? c.dangerSoft
        : c.brandSoft;

  return (
    <Animated.View
      style={{ transform: [{ translateY }], opacity }}>
      <Pressable
        onPress={onDismiss}
        style={[
          styles.toast,
          c.shadowFloating,
          {
            backgroundColor: c.surfaceElevated,
            borderColor: accent,
          },
        ]}>
        <View style={[styles.iconWrap, { backgroundColor: accentBg }]}>
          <MaterialCommunityIcons
            name={
              toast.kind === 'win'
                ? 'check-circle'
                : toast.kind === 'loss'
                  ? 'close-circle'
                  : 'information'
            }
            size={20}
            color={accent}
          />
        </View>
        <View style={styles.body}>
          <ThemedText style={[styles.title, { color: c.text }]}>
            {toast.title}
          </ThemedText>
          {toast.body ? (
            <ThemedText
              style={[styles.subtitle, { color: c.textMuted }]}
              numberOfLines={2}>
              {toast.body}
            </ThemedText>
          ) : null}
        </View>
      </Pressable>
    </Animated.View>
  );
}

const styles = StyleSheet.create({
  host: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    zIndex: 1000,
  },
  stack: {
    paddingHorizontal: 12,
    paddingTop: 6,
    gap: 8,
  },
  toast: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderRadius: 14,
    borderLeftWidth: 4,
  },
  iconWrap: {
    width: 32,
    height: 32,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
  },
  body: {
    flex: 1,
    gap: 2,
  },
  title: {
    fontSize: 13,
    fontWeight: '700',
  },
  subtitle: {
    fontSize: 11,
    fontWeight: '500',
  },
});
