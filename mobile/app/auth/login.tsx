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
import { SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { login } from '@/src/api/auth';
import { ApiClientError } from '@/src/api/client';
import { SocialSignInButtons } from '@/src/components/SocialSignInButtons';
import { useTheme } from '@/src/lib/useTheme';

export default function LoginScreen() {
  const c = useTheme();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const [usernameOrEmail, setUsernameOrEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const canSubmit =
    usernameOrEmail.trim().length > 0 && password.length > 0 && !submitting;

  const handleSubmit = async () => {
    if (!canSubmit) return;
    setError(null);
    setSubmitting(true);
    try {
      await login(usernameOrEmail.trim(), password);
      // Token store fires its listeners synchronously → useTier() flips
      // to 'free' on the previous screen. Pop back to wherever the user
      // came from (Settings most likely).
      if (router.canGoBack()) router.back();
      else router.replace('/');
    } catch (err) {
      const msg =
        err instanceof ApiClientError
          ? err.code === 'unauthorized'
            ? t('auth.errors.badCredentials')
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
          <MaterialCommunityIcons
            name="chevron-left"
            size={24}
            color={c.text}
          />
        </Pressable>
      </View>

      <KeyboardAvoidingView
        style={styles.flex}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        <ScrollView
          contentContainerStyle={[
            styles.scroll,
            { paddingBottom: insets.bottom + 32 },
          ]}
          keyboardShouldPersistTaps="handled">
          <ThemedText style={[styles.title, { color: c.text }]}>
            {t('auth.login.title')}
          </ThemedText>
          <ThemedText style={[styles.subtitle, { color: c.textMuted }]}>
            {t('auth.login.subtitle')}
          </ThemedText>

          <View style={styles.fieldGroup}>
            <ThemedText style={[styles.label, { color: c.textMuted }]}>
              {t('auth.fields.emailOrUsername')}
            </ThemedText>
            <TextInput
              value={usernameOrEmail}
              onChangeText={setUsernameOrEmail}
              autoCapitalize="none"
              autoCorrect={false}
              keyboardType="email-address"
              textContentType="username"
              returnKeyType="next"
              placeholderTextColor={c.textMuted}
              style={[
                styles.input,
                { color: c.text, backgroundColor: c.surface, borderColor: c.borderSoft },
              ]}
            />
          </View>

          <View style={styles.fieldGroup}>
            <View style={styles.labelRow}>
              <ThemedText style={[styles.label, { color: c.textMuted }]}>
                {t('auth.fields.password')}
              </ThemedText>
              <Pressable
                onPress={() => router.push('/auth/forgot-password' as never)}
                hitSlop={6}>
                <ThemedText style={[styles.linkSmall, { color: c.brand }]}>
                  {t('auth.login.forgotPassword')}
                </ThemedText>
              </Pressable>
            </View>
            <View style={styles.passwordRow}>
              <TextInput
                value={password}
                onChangeText={setPassword}
                secureTextEntry={!showPassword}
                autoCapitalize="none"
                autoCorrect={false}
                textContentType="password"
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
                {t('auth.login.submit')}
              </ThemedText>
            )}
          </Pressable>

          <SocialSignInButtons />

          <View style={styles.footerRow}>
            <ThemedText style={[styles.footerText, { color: c.textMuted }]}>
              {t('auth.login.noAccount')}{' '}
            </ThemedText>
            <Pressable
              onPress={() => router.replace('/auth/signup' as never)}
              hitSlop={6}>
              <ThemedText style={[styles.link, { color: c.brand }]}>
                {t('auth.login.signupLink')}
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
    gap: 14,
  },
  title: {
    fontSize: 24,
    fontWeight: '800',
    letterSpacing: 0.3,
  },
  subtitle: {
    fontSize: 13,
    marginBottom: 8,
  },
  fieldGroup: {
    gap: 6,
  },
  labelRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'baseline',
  },
  label: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.4,
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
  footerRow: {
    flexDirection: 'row',
    justifyContent: 'center',
    marginTop: 16,
  },
  footerText: {
    fontSize: 13,
  },
  link: {
    fontSize: 13,
    fontWeight: '700',
  },
  linkSmall: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
});
