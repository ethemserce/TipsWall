import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Tabs } from 'expo-router';
import React from 'react';
import { useTranslation } from 'react-i18next';

import { HapticTab } from '@/components/haptic-tab';
import { useTheme } from '@/src/lib/useTheme';

export default function TabLayout() {
  const c = useTheme();
  const { t } = useTranslation();

  // The tabs only render the bottom-bar destinations now. Detail screens
  // (fixture, league) live one level up in the root stack so router.back()
  // pops back to the tab the user came from instead of cycling through
  // hidden tab routes.
  return (
    <Tabs
      screenOptions={{
        tabBarActiveTintColor: c.brand,
        tabBarInactiveTintColor: c.textMuted,
        headerShown: false,
        tabBarButton: HapticTab,
      }}>
      <Tabs.Screen
        name="index"
        options={{
          title: t('tabs.home'),
          tabBarIcon: ({ color, size }) => (
            <MaterialCommunityIcons name="home-variant" size={size ?? 24} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="analysis"
        options={{
          title: t('tabs.analysis'),
          tabBarIcon: ({ color, size }) => (
            <MaterialCommunityIcons name="chart-line" size={size ?? 24} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="leagues"
        options={{
          title: t('tabs.leagues'),
          tabBarIcon: ({ color, size }) => (
            <MaterialCommunityIcons name="trophy-outline" size={size ?? 24} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="coupons"
        options={{
          title: t('tabs.coupons'),
          tabBarIcon: ({ color, size }) => (
            <MaterialCommunityIcons name="basket" size={size ?? 24} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="settings"
        options={{
          title: t('tabs.settings'),
          tabBarIcon: ({ color, size }) => (
            <MaterialCommunityIcons name="cog-outline" size={size ?? 24} color={color} />
          ),
        }}
      />
      <Tabs.Screen name="winning" options={{ href: null }} />
      <Tabs.Screen name="hot" options={{ href: null }} />
      <Tabs.Screen name="earning" options={{ href: null }} />
    </Tabs>
  );
}
