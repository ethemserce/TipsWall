import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useTranslation } from 'react-i18next';
import { ActivityIndicator, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ApiClientError } from '@/src/api/client';
import { useTheme } from '@/src/lib/useTheme';

type IconName = keyof typeof MaterialCommunityIcons.glyphMap;

export function TabLoading() {
  const c = useTheme();
  return (
    <View style={styles.empty}>
      <ActivityIndicator color={c.brand} />
    </View>
  );
}

interface TabErrorProps {
  error: unknown;
}

export function TabError({ error }: TabErrorProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const status = error instanceof ApiClientError ? error.status : undefined;
  const url = error instanceof ApiClientError ? error.url : undefined;
  const message =
    error instanceof Error ? error.message : t('common.somethingWentWrong');

  return (
    <View style={styles.empty}>
      <View style={[styles.iconCircle, { backgroundColor: c.dangerSoft }]}>
        <MaterialCommunityIcons
          name="alert-circle-outline"
          size={28}
          color={c.danger}
        />
      </View>
      <ThemedText style={[styles.title, { color: c.text }]}>
        {t('common.couldNotLoad')}{status ? ` (HTTP ${status})` : ''}
      </ThemedText>
      <ThemedText style={[styles.message, { color: c.textMuted }]} numberOfLines={3}>
        {message}
      </ThemedText>
      {url ? (
        <ThemedText style={[styles.url, { color: c.textMuted }]} numberOfLines={2}>
          {url}
        </ThemedText>
      ) : null}
    </View>
  );
}

interface TabEmptyProps {
  message: string;
  /** Optional icon for the brand-tinted circle. Defaults to a generic
   *  "no results" mark when caller doesn't pass one. */
  icon?: IconName;
}

export function TabEmpty({ message, icon = 'tray-remove' }: TabEmptyProps) {
  const c = useTheme();
  return (
    <View style={styles.empty}>
      <View style={[styles.iconCircle, { backgroundColor: c.brandSoft }]}>
        <MaterialCommunityIcons name={icon} size={28} color={c.brand} />
      </View>
      <ThemedText
        style={[styles.message, { color: c.textMuted }]}
        numberOfLines={3}>
        {message}
      </ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  empty: {
    paddingVertical: 56,
    paddingHorizontal: 32,
    alignItems: 'center',
    gap: 10,
  },
  iconCircle: {
    width: 64,
    height: 64,
    borderRadius: 32,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 4,
  },
  title: {
    fontSize: 15,
    fontWeight: '700',
  },
  message: {
    fontSize: 13,
    textAlign: 'center',
    fontWeight: '500',
    lineHeight: 19,
  },
  url: {
    fontSize: 11,
    fontFamily: 'monospace',
    textAlign: 'center',
    opacity: 0.7,
  },
});
