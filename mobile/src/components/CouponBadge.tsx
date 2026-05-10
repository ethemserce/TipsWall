import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { CouponSheet } from '@/src/components/CouponSheet';
import { useCouponStore } from '@/src/lib/coupons/store';
import { useTheme } from '@/src/lib/useTheme';

/**
 * Floating "Tahmin" pill anchored above the bottom tab bar. Visible only
 * when the draft has at least one selection. Tap to open the sheet.
 */
export function CouponBadge() {
  const c = useTheme();
  const [open, setOpen] = useState(false);
  const draft = useCouponStore((s) => s.draft);

  if (draft.selections.length === 0) return null;

  return (
    <>
      <View pointerEvents="box-none" style={styles.wrap}>
        <Pressable
          onPress={() => setOpen(true)}
          style={({ pressed }) => [
            styles.pill,
            {
              backgroundColor: c.brand,
              opacity: pressed ? 0.85 : 1,
            },
          ]}>
          <MaterialCommunityIcons
            name="basket"
            size={18}
            color={c.textInverse}
          />
          <ThemedText style={[styles.text, { color: c.textInverse }]}>
            Tahmin · {draft.selections.length}
          </ThemedText>
        </Pressable>
      </View>
      <CouponSheet visible={open} onClose={() => setOpen(false)} />
    </>
  );
}

const styles = StyleSheet.create({
  wrap: {
    position: 'absolute',
    left: 0,
    right: 0,
    // Sit above the bottom tab bar (tab height ~ 49 on iOS, ~56 on Android,
    // plus a safe-area gap). Slightly raised so the shadow doesn't clip.
    bottom: 70,
    alignItems: 'center',
    zIndex: 50,
  },
  pill: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 999,
    elevation: 6,
    shadowColor: '#000',
    shadowOpacity: 0.18,
    shadowRadius: 8,
    shadowOffset: { width: 0, height: 4 },
  },
  text: {
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
});
