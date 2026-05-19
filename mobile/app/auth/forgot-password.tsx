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
import { requestPasswordReset } from '@/src/api/auth';
import { ApiClientError } from '@/src/api/client';
import { useTheme } from '@/src/lib/useTheme';

export default function ForgotPasswordScreen() {
  const c = useTheme();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const [emailOrUsername, setEmailOrUsername] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const canSubmit = emailOrUsername.trim().length > 0 && !submitting && !sent;

  const handleSubmit = async () => {
    if (!canSubmit) return;
    setError(null);
    setSubmitting(true);
    try {
      await requestPasswordReset(emailOrUsername.trim());
      setSent(true);
    } catch (err) {
      const msg =
        err instanceof ApiClientError ? err.message : t('auth.errors.network');
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
          contentContainerStyle={[
            styles.scroll,
            { paddingBottom: insets.bottom + 32 },
          ]}
          keyboardShouldPersistTaps="handled">
          <ThemedText style={[styles.title, { color: c.text }]}>
            {t('auth.forgot.title')}
          </ThemedText>
          <ThemedText style={[styles.subtitle, { color: c.textMuted }]}>
            {t('auth.forgot.subtitle')}
          </ThemedText>

          <View style={styles.fieldGroup}>
            <ThemedText style={[styles.label, { color: c.textMuted }]}>
              {t('auth.fields.emailOrUsername')}
            </ThemedText>
            <TextInput
              value={emailOrUsername}
              onChangeText={setEmailOrUsername}
              autoCapitalize="none"
              autoCorrect={false}
              keyboardType="email-address"
              textContentType="username"
              returnKeyType="done"
              editable={!sent}
              onSubmitEditing={handleSubmit}
              placeholderTextColor={c.textMuted}
              placeholder="ornek@mail.com"
              style={[
                styles.input,
                { color: c.text, backgroundColor: c.surface, borderColor: c.borderSoft },
                sent && { opacity: 0.6 },
              ]}
            />
          </View>

          {error ? (
            <View style={[styles.errorBox, { backgroundColor: c.dangerSoft, borderColor: c.danger }]}>
              <MaterialCommunityIcons name="alert-circle" size={16} color={c.danger} />
              <ThemedText style={[styles.errorText, { color: c.danger }]}>
                {error}
              </ThemedText>
            </View>
          ) : null}

          {sent ? (
            <View style={[styles.successBox, { backgroundColor: c.successSoft, borderColor: c.success }]}>
              <MaterialCommunityIcons name="email-check" size={18} color={c.success} />
              <ThemedText style={[styles.successText, { color: c.success }]}>
                {t('auth.forgot.sent')}
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
                {t('auth.forgot.submit')}
              </ThemedText>
            )}
          </Pressable>

          <Pressable
            onPress={() => router.replace('/auth/login' as never)}
            hitSlop={6}
            style={styles.footerRow}>
            <ThemedText style={[styles.link, { color: c.brand }]}>
              {t('auth.forgot.backToLogin')}
            </ThemedText>
          </Pressable>
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
    lineHeight: 18,
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
  input: {
    fontSize: 15,
    paddingHorizontal: 14,
    paddingVertical: 12,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
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
  successBox: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
  },
  successText: {
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
    alignItems: 'center',
    marginTop: 16,
  },
  link: {
    fontSize: 13,
    fontWeight: '700',
  },
});
