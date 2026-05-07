import { ActivityIndicator, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ApiClientError } from '@/src/api/client';
import { useTheme } from '@/src/lib/useTheme';

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
  const status = error instanceof ApiClientError ? error.status : undefined;
  const url = error instanceof ApiClientError ? error.url : undefined;
  const message =
    error instanceof Error ? error.message : 'Something went wrong.';

  return (
    <View style={styles.empty}>
      <ThemedText style={[styles.title, { color: c.text }]}>
        Couldn&apos;t load this tab{status ? ` (HTTP ${status})` : ''}
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
}

export function TabEmpty({ message }: TabEmptyProps) {
  const c = useTheme();
  return (
    <View style={styles.empty}>
      <ThemedText style={[styles.message, { color: c.textMuted }]}>
        {message}
      </ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  empty: {
    paddingVertical: 64,
    paddingHorizontal: 32,
    alignItems: 'center',
    gap: 6,
  },
  title: {
    fontSize: 14,
    fontWeight: '600',
  },
  message: {
    fontSize: 13,
    textAlign: 'center',
  },
  url: {
    fontSize: 11,
    fontFamily: 'monospace',
    textAlign: 'center',
    opacity: 0.7,
  },
});
