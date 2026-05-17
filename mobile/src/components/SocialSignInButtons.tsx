import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import * as AppleAuthentication from 'expo-apple-authentication';
import * as Google from 'expo-auth-session/providers/google';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ActivityIndicator, Platform, Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { socialSignIn } from '@/src/api/auth';
import { ApiClientError } from '@/src/api/client';
import { getCouponCounts } from '@/src/lib/coupons/store';
import { env } from '@/src/lib/env';
import { notify } from '@/src/lib/toasts';
import { useTheme } from '@/src/lib/useTheme';

/**
 * Apple Sign-In + Google Sign-In buttons, shared by login and signup
 * screens. Both providers return an id_token JWT that the backend
 * verifies against the provider's JWKS. The mobile side just funnels
 * the token to /auth/social-signin and stores the tokens that come
 * back.
 *
 * Buttons render only when their provider is configured + available:
 *  - Apple: iOS only (Sign in with Apple isn't available elsewhere).
 *  - Google: any platform, but requires at least one EXPO_PUBLIC_
 *    GOOGLE_CLIENT_ID_* env var.
 *
 * Failure UX is intentionally light — user can fall back to email/
 * password without seeing a scary error if Apple/Google pops a
 * cancel sheet.
 */
// Google's hook validates the per-platform client id at call time — calling
// it on Android without `androidClientId` throws before we ever get to the
// `showGoogle` gate. Resolving the requirement per Platform.OS up-front
// also matches the library's intent: a Web-only id is useless on a native
// device. When the platform id is missing we skip the entire Google child
// so the hook never fires.
function hasGoogleOnThisPlatform(): boolean {
  // All three client ids are required by useIdTokenAuthRequest on
  // production builds (see the comment above the hook call below).
  // The Web id alone is not enough because Expo defaults to the
  // app's custom scheme on native, which the WEB client rejects.
  // Show the button only when every id is set so we don't fire the
  // hook with missing config.
  if (env.googleClientIdWeb.length === 0) return false;
  if (Platform.OS === 'ios') return env.googleClientIdIos.length > 0;
  if (Platform.OS === 'android') return env.googleClientIdAndroid.length > 0;
  return true;
}

export function SocialSignInButtons() {
  const c = useTheme();
  const { t } = useTranslation();
  const [busy, setBusy] = useState<'apple' | 'google' | null>(null);
  const [appleAvailable, setAppleAvailable] = useState(false);

  useEffect(() => {
    if (Platform.OS !== 'ios') return;
    AppleAuthentication.isAvailableAsync().then(setAppleAvailable).catch(() => {});
  }, []);

  const handleSignedIn = (provider: 'apple' | 'google') => {
    // Same "your guest picks moved" toast as the email-password
    // signup path — same value prop applies.
    const counts = getCouponCounts();
    const carryOver = counts.draft + counts.saved;
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
    if (router.canGoBack()) router.back();
    else router.replace('/');
  };

  const handleApple = async () => {
    if (busy) return;
    setBusy('apple');
    try {
      const credential = await AppleAuthentication.signInAsync({
        requestedScopes: [
          AppleAuthentication.AppleAuthenticationScope.EMAIL,
          AppleAuthentication.AppleAuthenticationScope.FULL_NAME,
        ],
      });
      if (!credential.identityToken) {
        throw new Error('Apple returned no identityToken.');
      }
      await socialSignIn('apple', credential.identityToken);
      handleSignedIn('apple');
    } catch (err: unknown) {
      if (
        err &&
        typeof err === 'object' &&
        'code' in err &&
        (err as { code?: string }).code === 'ERR_REQUEST_CANCELED'
      ) {
        // User backed out of the Apple sheet — silent.
        return;
      }
      notify({
        kind: 'loss',
        title: t('auth.social.errorTitle'),
        body:
          err instanceof ApiClientError
            ? err.message
            : t('auth.social.errorApple'),
      });
    } finally {
      setBusy(null);
    }
  };

  const showApple = Platform.OS === 'ios' && appleAvailable;
  const showGoogle = hasGoogleOnThisPlatform();
  if (!showApple && !showGoogle) return null;

  return (
    <View style={styles.wrap}>
      <View style={styles.divider}>
        <View style={[styles.line, { backgroundColor: c.borderSoft }]} />
        <ThemedText style={[styles.dividerText, { color: c.textMuted }]}>
          {t('auth.social.dividerLabel')}
        </ThemedText>
        <View style={[styles.line, { backgroundColor: c.borderSoft }]} />
      </View>

      {showApple ? (
        <Pressable
          onPress={handleApple}
          disabled={busy != null}
          style={({ pressed }) => [
            styles.btn,
            { backgroundColor: c.text, opacity: pressed ? 0.85 : 1 },
          ]}>
          {busy === 'apple' ? (
            <ActivityIndicator color={c.bg} />
          ) : (
            <>
              <MaterialCommunityIcons name="apple" size={20} color={c.bg} />
              <ThemedText style={[styles.btnText, { color: c.bg }]}>
                {t('auth.social.appleBtn')}
              </ThemedText>
            </>
          )}
        </Pressable>
      ) : null}

      {showGoogle ? (
        <GoogleSignInButton
          busy={busy === 'google'}
          disabled={busy != null}
          onStart={() => setBusy('google')}
          onSettle={() => setBusy(null)}
          onSuccess={() => handleSignedIn('google')}
        />
      ) : null}
    </View>
  );
}

