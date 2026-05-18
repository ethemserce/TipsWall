import AsyncStorage from '@react-native-async-storage/async-storage';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { BackHandler, Modal, Platform, Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

// One-shot 18+ + ToS / Privacy gate shown on first launch. Persisted in
// AsyncStorage so it never reappears once accepted. Declining quits the
// app on Android (BackHandler.exitApp) and leaves the modal blocking on
// iOS (the user has to background the app manually — iOS apps can't
// programmatically terminate).
//
// Why a gate: TipsWall surfaces betting-adjacent statistics. The
// product is positioned as analysis-only but the Turkish regulatory
// climate around 7258 takes a wide view; an explicit 18+ confirmation
// + visible Terms / Privacy acceptance at first launch hardens the
// "we never targeted minors / we disclosed the framing" posture.
const STORAGE_KEY = 'tipswall.age_gate.accepted_v1';

export function AgeGateModal() {
  const c = useTheme();
  const { t } = useTranslation();
  // null = still loading from AsyncStorage; true = accepted; false = needs prompt
  const [accepted, setAccepted] = useState<boolean | null>(null);

  useEffect(() => {
    AsyncStorage.getItem(STORAGE_KEY)
      .then((v) => setAccepted(v === 'true'))
      .catch(() => setAccepted(false));
  }, []);

  if (accepted === null || accepted) return null;

  const handleAccept = async () => {
    try {
      await AsyncStorage.setItem(STORAGE_KEY, 'true');
    } catch {
      // Even if persistence fails, dismiss the gate for this session —
      // the user did consent, the storage write is best-effort.
    }
    setAccepted(true);
  };

  const handleDecline = () => {
    // Android can exit the app cleanly. iOS apps cannot programmatically
    // quit (Apple HIG forbids it); on iOS we leave the modal blocking
    // and the user has to background the app via the home gesture.
    if (Platform.OS === 'android') {
      BackHandler.exitApp();
    }
  };

  return (
    <Modal visible transparent animationType="fade" onRequestClose={() => { /* swallow back button */ }}>
      <View style={styles.backdrop}>
        <View
          style={[
            styles.sheet,
            { backgroundColor: c.surfaceElevated, borderColor: c.border },
          ]}>
          <ThemedText style={[styles.title, { color: c.text }]}>
            {t('ageGate.title')}
          </ThemedText>
          <ThemedText style={[styles.body, { color: c.textMuted }]}>
            {t('ageGate.body')}
          </ThemedText>
          <View style={styles.legalLinks}>
            <Pressable onPress={() => router.push('/legal/terms' as never)} hitSlop={6}>
              <ThemedText style={[styles.link, { color: c.brand }]}>
                {t('settings.legal.terms')}
              </ThemedText>
            </Pressable>
            <ThemedText style={{ color: c.textMuted }}> · </ThemedText>
            <Pressable onPress={() => router.push('/legal/privacy' as never)} hitSlop={6}>
              <ThemedText style={[styles.link, { color: c.brand }]}>
                {t('settings.legal.privacy')}
              </ThemedText>
            </Pressable>
          </View>
          <View style={styles.actions}>
            <Pressable
              onPress={handleDecline}
              style={({ pressed }) => [
                styles.btn,
                styles.btnGhost,
                { borderColor: c.border },
                pressed && { opacity: 0.7 },
              ]}>
              <ThemedText style={[styles.btnText, { color: c.text }]}>
                {t('ageGate.decline')}
              </ThemedText>
            </Pressable>
            <Pressable
              onPress={handleAccept}
              style={({ pressed }) => [
                styles.btn,
                { backgroundColor: c.brand },
                pressed && { opacity: 0.85 },
              ]}>
              <ThemedText style={[styles.btnText, { color: c.textInverse }]}>
                {t('ageGate.confirm')}
              </ThemedText>
            </Pressable>
          </View>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.65)',
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 24,
  },
  sheet: {
    width: '100%',
    maxWidth: 360,
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 20,
    gap: 12,
  },
  title: {
    fontSize: 17,
    fontWeight: '800',
    letterSpacing: 0.2,
  },
  body: {
    fontSize: 13,
    lineHeight: 19,
  },
  legalLinks: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
  },
  link: {
    fontSize: 12,
    fontWeight: '700',
    textDecorationLine: 'underline',
  },
  actions: {
    flexDirection: 'row',
    gap: 10,
    marginTop: 6,
  },
  btn: {
    flex: 1,
    paddingVertical: 12,
    borderRadius: 10,
    alignItems: 'center',
    justifyContent: 'center',
  },
  btnGhost: {
    borderWidth: StyleSheet.hairlineWidth,
  },
  btnText: {
    fontSize: 13,
    fontWeight: '800',
    letterSpacing: 0.3,
  },
});
