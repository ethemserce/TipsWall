import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Alert,
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { deleteAccount, fetchMe, logout, requestEmailVerification } from '@/src/api/auth';
import { AppBrand } from '@/src/components/AppBrand';
import { analytics, consentStore } from '@/src/lib/analytics';
import { useEmailVerified, useTier } from '@/src/lib/auth/authStore';
import { refreshAccessToken } from '@/src/lib/auth/refreshLock';
import { useMarketPreferences } from '@/src/hooks/useMarketPreferences';
import {
  setLanguageMode,
  setOddsHidden,
  setThemeMode,
  useSettings,
  type LanguageMode,
  type ThemeMode,
} from '@/src/lib/settings/settingsStore';
import { useTheme } from '@/src/lib/useTheme';

const THEME_MODES: { mode: ThemeMode; icon: 'cellphone' | 'white-balance-sunny' | 'weather-night' }[] = [
  { mode: 'system', icon: 'cellphone' },
  { mode: 'light', icon: 'white-balance-sunny' },
  { mode: 'dark', icon: 'weather-night' },
];

const LANGUAGE_MODES: { mode: LanguageMode }[] = [
  { mode: 'system' },
  { mode: 'en' },
  { mode: 'tr' },
];

export function SettingsScreen() {
  const c = useTheme();
  const { t } = useTranslation();
  const { themeMode, languageMode, oddsHidden } = useSettings();
  const tier = useTier();
  const jwtEmailVerified = useEmailVerified();
  const marketPrefs = useMarketPreferences();
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);
  // Mirror the consent store with a local snapshot so toggling redraws.
  const [analyticsConsent, setAnalyticsConsent] = useState(consentStore.getState());
  useEffect(() => consentStore.subscribe(() => setAnalyticsConsent(consentStore.getState())), []);
  const analyticsOn = analyticsConsent === 'granted';

  // Email verification banner state. JWT claim is the fast path (offline-
  // capable, no extra round trip), but it lags up to 15 min behind the
  // DB after the user clicks the verify link. fetchMe() hits the DB so
  // a freshly verified account hides the banner immediately. We refresh
  // the token after a confirmed flip so other gates that read the JWT
  // see the new state too.
  const [dbEmailVerified, setDbEmailVerified] = useState<boolean | null>(null);
  const [verifySending, setVerifySending] = useState(false);
  const [verifyStatus, setVerifyStatus] = useState<'idle' | 'sent' | 'error'>('idle');
  useEffect(() => {
    if (tier === 'guest') return;
    let cancelled = false;
    fetchMe()
      .then((me) => {
        if (!cancelled) setDbEmailVerified(me.email_verified);
      })
      .catch(() => {
        // Network down — fall back to JWT claim.
      });
    return () => {
      cancelled = true;
    };
  }, [tier]);
  const emailVerified =
    dbEmailVerified != null ? dbEmailVerified : jwtEmailVerified;

  const handleResendVerification = async () => {
    setVerifySending(true);
    setVerifyStatus('idle');
    try {
      await requestEmailVerification();
      setVerifyStatus('sent');
    } catch {
      setVerifyStatus('error');
    } finally {
      setVerifySending(false);
    }
  };

  const handleVerifiedRefresh = async () => {
    setVerifySending(true);
    try {
      // Force token rotation so the new JWT carries email_verified=true.
      // The DB check on /me already flipped — this propagates to gates
      // that only see the JWT claim.
      await refreshAccessToken();
      const me = await fetchMe();
      setDbEmailVerified(me.email_verified);
    } catch {
      // Stay on whatever state was last known.
    } finally {
      setVerifySending(false);
    }
  };

  const handleLogout = async () => {
    try {
      await logout();
    } catch {
      // logout() already clears tokens locally regardless — nothing to
      // surface to the user.
    }
  };

  const handleDelete = async () => {
    setDeleting(true);
    try {
      await deleteAccount();
      setDeleteOpen(false);
    } catch (err) {
      Alert.alert(
        t('auth.errors.network'),
        err instanceof Error ? err.message : t('common.somethingWentWrong'),
      );
    } finally {
      setDeleting(false);
    }
  };

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.headerRow}>
        <AppBrand />
      </View>

      <ScrollView contentContainerStyle={styles.scroll}>
        {/* Hesap — guest sees Giriş/Üye Ol; registered sees Çıkış + Sil */}
        <SectionHeader label={t('settings.account.header')} />
        <View
          style={[
            styles.card,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          {tier === 'guest' ? (
            <>
              <View style={styles.rowHeader}>
                <ThemedText style={[styles.rowTitle, { color: c.text }]}>
                  {t('settings.account.guestTitle')}
                </ThemedText>
                <ThemedText style={[styles.rowHint, { color: c.textMuted }]}>
                  {t('settings.account.guestHint')}
                </ThemedText>
              </View>
              <View style={styles.accountActions}>
                <Pressable
                  onPress={() => router.push('/auth/signup' as never)}
                  style={({ pressed }) => [
                    styles.actionBtnPrimary,
                    { backgroundColor: c.brand, opacity: pressed ? 0.85 : 1 },
                  ]}>
                  <ThemedText style={[styles.actionTextPrimary, { color: c.textInverse }]}>
                    {t('settings.account.signupBtn')}
                  </ThemedText>
                </Pressable>
                <Pressable
                  onPress={() => router.push('/auth/login' as never)}
                  style={({ pressed }) => [
                    styles.actionBtnSecondary,
                    { borderColor: c.brand, opacity: pressed ? 0.7 : 1 },
                  ]}>
                  <ThemedText style={[styles.actionTextSecondary, { color: c.brand }]}>
                    {t('settings.account.loginBtn')}
                  </ThemedText>
                </Pressable>
              </View>
            </>
          ) : (
            <>
              <View style={styles.rowHeader}>
                <ThemedText style={[styles.rowTitle, { color: c.text }]}>
                  {tier === 'premium'
                    ? t('settings.account.premiumTitle')
                    : t('settings.account.memberTitle')}
                </ThemedText>
                <ThemedText style={[styles.rowHint, { color: c.textMuted }]}>
                  {tier === 'premium'
                    ? t('settings.account.premiumHint')
                    : t('settings.account.memberHint')}
                </ThemedText>
              </View>
              {!emailVerified ? (
                <View
                  style={[
                    styles.verifyBanner,
                    {
                      backgroundColor: c.warningSoft ?? c.brandSoft,
                      borderColor: c.warning ?? c.brand,
                    },
                  ]}>
                  <View style={styles.verifyTextBlock}>
                    <ThemedText
                      style={[styles.verifyTitle, { color: c.text }]}>
                      {t('settings.verifyEmail.title', {
                        defaultValue: 'Email adresini onayla',
                      })}
                    </ThemedText>
                    <ThemedText
                      style={[styles.verifyHint, { color: c.textMuted }]}>
                      {verifyStatus === 'sent'
                        ? t('settings.verifyEmail.sentHint', {
                            defaultValue:
                              'Gelen kutuna bir doğrulama linki gönderdik. Linke tıkladıktan sonra "Onayladım"a bas.',
                          })
                        : verifyStatus === 'error'
                          ? t('settings.verifyEmail.errorHint', {
                              defaultValue: 'Mail gönderilemedi. Sonra tekrar dene.',
                            })
                          : t('settings.verifyEmail.hint', {
                              defaultValue:
                                'Hesap güvenliği için mailini onayla. Onaylamadan kupon kaydedemezsin.',
                            })}
                    </ThemedText>
                  </View>
                  <View style={styles.verifyActions}>
                    <Pressable
                      onPress={handleResendVerification}
                      disabled={verifySending}
                      style={({ pressed }) => [
                        styles.verifyBtnPrimary,
                        {
                          backgroundColor: c.brand,
                          opacity: pressed || verifySending ? 0.7 : 1,
                        },
                      ]}>
                      <ThemedText
                        style={[styles.verifyBtnText, { color: c.textInverse }]}>
                        {verifyStatus === 'sent'
                          ? t('settings.verifyEmail.resendBtn', {
                              defaultValue: 'Tekrar gönder',
                            })
                          : t('settings.verifyEmail.sendBtn', {
                              defaultValue: 'Mail gönder',
                            })}
                      </ThemedText>
                    </Pressable>
                    {verifyStatus === 'sent' ? (
                      <Pressable
                        onPress={handleVerifiedRefresh}
                        disabled={verifySending}
                        style={({ pressed }) => [
                          styles.verifyBtnSecondary,
                          {
                            borderColor: c.brand,
                            opacity: pressed || verifySending ? 0.7 : 1,
                          },
                        ]}>
                        <ThemedText
                          style={[styles.verifyBtnText, { color: c.brand }]}>
                          {t('settings.verifyEmail.refreshBtn', {
                            defaultValue: 'Onayladım',
                          })}
                        </ThemedText>
                      </Pressable>
                    ) : null}
                  </View>
                </View>
              ) : null}
              <Pressable
                onPress={handleLogout}
                style={({ pressed }) => [
                  styles.actionBtnSecondary,
                  { borderColor: c.border, opacity: pressed ? 0.7 : 1 },
                ]}>
                <MaterialCommunityIcons name="logout" size={16} color={c.textMuted} />
                <ThemedText style={[styles.actionTextSecondary, { color: c.textMuted }]}>
                  {t('settings.account.logoutBtn')}
                </ThemedText>
              </Pressable>
              <Pressable
                onPress={() => setDeleteOpen(true)}
                style={({ pressed }) => [
                  styles.deleteRow,
                  { opacity: pressed ? 0.6 : 1 },
                ]}>
                <ThemedText style={[styles.deleteText, { color: c.danger }]}>
                  {t('settings.account.deleteAccount')}
                </ThemedText>
              </Pressable>
            </>
          )}
        </View>

        {/* Appearance — theme switcher */}
        <SectionHeader label={t('settings.appearance.header')} />
        <View
          style={[
            styles.card,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <View style={styles.rowHeader}>
            <ThemedText style={[styles.rowTitle, { color: c.text }]}>
              {t('settings.appearance.theme')}
            </ThemedText>
            <ThemedText style={[styles.rowHint, { color: c.textMuted }]}>
              {t('settings.appearance.themeHint')}
            </ThemedText>
          </View>
          <View style={[styles.segment, { backgroundColor: c.bg, borderColor: c.borderSoft }]}>
            {THEME_MODES.map(({ mode, icon }) => {
              const active = themeMode === mode;
              return (
                <Pressable
                  key={mode}
                  onPress={() => setThemeMode(mode)}
                  style={[
                    styles.segmentBtn,
                    active && { backgroundColor: c.brand },
                  ]}
                  accessibilityRole="button"
                  accessibilityState={{ selected: active }}>
                  <MaterialCommunityIcons
                    name={icon}
                    size={16}
                    color={active ? c.textInverse : c.textMuted}
                  />
                  <ThemedText
                    style={[
                      styles.segmentText,
                      { color: active ? c.textInverse : c.textMuted },
                    ]}>
                    {t(`settings.appearance.modes.${mode}`)}
                  </ThemedText>
                </Pressable>
              );
            })}
          </View>
        </View>

        {/* Language */}
        <SectionHeader label={t('settings.language.header')} />
        <View
          style={[
            styles.card,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <View style={styles.rowHeader}>
            <ThemedText style={[styles.rowTitle, { color: c.text }]}>
              {t('settings.language.label')}
            </ThemedText>
            <ThemedText style={[styles.rowHint, { color: c.textMuted }]}>
              {t('settings.language.hint')}
            </ThemedText>
          </View>
          <View style={[styles.segment, { backgroundColor: c.bg, borderColor: c.borderSoft }]}>
            {LANGUAGE_MODES.map(({ mode }) => {
              const active = languageMode === mode;
              return (
                <Pressable
                  key={mode}
                  onPress={() => setLanguageMode(mode)}
                  style={[
                    styles.segmentBtn,
                    active && { backgroundColor: c.brand },
                  ]}
                  accessibilityRole="button"
                  accessibilityState={{ selected: active }}>
                  <ThemedText
                    style={[
                      styles.segmentText,
                      { color: active ? c.textInverse : c.textMuted },
                    ]}>
                    {t(`settings.language.modes.${mode}`)}
                  </ThemedText>
                </Pressable>
              );
            })}
          </View>
        </View>

        {/* Display — opt-in surfaces. Default is the no-betting framing
            (no odd values anywhere); users who want to see the raw odds
            can flip this on. State lives in settingsStore so every odds
            card reads the same flag. */}
        <SectionHeader label={t('settings.display.header')} />
        <Pressable
          onPress={() => setOddsHidden(!oddsHidden)}
          style={[
            styles.card,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}
          accessibilityRole="switch"
          accessibilityState={{ checked: !oddsHidden }}>
          <View style={styles.toggleRow}>
            <View style={styles.toggleText}>
              <ThemedText style={[styles.rowTitle, { color: c.text }]}>
                {t('settings.display.showOdds.label')}
              </ThemedText>
              <ThemedText style={[styles.rowHint, { color: c.textMuted }]}>
                {t('settings.display.showOdds.hint')}
              </ThemedText>
            </View>
            <View
              style={[
                styles.toggle,
                {
                  backgroundColor: !oddsHidden ? c.brand : c.borderSoft,
                  borderColor: !oddsHidden ? c.brand : c.border,
                },
              ]}>
              <View
                style={[
                  styles.toggleKnob,
                  {
                    backgroundColor: c.textInverse,
                    transform: [{ translateX: !oddsHidden ? 14 : 0 }],
                  },
                ]}
              />
            </View>
          </View>
        </Pressable>

        {/* Analytics — KVKK opt-in toggle. Mirrors the consent state from
            the AnalyticsConsentBanner; flipping here is the canonical
            way to opt back out (or back in) after the banner was
            dismissed. */}
        <Pressable
          onPress={() => {
            if (analyticsOn) void analytics.denyConsent();
            else void analytics.grantConsent();
          }}
          style={[
            styles.card,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}
          accessibilityRole="switch"
          accessibilityState={{ checked: analyticsOn }}>
          <View style={styles.toggleRow}>
            <View style={styles.toggleText}>
              <ThemedText style={[styles.rowTitle, { color: c.text }]}>
                {t('settings.analytics.label', { defaultValue: 'Kullanım verisi paylaş' })}
              </ThemedText>
              <ThemedText style={[styles.rowHint, { color: c.textMuted }]}>
                {t('settings.analytics.hint', {
                  defaultValue:
                    'Anonim kullanım istatistikleri toplanır. Kişisel veri içermez; istediğinde kapatabilirsin.',
                })}
              </ThemedText>
            </View>
            <View
              style={[
                styles.toggle,
                {
                  backgroundColor: analyticsOn ? c.brand : c.borderSoft,
                  borderColor: analyticsOn ? c.brand : c.border,
                },
              ]}>
              <View
                style={[
                  styles.toggleKnob,
                  {
                    backgroundColor: c.textInverse,
                    transform: [{ translateX: analyticsOn ? 14 : 0 }],
                  },
                ]}
              />
            </View>
          </View>
        </Pressable>

        <Pressable
          onPress={() => router.push('/market-preferences' as never)}
          style={[
            styles.card,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <View style={styles.toggleRow}>
            <View style={styles.toggleText}>
              <ThemedText style={[styles.rowTitle, { color: c.text }]}>
                {t('settings.marketPrefs.label', {
                  defaultValue: 'Favori Marketler',
                })}
              </ThemedText>
              <ThemedText style={[styles.rowHint, { color: c.textMuted }]}>
                {marketPrefs.marketIds.length > 0
                  ? t('settings.marketPrefs.hintSelected', {
                      defaultValue: '{{count}}/{{cap}} market seçili',
                      count: marketPrefs.marketIds.length,
                      cap: marketPrefs.cap,
                    })
                  : t('settings.marketPrefs.hintEmpty', {
                      defaultValue:
                        'Boş bırakırsan her market gelir. Seçim yaparsan sadece onlar görünür.',
                    })}
              </ThemedText>
            </View>
            <MaterialCommunityIcons name="chevron-right" size={20} color={c.textMuted} />
          </View>
        </Pressable>

        {/* About — positioning disclaimer. The app is a prediction
            tracker, not a betting client; this section is the canonical
            place where that statement lives. */}
        <SectionHeader label={t('settings.about.header')} />
        <View
          style={[
            styles.card,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <View style={styles.rowHeader}>
            <ThemedText style={[styles.rowTitle, { color: c.text }]}>
              {t('settings.about.title')}
            </ThemedText>
            <ThemedText style={[styles.rowHint, { color: c.textMuted }]}>
              {t('settings.about.disclaimer')}
            </ThemedText>
          </View>
        </View>
      </ScrollView>

      <Modal
        visible={deleteOpen}
        transparent
        animationType="fade"
        onRequestClose={() => !deleting && setDeleteOpen(false)}>
        <Pressable
          style={styles.modalBackdrop}
          onPress={() => !deleting && setDeleteOpen(false)}>
          <Pressable
            onPress={(e) => e.stopPropagation()}
            style={[
              styles.modalSheet,
              { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
            ]}>
            <ThemedText style={[styles.modalTitle, { color: c.text }]}>
              {t('settings.account.deleteConfirmTitle')}
            </ThemedText>
            <ThemedText style={[styles.modalBody, { color: c.textMuted }]}>
              {t('settings.account.deleteConfirmBody')}
            </ThemedText>
            <View style={styles.modalActions}>
              <Pressable
                onPress={() => !deleting && setDeleteOpen(false)}
                disabled={deleting}
                style={({ pressed }) => [
                  styles.modalBtn,
                  { borderColor: c.border, opacity: pressed ? 0.7 : 1 },
                ]}>
                <ThemedText style={[styles.modalBtnText, { color: c.text }]}>
                  {t('common.cancel').toLocaleUpperCase('tr-TR')}
                </ThemedText>
              </Pressable>
              <Pressable
                onPress={handleDelete}
                disabled={deleting}
                style={({ pressed }) => [
                  styles.modalBtn,
                  {
                    backgroundColor: c.danger,
                    borderColor: c.danger,
                    opacity: deleting ? 0.6 : pressed ? 0.85 : 1,
                  },
                ]}>
                <ThemedText style={[styles.modalBtnText, { color: c.textInverse }]}>
                  {t('settings.account.deleteConfirm').toLocaleUpperCase('tr-TR')}
                </ThemedText>
              </Pressable>
            </View>
          </Pressable>
        </Pressable>
      </Modal>
    </SafeAreaView>
  );
}

function SectionHeader({ label }: { label: string }) {
  const c = useTheme();
  return (
    <ThemedText style={[styles.sectionHeader, { color: c.textMuted }]}>
      {label.toLocaleUpperCase('tr-TR')}
    </ThemedText>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  headerRow: {
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 4,
  },
  scroll: {
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 32,
    gap: 8,
  },
  sectionHeader: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.6,
    marginTop: 16,
    marginBottom: 6,
    paddingHorizontal: 4,
  },
  card: {
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 14,
    gap: 12,
  },
  rowHeader: {
    gap: 2,
  },
  rowTitle: {
    fontSize: 15,
    fontWeight: '600',
  },
  rowHint: {
    fontSize: 12,
    lineHeight: 16,
  },
  toggleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  toggleText: {
    flex: 1,
    gap: 2,
  },
  toggle: {
    width: 36,
    height: 22,
    borderRadius: 11,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 2,
    justifyContent: 'center',
  },
  toggleKnob: {
    width: 16,
    height: 16,
    borderRadius: 8,
  },
  segment: {
    flexDirection: 'row',
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 3,
    gap: 2,
  },
  segmentBtn: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingVertical: 8,
    borderRadius: 999,
  },
  segmentText: {
    fontSize: 12,
    fontWeight: '600',
  },
  accountActions: {
    flexDirection: 'row',
    gap: 8,
  },
  actionBtnPrimary: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingVertical: 11,
    borderRadius: 10,
  },
  actionBtnSecondary: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingVertical: 10,
    borderRadius: 10,
    borderWidth: 1,
  },
  actionTextPrimary: {
    fontSize: 13,
    fontWeight: '800',
    letterSpacing: 0.4,
  },
  actionTextSecondary: {
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0.4,
  },
  deleteRow: {
    paddingTop: 6,
    paddingBottom: 2,
    alignItems: 'center',
  },
  deleteText: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.4,
  },
  verifyBanner: {
    marginTop: 12,
    padding: 12,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
    gap: 10,
  },
  verifyTextBlock: {
    gap: 4,
  },
  verifyTitle: {
    fontSize: 13,
    fontWeight: '700',
  },
  verifyHint: {
    fontSize: 11,
    lineHeight: 15,
  },
  verifyActions: {
    flexDirection: 'row',
    gap: 8,
  },
  verifyBtnPrimary: {
    flex: 1,
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 8,
    alignItems: 'center',
  },
  verifyBtnSecondary: {
    flex: 1,
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 8,
    alignItems: 'center',
    borderWidth: StyleSheet.hairlineWidth,
  },
  verifyBtnText: {
    fontSize: 12,
    fontWeight: '700',
  },
  modalBackdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.55)',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  modalSheet: {
    width: '100%',
    maxWidth: 360,
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 16,
    gap: 8,
  },
  modalTitle: {
    fontSize: 16,
    fontWeight: '700',
  },
  modalBody: {
    fontSize: 13,
    lineHeight: 18,
  },
  modalActions: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    gap: 8,
    paddingTop: 8,
  },
  modalBtn: {
    paddingHorizontal: 14,
    paddingVertical: 9,
    borderRadius: 8,
    borderWidth: 1,
  },
  modalBtnText: {
    fontSize: 12,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
});