interface GoogleSignInButtonProps {
  busy: boolean;
  disabled: boolean;
  onStart: () => void;
  onSettle: () => void;
  onSuccess: () => void;
}

// Mounted only when the device's platform-specific Google client id is
// present (see `hasGoogleOnThisPlatform`). That way the `useIdTokenAuthRequest`
// hook never fires with missing required props.
function GoogleSignInButton({
  busy,
  disabled,
  onStart,
  onSettle,
  onSuccess,
}: GoogleSignInButtonProps) {
  const c = useTheme();
  const { t } = useTranslation();
  // All three client IDs are required for production builds on Expo
  // SDK 54+:
  //
  //   * iosClientId + androidClientId let useIdTokenAuthRequest pick the
  //     reverse-domain redirect URI (com.googleusercontent.apps.NNN:/
  //     oauthredirect) that Google's native client types accept.
  //   * clientId (Web) is still required so Expo can fill the standard
  //     PKCE/OAuth params (audience etc.) for token exchange.
  //
  // Two earlier attempts failed:
  //   * Removing ios/android client ids and shipping only the web id
  //     produced "Custom scheme URIs are not allowed for 'WEB' client
  //     type" — Expo defaulted to the app's preoddsmobile:// custom
  //     scheme which the WEB OAuth client rejects. Expo's old
  //     auth.expo.io HTTPS proxy was removed in SDK 49 so we can't
  //     route the web client through a hosted https redirect.
  //   * Keeping all three ids but with the Android client's "Enable
  //     Custom URI scheme" toggle OFF produced "Custom URI scheme is
  //     not enabled for your Android client".
  //
  // Working configuration:
  //   * All three client ids passed below.
  //   * Google Cloud Console → Android client → enable "Custom URI
  //     scheme" (a one-time toggle on the Android client page).
  const [, , promptGoogle] = Google.useIdTokenAuthRequest({
    iosClientId: env.googleClientIdIos || undefined,
    androidClientId: env.googleClientIdAndroid || undefined,
    clientId: env.googleClientIdWeb || undefined,
  });

  const handlePress = async () => {
    if (disabled) return;
    onStart();
    try {
      const result = await promptGoogle();
      if (result?.type !== 'success' || !result.params.id_token) return;
      await socialSignIn('google', result.params.id_token);
      onSuccess();
    } catch (err) {
      notify({
        kind: 'loss',
        title: t('auth.social.errorTitle'),
        body:
          err instanceof ApiClientError
            ? err.message
            : t('auth.social.errorGoogle'),
      });
    } finally {
      onSettle();
    }
  };

  return (
    <Pressable
      onPress={handlePress}
      disabled={disabled}
      style={({ pressed }) => [
        styles.btn,
        {
          backgroundColor: c.surfaceElevated,
          borderColor: c.border,
          borderWidth: StyleSheet.hairlineWidth,
          opacity: pressed ? 0.7 : 1,
        },
      ]}>
      {busy ? (
        <ActivityIndicator color={c.text} />
      ) : (
        <>
          <MaterialCommunityIcons name="google" size={18} color={c.text} />
          <ThemedText style={[styles.btnText, { color: c.text }]}>
            {t('auth.social.googleBtn')}
          </ThemedText>
        </>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  wrap: {
    gap: 10,
    paddingTop: 4,
  },
  divider: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingVertical: 4,
  },
  line: {
    flex: 1,
    height: StyleSheet.hairlineWidth,
  },
  dividerText: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.6,
  },
  btn: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    paddingVertical: 12,
    borderRadius: 10,
  },
  btnText: {
    fontSize: 14,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
});
