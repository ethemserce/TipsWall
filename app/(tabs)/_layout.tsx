import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Tabs } from 'expo-router';
import React from 'react';
import { useTranslation } from 'react-i18next';
import { View } from 'react-native';

import { HapticTab } from '@/components/haptic-tab';
import { CouponBadge } from '@/src/components/CouponBadge';
import { useAutoSettleSavedCoupons } from '@/src/hooks/useCouponSettlement';
import { useTheme } from '@/src/lib/useTheme';

export default function TabLayout() {
  const c = useTheme();
  const { t } = useTranslation();
  // Run settlement at the tab-shell level so toasts fire even when the user
  // is on Home / Analysis / Detail and not specifically on Kuponlarım.
  useAutoSettleSavedCoupons();

  // CouponBadge is rendered as a sibling of Tabs so the floating pill
  // hovers above every tab content (analysis, home, coupons, fixture detail).
  // pointerEvents: 'box-none' on its wrapper lets touches pass through except
  // on the pill itself.
  return (
    <View style={{ flex: 1 }}>
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
          name="coupons"
          options={{
            title: t('tabs.coupons'),
            tabBarIcon: ({ color, size }) => (
              <MaterialCommunityIcons name="basket" size={size ?? 24} color={color} />
            ),
          }}
        />
        <Tabs.Screen name="winning" options={{ href: null }} />
        <Tabs.Screen name="hot" options={{ href: null }} />
        <Tabs.Screen name="earning" options={{ href: null }} />
        <Tabs.Screen name="explore" options={{ href: null }} />
        <Tabs.Screen name="fixture/[id]" options={{ href: null }} />
      </Tabs>
      <CouponBadge />
    </View>
  );
}
