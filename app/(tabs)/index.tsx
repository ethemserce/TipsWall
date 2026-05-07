import { format } from 'date-fns';
import { useState } from 'react';
import {
  ActivityIndicator,
  FlatList,
  RefreshControl,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { DateBar } from '@/src/components/DateBar';
import { FixtureCard } from '@/src/components/FixtureCard';
import { useFixtures } from '@/src/hooks/useFixtures';

export default function TodayScreen() {
  const [selectedDate, setSelectedDate] = useState(() => new Date());
  const isoDate = format(selectedDate, 'yyyy-MM-dd');

  const { data, isLoading, isFetching, isError, error, refetch } = useFixtures({
    date: isoDate,
    perPage: 100,
  });

  return (
    <SafeAreaView style={styles.flex} edges={['top']}>
      <ThemedView style={styles.flex}>
        <View style={styles.header}>
          <ThemedText type="title">Matches</ThemedText>
          <ThemedText style={styles.subtitle}>
            {format(selectedDate, 'EEEE, d MMMM yyyy')}
          </ThemedText>
        </View>

        <DateBar selectedDate={selectedDate} onSelect={setSelectedDate} />

        {isLoading ? (
          <View style={styles.center}>
            <ActivityIndicator />
          </View>
        ) : isError ? (
          <View style={styles.center}>
            <ThemedText style={styles.errorTitle}>Couldn&apos;t load fixtures</ThemedText>
            <ThemedText style={styles.errorMessage}>
              {error instanceof Error ? error.message : 'Unknown error'}
            </ThemedText>
          </View>
        ) : (
          <FlatList
            data={data?.items ?? []}
            keyExtractor={(item) => String(item.id)}
            renderItem={({ item }) => <FixtureCard fixture={item} />}
            contentContainerStyle={styles.list}
            refreshControl={
              <RefreshControl refreshing={isFetching} onRefresh={refetch} />
            }
            ListEmptyComponent={
              <View style={styles.center}>
                <ThemedText>No fixtures for this day.</ThemedText>
              </View>
            }
          />
        )}
      </ThemedView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  header: {
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 4,
  },
  subtitle: {
    opacity: 0.6,
    fontSize: 13,
    marginTop: 2,
  },
  list: {
    paddingBottom: 32,
    flexGrow: 1,
  },
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 32,
    gap: 8,
  },
  errorTitle: {
    fontSize: 16,
    fontWeight: '600',
  },
  errorMessage: {
    fontSize: 13,
    opacity: 0.7,
    textAlign: 'center',
  },
});
