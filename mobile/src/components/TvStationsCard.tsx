import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Image } from 'expo-image';
import { useTranslation } from 'react-i18next';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';
import type { FixtureTvStation } from '@/src/types/fixtureDetailExtras';

interface TvStationsCardProps {
  stations: FixtureTvStation[] | undefined;
}

export function TvStationsCard({ stations }: TvStationsCardProps) {
  const c = useTheme();
  const { t } = useTranslation();
  if (!stations || stations.length === 0) return null;

  return (
    <View
      style={[
        styles.card,
        { backgroundColor: c.surface, borderColor: c.border },
      ]}>
      <View style={styles.headerRow}>
        <MaterialCommunityIcons name="television" size={16} color={c.textMuted} />
        <ThemedText style={[styles.title, { color: c.textMuted }]}>
          {t('fixture.tvStations.title').toUpperCase()}
        </ThemedText>
      </View>
      <View style={styles.list}>
        {stations.map((s) => (
          <View
            key={s.id}
            style={[
              styles.chip,
              { backgroundColor: c.surfaceElevated, borderColor: c.border },
            ]}>
            {s.image_path ? (
              <Image
                source={{ uri: s.image_path }}
                style={styles.logo}
                contentFit="contain"
              />
            ) : null}
            <ThemedText style={[styles.name, { color: c.text }]} numberOfLines={1}>
              {s.name ?? '—'}
            </ThemedText>
          </View>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    marginHorizontal: 16,
    marginTop: 16,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 12,
    paddingHorizontal: 14,
    paddingVertical: 12,
  },
  headerRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    marginBottom: 10,
  },
  title: {
    fontSize: 11,
    fontWeight: '600',
    letterSpacing: 0.6,
  },
  list: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  chip: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 14,
    paddingHorizontal: 10,
    paddingVertical: 6,
  },
  logo: {
    width: 14,
    height: 14,
  },
  name: {
    fontSize: 12,
    fontWeight: '500',
  },
});
