import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Tabs } from 'expo-router';
import React from 'react';
import { useTranslation } from 'react-i18next';

import { HapticTab } from '@/components/haptic-tab';
import { useTheme } from '@/src/lib/useTheme';

export default function TabLayout() {
  const c = useTheme();
  const { t } = useTranslation();

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
        name="winning"
        options={{
          title: t('tabs.winning'),
          tabBarIcon: ({ color, size }) => (
            <MaterialCommunityIcons name="trophy" size={size ?? 24} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="hot"
        options={{
          title: t('tabs.hot'),
          tabBarIcon: ({ color, size }) => (
            <MaterialCommunityIcons name="fire" size={size ?? 24} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="earning"
        options={{
          title: t('tabs.earning'),
          tabBarIcon: ({ color, size }) => (
            <MaterialCommunityIcons name="chart-line" size={size ?? 24} color={color} />
          ),
        }}
      />
      <Tabs.Screen
        name="explore"
        options={{ href: null }}
      />
    </Tabs>
  );
}
