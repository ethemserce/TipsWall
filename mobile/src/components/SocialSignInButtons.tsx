import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { GoogleSignin } from '@react-native-google-signin/google-signin';
import * as AppleAuthentication from 'expo-apple-authentication';
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

// Configure once per process. `configure` is synchronous + idempotent so
// stamping it at module load is safe; the platform-specific client ids
// come from google-services.json (Android) and GoogleService-Info.plist
// (iOS) which the @react-native-firebase/app plugin already wires in.
// `webClientId` is the audience for the id_token — backend
// /auth/social-signin verifies against this client id via Google's JWKS.
if (env.googleClientIdWeb.length > 0) {
  GoogleSignin.configure({
    webClientId: env.googleClientIdWeb,
    scopes: ['email', 'profile'],
    // Forces account picker even when only one Google account is on the
    // device — the user explicitly tapped "sign in", they shouldn't be
    // surprised by a silent sign-in to the last-used account.
    forceCodeForRefreshToken: false,
  });
}

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
// Native flow only needs the Web client id (used as the id_token audience).
// Per-platform client ids ship inside google-services.json / GoogleService-
// Info.plist that the Firebase config plugin already installs, so we no
// longer require the EXPO_PUBLIC_GOOGLE_CLIENT_ID_{IOS,ANDROID} env vars
// to be set. The button stays hidden if even the Web id is missing — at
// that point the backend wouldn't be able to verify the id_token anyway.
function hasGoogleOnThisPlatform(): boolean {
  return env.googleClientIdWeb.length > 0;
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

  const handleSignedIn = () => {
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
      handleSignedIn();
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
          onSuccess={() => handleSignedIn()}
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

// Uses the native Google Sign-In SDK (Credentials Manager on Android,
// GIDSignIn on iOS). No browser, no redirect URI, no intent filter —
// the OS shows Google's account chooser directly and hands an id_token
// back to the app via callback. This sidesteps the Expo Auth Session
// redirect dance that depended on a custom URI scheme + matching SHA-1
// being registered in the Google Cloud Console. Same audience model
// (webClientId) so the backend's /auth/social-signin endpoint still
// verifies the id_token against the same project.
function GoogleSignInButton({
  busy,
  disabled,
  onStart,
  onSettle,
  onSuccess,
}: GoogleSignInButtonProps) {
  const c = useTheme();
  const { t } = useTranslation();

  const handlePress = async () => {
    if (disabled) return;
    onStart();
    try {
      // Surfaces the "update Google Play services" dialog when the
      // device's Play services are stale instead of silently failing.
      await GoogleSignin.hasPlayServices({ showPlayServicesUpdateDialog: true });
      const response = await GoogleSignin.signIn();
      // User dismissed the picker — silent.
      if (response.type === 'cancelled') return;
      const idToken = response.data.idToken;
      if (!idToken) {
        notify({
          kind: 'loss',
          title: t('auth.social.errorTitle'),
          body: t('auth.social.errorGoogle'),
        });
        return;
      }
      await socialSignIn('google', idToken);
      onSuccess();
    } catch (err) {
      notify({
        kind: 'loss',
        title: t('auth.social.errorTitle'),
        body:
          err instanceof ApiClientError
            ? err.message
            : err instanceof Error && err.message
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
