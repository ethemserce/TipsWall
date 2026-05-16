import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { router } from 'expo-router';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  TextInput,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { signup } from '@/src/api/auth';
import { ApiClientError } from '@/src/api/client';
import { SocialSignInButtons } from '@/src/components/SocialSignInButtons';
import { getCouponCounts } from '@/src/lib/coupons/store';
import { notify } from '@/src/lib/toasts';
import { useTheme } from '@/src/lib/useTheme';

// Server enforces these too, but failing fast on-device skips a
// round-trip and gives the user clearer feedback.
const MIN_PASSWORD = 8;
const USERNAME_REGEX = /^[a-zA-Z0-9_]{3,32}$/;

export default function SignupScreen() {
  const c = useTheme();
  const { t } = useTranslation();
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const validate = (): string | null => {
    if (!USERNAME_REGEX.test(username.trim())) return t('auth.errors.username');
    if (!email.includes('@')) return t('auth.errors.email');
    if (password.length < MIN_PASSWORD) return t('auth.errors.passwordShort');
    return null;
  };

  const canSubmit =
    username.trim().length > 0 &&
    email.trim().length > 0 &&
    password.length > 0 &&
    !submitting;

  const handleSubmit = async () => {
    if (!canSubmit) return;
    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }
    setError(null);
    setSubmitting(true);
    // Snapshot local pick count BEFORE signup — used to surface a
    // "X tahminin hesabına bağlandı" toast so the user sees their
    // guest-mode work carrying over.
    const counts = getCouponCounts();
    const carryOver = counts.draft + counts.saved;
    try {
      await signup({
        username: username.trim(),
        email: email.trim(),
        password,
      });
      // Successful signup auto-logs the user in. Pop the auth stack.
      if (router.canGoBack()) router.back();
      else router.replace('/');
      if (carryOver > 0) {
        notify({
          kind: 'win',
          title: t('auth.signup.migrationToastTitle'),
          body: t('auth.signup.migrationToastBody', { count: carryOver }),
        });
      } else {
        notify({
          kind: 'info',
          title: t('auth.signup.welcomeToastTitle'),
          body: t('auth.signup.welcomeToastBody'),
        });
      }
    } catch (err) {
      const msg =
        err instanceof ApiClientError
          ? err.code === 'USERNAME_TAKEN'
            ? t('auth.errors.usernameTaken')
            : err.code === 'EMAIL_TAKEN'
              ? t('auth.errors.emailTaken')
              : err.message
          : t('auth.errors.network');
      setError(msg);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.headerRow}>
        <Pressable
          onPress={() => router.back()}
          hitSlop={12}
          style={({ pressed }) => [
            styles.backBtn,
            { backgroundColor: pressed ? c.brandSoft : 'transparent' },
          ]}>
          <MaterialCommunityIcons name="chevron-left" size={24} color={c.text} />
        </Pressable>
      </View>

      <KeyboardAvoidingView
        style={styles.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <ScrollView
          contentContainerStyle={styles.scroll}
          keyboardShouldPersistTaps="handled">
          <ThemedText style={[styles.title, { color: c.text }]}>
            {t('auth.signup.title')}
          </ThemedText>
          <ThemedText style={[styles.subtitle, { color: c.textMuted }]}>
            {t('auth.signup.subtitle')}
          </ThemedText>

          <View style={styles.fieldGroup}>
            <ThemedText style={[styles.label, { color: c.textMuted }]}>
              {t('auth.fields.username')}
            </ThemedText>
            <TextInput
              value={username}
              onChangeText={setUsername}
              autoCapitalize="none"
              autoCorrect={false}
              textContentType="username"
              returnKeyType="next"
              placeholderTextColor={c.textMuted}
              style={[
                styles.input,
                { color: c.text, backgroundColor: c.surface, borderColor: c.borderSoft },
              ]}
            />
            <ThemedText style={[styles.hint, { color: c.textMuted }]}>
              {t('auth.signup.usernameHint')}
            </ThemedText>
          </View>

          <View style={styles.fieldGroup}>
            <ThemedText style={[styles.label, { color: c.textMuted }]}>
              {t('auth.fields.email')}
            </ThemedText>
            <TextInput
              value={email}
              onChangeText={setEmail}
              autoCapitalize="none"
              autoCorrect={false}
              keyboardType="email-address"
              textContentType="emailAddress"
              returnKeyType="next"
              placeholderTextColor={c.textMuted}
              style={[
                styles.input,
                { color: c.text, backgroundColor: c.surface, borderColor: c.borderSoft },
              ]}
            />
          </View>

          <View style={styles.fieldGroup}>
            <ThemedText style={[styles.label, { color: c.textMuted }]}>
              {t('auth.fields.password')}
            </ThemedText>
            <View style={styles.passwordRow}>
              <TextInput
                value={password}
                onChangeText={setPassword}
                secureTextEntry={!showPassword}
                autoCapitalize="none"
                autoCorrect={false}
                textContentType="newPassword"
                returnKeyType="done"
                onSubmitEditing={handleSubmit}
                placeholderTextColor={c.textMuted}
                placeholder="••••••••"
                style={[
                  styles.input,
                  styles.passwordInput,
                  { color: c.text, backgroundColor: c.surface, borderColor: c.borderSoft },
                ]}
              />
              <Pressable
                onPress={() => setShowPassword((v) => !v)}
                hitSlop={8}
                style={styles.eyeBtn}>
                <MaterialCommunityIcons
                  name={showPassword ? 'eye-off' : 'eye'}
                  size={20}
                  color={c.textMuted}
                />
              </Pressable>
            </View>
            <ThemedText style={[styles.hint, { color: c.textMuted }]}>
              {t('auth.signup.passwordHint', { min: MIN_PASSWORD })}
            </ThemedText>
          </View>

          {error ? (
            <View style={[styles.errorBox, { backgroundColor: c.dangerSoft, borderColor: c.danger }]}>
              <MaterialCommunityIcons name="alert-circle" size={16} color={c.danger} />
              <ThemedText style={[styles.errorText, { color: c.danger }]}>
                {error}
              </ThemedText>
            </View>
          ) : null}

          <Pressable
            onPress={handleSubmit}
            disabled={!canSubmit}
            style={({ pressed }) => [
              styles.primaryBtn,
              {
                backgroundColor: canSubmit ? c.brand : c.border,
                opacity: pressed && canSubmit ? 0.85 : 1,
              },
            ]}>
            {submitting ? (
              <ActivityIndicator color={c.textInverse} />
            ) : (
              <ThemedText style={[styles.primaryBtnText, { color: c.textInverse }]}>
                {t('auth.signup.submit')}
              </ThemedText>
            )}
          </Pressable>

          <SocialSignInButtons />

          <ThemedText style={[styles.legal, { color: c.textMuted }]}>
            {t('auth.signup.legal')}
          </ThemedText>

          <View style={styles.footerRow}>
            <ThemedText style={[styles.footerText, { color: c.textMuted }]}>
              {t('auth.signup.haveAccount')}{' '}
            </ThemedText>
            <Pressable
              onPress={() => router.replace('/auth/login' as never)}
              hitSlop={6}>
              <ThemedText style={[styles.link, { color: c.brand }]}>
                {t('auth.signup.loginLink')}
              </ThemedText>
            </Pressable>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  headerRow: {
    flexDirection: 'row',
    paddingHorizontal: 8,
    paddingTop: 8,
  },
  backBtn: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  scroll: {
    paddingHorizontal: 20,
    paddingTop: 16,
    paddingBottom: 32,
    gap: 14,
  },
  title: {
    fontSize: 24,
    fontWeight: '800',
    letterSpacing: 0.3,
    // Explicit lineHeight so descenders (g, y) on a heavy 24px glyph
    // don't bleed into the subtitle below. RN's auto line-height for
    // bold text comes in a touch tighter than the font's bounding box.
    lineHeight: 30,
    marginBottom: 4,
  },
  subtitle: {
    fontSize: 13,
    marginBottom: 8,
  },
  fieldGroup: {
    gap: 6,
  },
  label: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.4,
  },
  hint: {
    fontSize: 11,
  },
  input: {
    fontSize: 15,
    paddingHorizontal: 14,
    paddingVertical: 12,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
  },
  passwordRow: {
    position: 'relative',
  },
  passwordInput: {
    paddingRight: 44,
  },
  eyeBtn: {
    position: 'absolute',
    right: 10,
    top: 0,
    bottom: 0,
    justifyContent: 'center',
    paddingHorizontal: 6,
  },
  errorBox: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
  },
  errorText: {
    flex: 1,
    fontSize: 12,
    fontWeight: '600',
  },
  primaryBtn: {
    paddingVertical: 14,
    borderRadius: 10,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 8,
  },
  primaryBtnText: {
    fontSize: 14,
    fontWeight: '800',
    letterSpacing: 0.5,
  },
  legal: {
    fontSize: 11,
    textAlign: 'center',
    lineHeight: 15,
    marginTop: 8,
  },
  footerRow: {
    flexDirection: 'row',
    justifyContent: 'center',
    marginTop: 8,
  },
  footerText: {
    fontSize: 13,
  },
  link: {
    fontSize: 13,
    fontWeight: '700',
  },
});
