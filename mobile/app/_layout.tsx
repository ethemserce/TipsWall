import { DarkTheme, DefaultTheme, ThemeProvider } from '@react-navigation/native';
import { QueryClientProvider } from '@tanstack/react-query';
import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import 'react-native-reanimated';
import { useEffect } from 'react';
import { View } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { useColorScheme } from '@/hooks/use-color-scheme';
import { AgeGateModal } from '@/src/components/AgeGateModal';
import { AnalyticsConsentBanner } from '@/src/components/AnalyticsConsentBanner';
import { CouponBadge } from '@/src/components/CouponBadge';
import { LiveStatusBanner } from '@/src/components/LiveStatusBanner';
import { QuotaLimitModal } from '@/src/components/QuotaLimitModal';
import { ToastHost } from '@/src/components/ToastHost';
import { useAppFocusBridge } from '@/src/hooks/useAppFocusBridge';
import { useAutoSettleSavedCoupons } from '@/src/hooks/useCouponSettlement';
import { useTrackScreens } from '@/src/hooks/useTrackScreens';
import { analytics } from '@/src/lib/analytics';
import { getAuthSnapshot, subscribeAuth } from '@/src/lib/auth/authStore';
import '@/src/lib/i18n';
import { marketPreferencesStore } from '@/src/lib/marketPreferences/store';
import { initMonitoring } from '@/src/lib/monitoring';
import { queryClient } from '@/src/lib/queryClient';
import {
  resolveScheme,
  useThemeMode,
} from '@/src/lib/settings/settingsStore';

// Initialise Sentry once at module load — before any screen mounts. Cheap
// no-op when SENTRY_DSN isn't set or the package isn't installed.
initMonitoring();
// Initialise Google Mobile Ads SDK once. Sets the strictest content
// filter (maxAdContentRating='G') so the inventory excludes gambling,
// alcohol, dating and other adult categories — important for TipsWall's
// "no betting framing" posture in TR.
//
// UMP (User Messaging Platform) consent flow runs first: in regulated
// regions (EU/UK/CH + Turkey when configured in AdMob console) the
// user sees Google's standard "personalised vs non-personalised ads"
// choice screen. KVKK Article 5(2)(f) "legitimate interest" doesn't
// cover personalised advertising — explicit consent is required, and
// the UMP form is the channel. AdsConsent.gatherConsent() is a one-
// shot helper that calls requestInfoUpdate, evaluates whether a form
// is required, and presents it. Outside regulated regions it's a
// no-op so the call is cheap to make unconditionally.
//
// On any UMP error we still initialise ads — they'll serve non-
// personalised by default, which is the privacy-safe fallback.
void import('react-native-google-mobile-ads')
  .then(async ({ default: mobileAds, AdsConsent, MaxAdContentRating }) => {
    try {
      await AdsConsent.gatherConsent();
    } catch {
      // Consent flow failure shouldn't block the rest of init.
    }
    await mobileAds().setRequestConfiguration({
      maxAdContentRating: MaxAdContentRating.G,
      tagForChildDirectedTreatment: false,
      tagForUnderAgeOfConsent: false,
    });
    await mobileAds().initialize();
  })
  .catch(() => {
    // Initialisation failure is silent — the BannerAd component
    // surfaces its own load errors and the rest of the app keeps
    // working without ads.
  });

export const unstable_settings = {
  anchor: '(tabs)',
};

export default function RootLayout() {
  const deviceScheme = useColorScheme();
  const mode = useThemeMode();
  const scheme = resolveScheme(mode, deviceScheme);
  // Pin StatusBar to the inverse of the active scheme so the system bar
  // contrasts with our background — 'auto' would track the device theme
  // even when the user has forced light/dark via Settings.
  const statusBarStyle = scheme === 'dark' ? 'light' : 'dark';

  return (
    <QueryClientProvider client={queryClient}>
      <SafeAreaProvider>
        <ThemeProvider value={scheme === 'dark' ? DarkTheme : DefaultTheme}>
          <RootShell statusBarStyle={statusBarStyle} />
        </ThemeProvider>
      </SafeAreaProvider>
    </QueryClientProvider>
  );
}

