import { DarkTheme, DefaultTheme, ThemeProvider } from '@react-navigation/native';
import { QueryClientProvider } from '@tanstack/react-query';
import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import 'react-native-reanimated';
import { View } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { useColorScheme } from '@/hooks/use-color-scheme';
import { CouponBadge } from '@/src/components/CouponBadge';
import { LiveStatusBanner } from '@/src/components/LiveStatusBanner';
import { QuotaLimitModal } from '@/src/components/QuotaLimitModal';
import { ToastHost } from '@/src/components/ToastHost';
import { useAutoSettleSavedCoupons } from '@/src/hooks/useCouponSettlement';
import '@/src/lib/i18n';
import { initMonitoring } from '@/src/lib/monitoring';
import { queryClient } from '@/src/lib/queryClient';
import {
  resolveScheme,
  useThemeMode,
} from '@/src/lib/settings/settingsStore';

// Initialise Sentry once at module load — before any screen mounts. Cheap
// no-op when SENTRY_DSN isn't set or the package isn't installed.
initMonitoring();

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
  // Settlement runs at the root so toasts fire on every screen, including
  // fixture / league detail pushed on top of (tabs).
  useAutoSettleSavedCoupons();

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
        {/* Auth flow — root-level so users land back on whichever tab
            they were on when they opened login from Settings or hit a
            "Üye Ol" CTA on a card. */}
        <Stack.Screen name="auth/login" options={{ headerShown: false }} />
        <Stack.Screen name="auth/signup" options={{ headerShown: false }} />
        <Stack.Screen
          name="auth/forgot-password"
          options={{ headerShown: false }}
        />
      </Stack>
      <CouponBadge />
      <ToastHost />
      <QuotaLimitModal />
      <StatusBar style={statusBarStyle} />
    </View>
  );
}