// Hoisted into its own component so the auto-settlement hook (which lives
// inside react-query's provider) can use the QueryClientProvider context
// — RootLayout itself sits above the provider.
function RootShell({ statusBarStyle }: { statusBarStyle: 'light' | 'dark' }) {
  // App-focus bridge: wires React Native AppState into TanStack Query's
  // focusManager + nudges SignalR back to life when the app resumes.
  // Without this, live-minute can sit frozen if the user backgrounds
  // the app long enough for SignalR's retry budget to run out.
  useAppFocusBridge();
  // Settlement runs at the root so toasts fire on every screen, including
  // fixture / league detail pushed on top of (tabs).
  useAutoSettleSavedCoupons();
  // Hydrate the market-preferences store once at boot. Best-effort:
  // logged-in users get a backend round-trip, guests stay on local
  // storage. The hook'd-in screens subscribe; nothing else to wire.
  //
  // Also subscribe to auth-token changes so a login / signup / logout
  // mid-session re-pulls the tier-aware cap (3 → 10 → 30) and the
  // server-saved selection without needing the user to relaunch the
  // app. Ethem hit this on 2026-05-18: signed up, market cap was
  // still stuck at the guest 3 until he killed and reopened the app.
  useEffect(() => {
    void marketPreferencesStore.hydrate();
    let lastToken = getAuthSnapshot().accessToken;
    const unsubscribe = subscribeAuth(() => {
      const nextToken = getAuthSnapshot().accessToken;
      if (nextToken === lastToken) return;
      lastToken = nextToken;
      // Token transition (login / logout / refresh path) — re-hydrate
      // with force so cap + tier + market list all refresh against the
      // new session.
      void marketPreferencesStore.hydrate(true);
    });
    return () => unsubscribe();
  }, []);

  // Analytics bootstrap: hydrate the persisted consent choice, then push
  // it down to the Firebase SDK. Subsequent toggles in Settings call
  // analytics.syncCollectionFromConsent() directly. screen_view events
  // ride the path change via useTrackScreens — the consent gate inside
  // each call keeps them silent until the user opts in.
  useEffect(() => {
    void (async () => {
      await analytics.hydrate();
      await analytics.syncCollectionFromConsent();
    })();
  }, []);
  useTrackScreens();

  return (
    <View style={{ flex: 1 }}>
      <LiveStatusBanner />
      <Stack>
        <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
        {/* Detail routes live in the root stack — pushed on top of (tabs)
            so router.back() returns to whichever tab the user came from
            (Home / Leagues / Analysis), not the default index tab. */}
        <Stack.Screen name="fixture/[id]" options={{ headerShown: false }} />
        <Stack.Screen name="league/[id]" options={{ headerShown: false }} />
        <Stack.Screen name="team/[id]" options={{ headerShown: false }} />
        <Stack.Screen name="player/[id]" options={{ headerShown: false }} />
        {/* Auth flow — root-level so users land back on whichever tab
            they were on when they opened login from Settings or hit a
            "Üye Ol" CTA on a card. */}
        <Stack.Screen name="auth/login" options={{ headerShown: false }} />
        <Stack.Screen name="auth/signup" options={{ headerShown: false }} />
        <Stack.Screen
          name="auth/forgot-password"
          options={{ headerShown: false }}
        />
        <Stack.Screen name="market-preferences" options={{ headerShown: false }} />
        {/* Legal docs (terms / privacy / kvkk) — own SafeAreaView header
            inside the route, so suppress the stack header to avoid the
            default "legal/[topic]" route-name label. */}
        <Stack.Screen name="legal/[topic]" options={{ headerShown: false }} />
      </Stack>
      <CouponBadge />
      <ToastHost />
      <QuotaLimitModal />
      <AnalyticsConsentBanner />
      {/* Age gate is rendered last so its Modal layers above all other
          overlays — first-launch block until the user confirms 18+. */}
      <AgeGateModal />
      <StatusBar style={statusBarStyle} />
    </View>
  );
}
