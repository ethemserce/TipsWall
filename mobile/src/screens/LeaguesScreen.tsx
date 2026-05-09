import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Image } from 'expo-image';
import { router } from 'expo-router';
import { useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  Pressable,
  RefreshControl,
  SectionList,
  StyleSheet,
  TextInput,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { AppBrand } from '@/src/components/AppBrand';
import { useCountryLookup } from '@/src/hooks/useCountryLookup';
import { useLeagues } from '@/src/hooks/useLeagues';
import { useTheme } from '@/src/lib/useTheme';
import type { Country } from '@/src/types/country';
import type { League } from '@/src/types/league';

// Turkish-aware lowercasing so "İstanbul" / "ISTANBUL" / "istanbul" all
// match. Mirrors the home search helper.
const normalize = (value: string | null | undefined): string =>
  (value ?? '').toLocaleLowerCase('tr-TR');

interface LeagueSection {
  title: string;
  data: League[];
  countryImage: string | null;
}

export function LeaguesScreen() {
  const c = useTheme();
  const { t } = useTranslation();
  const [searchOpen, setSearchOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const searchInputRef = useRef<TextInput | null>(null);

  const leaguesQuery = useLeagues({ active: true });
  const allLeagues = leaguesQuery.data?.items ?? [];

  // Pull country names + flags for every league we render. The lookup
  // dedups internally so passing a long list with repeats is fine.
  const countryIds = useMemo(
    () => allLeagues.map((l) => l.country_id),
    [allLeagues],
  );
  const { lookup: countryLookup } = useCountryLookup(countryIds);

  const normalizedQuery = useMemo(
    () => normalize(searchQuery.trim()),
    [searchQuery],
  );
  const searchActive = normalizedQuery.length > 0;

  const filtered = useMemo(() => {
    if (!searchActive) return allLeagues;
    return allLeagues.filter((l) => {
      if (normalize(l.name).includes(normalizedQuery)) return true;
      const country = l.country_id ? countryLookup.get(l.country_id) : undefined;
      return normalize(country?.name).includes(normalizedQuery);
    });
  }, [allLeagues, countryLookup, normalizedQuery, searchActive]);

  // Group by country, then sort countries alphabetically by their TR-aware
  // collation (so İ sorts after I correctly when locale is tr).
  const sections = useMemo<LeagueSection[]>(() => {
    const groups = new Map<
      number | -1,
      { name: string; image: string | null; data: League[] }
    >();
    for (const league of filtered) {
      const id = league.country_id ?? -1;
      const country: Country | undefined =
        id >= 0 ? countryLookup.get(id) : undefined;
      const name = country?.name ?? t('leagues.unknownCountry');
      const image = country?.image_path ?? null;
      const existing = groups.get(id);
      if (existing) existing.data.push(league);
      else groups.set(id, { name, image, data: [league] });
    }
    return Array.from(groups.values())
      .map((g) => ({
        title: g.name,
        countryImage: g.image,
        data: [...g.data].sort((a, b) =>
          a.name.localeCompare(b.name, 'tr'),
        ),
      }))
      .sort((a, b) => a.title.localeCompare(b.title, 'tr'));
  }, [filtered, countryLookup, t]);

  const handleToggleSearch = () => {
    setSearchOpen((prev) => {
      const next = !prev;
      if (!next) setSearchQuery('');
      else setTimeout(() => searchInputRef.current?.focus(), 0);
      return next;
    });
  };

  const handleSelectLeague = (league: League) => {
    router.push(`/league/${league.id}` as never);
  };

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.headerRow}>
        <AppBrand />
        <Pressable
          onPress={handleToggleSearch}
          hitSlop={12}
          accessibilityRole="button"
          accessibilityLabel={
            searchOpen ? t('home.search.a11yClose') : t('home.search.a11yOpen')
          }
          style={({ pressed }) => [
            styles.headerSearchBtn,
            {
              backgroundColor:
                pressed || searchOpen ? c.brandSoft : 'transparent',
            },
          ]}>
          <MaterialCommunityIcons
            name={searchOpen ? 'close' : 'magnify'}
            size={22}
            color={searchOpen ? c.brand : c.textMuted}
          />
        </Pressable>
      </View>

      {searchOpen ? (
        <View
          style={[
            styles.searchRow,
            { backgroundColor: c.surface, borderColor: c.borderSoft },
          ]}>
          <MaterialCommunityIcons name="magnify" size={18} color={c.textMuted} />
          <TextInput
            ref={searchInputRef}
            value={searchQuery}
            onChangeText={setSearchQuery}
            placeholder={t('leagues.searchPlaceholder')}
            placeholderTextColor={c.textMuted}
            autoCapitalize="none"
            autoCorrect={false}
            returnKeyType="search"
            style={[styles.searchInput, { color: c.text }]}
          />
          {searchQuery.length > 0 ? (
            <Pressable
              onPress={() => setSearchQuery('')}
              hitSlop={10}
              accessibilityRole="button"
              accessibilityLabel={t('home.search.a11yClear')}>
              <MaterialCommunityIcons
                name="close-circle"
                size={18}
                color={c.textMuted}
              />
            </Pressable>
          ) : null}
        </View>
      ) : null}

      {leaguesQuery.isLoading && allLeagues.length === 0 ? (
        <View style={styles.center}>
          <ActivityIndicator color={c.brand} />
        </View>
      ) : leaguesQuery.isError && allLeagues.length === 0 ? (
        <View style={styles.center}>
          <ThemedText style={[styles.errorTitle, { color: c.text }]}>
            {t('common.couldNotLoad')}
          </ThemedText>
          <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
            {leaguesQuery.error instanceof Error
              ? leaguesQuery.error.message
              : t('common.somethingWentWrong')}
          </ThemedText>
        </View>
      ) : (
        <SectionList
          sections={sections}
          keyExtractor={(l) => String(l.id)}
          keyboardShouldPersistTaps="handled"
          renderItem={({ item, index, section }) => (
            <Pressable
              onPress={() => handleSelectLeague(item)}
              accessibilityRole="button"
              accessibilityLabel={item.name}
              style={({ pressed }) => [
                styles.leagueRow,
                {
                  backgroundColor: pressed ? c.brandSoft : c.surfaceElevated,
                  borderColor: c.borderSoft,
                },
                index === 0 && styles.cardWrapFirst,
                index === section.data.length - 1 && styles.cardWrapLast,
                index > 0 && { borderTopWidth: 0 },
              ]}>
              {item.image_path ? (
                <Image
                  source={{ uri: item.image_path }}
                  style={styles.leagueLogo}
                  contentFit="contain"
                />
              ) : (
                <View
                  style={[
                    styles.leagueLogoPlaceholder,
                    { backgroundColor: c.border },
                  ]}
                />
              )}
              <ThemedText
                style={[styles.leagueName, { color: c.text }]}
                numberOfLines={1}>
                {item.name}
              </ThemedText>
              <MaterialCommunityIcons
                name="chevron-right"
                size={20}
                color={c.textMuted}
              />
            </Pressable>
          )}
          renderSectionHeader={({ section }) => (
            <View style={[styles.sectionHeader, { backgroundColor: c.bg }]}>
              {section.countryImage ? (
                <Image
                  source={{ uri: section.countryImage }}
                  style={styles.flag}
                  contentFit="cover"
                />
              ) : null}
              <ThemedText
                style={[styles.sectionHeaderText, { color: c.textMuted }]}>
                {section.title.toLocaleUpperCase('tr-TR')}
              </ThemedText>
              <View style={[styles.countBadge, { backgroundColor: c.brandSoft }]}>
                <ThemedText style={[styles.countText, { color: c.brand }]}>
                  {section.data.length}
                </ThemedText>
              </View>
            </View>
          )}
          stickySectionHeadersEnabled={false}
          ListEmptyComponent={
            <View style={styles.center}>
              <View style={[styles.emptyIcon, { backgroundColor: c.brandSoft }]}>
                <MaterialCommunityIcons
                  name={searchActive ? 'magnify-close' : 'trophy-outline'}
                  size={28}
                  color={c.brand}
                />
              </View>
              <ThemedText style={[styles.errorTitle, { color: c.text }]}>
                {searchActive
                  ? t('leagues.empty.searchTitle')
                  : t('leagues.empty.title')}
              </ThemedText>
              <ThemedText style={[styles.errorMessage, { color: c.textMuted }]}>
                {searchActive
                  ? t('leagues.empty.searchBody')
                  : t('leagues.empty.body')}
              </ThemedText>
            </View>
          }
          contentContainerStyle={styles.list}
          refreshControl={
            <RefreshControl
              refreshing={leaguesQuery.isFetching}
              onRefresh={leaguesQuery.refetch}
              tintColor={c.brand}
            />
          }
        />
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  headerRow: {
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 4,
    justifyContent: 'center',
  },
  headerSearchBtn: {
    position: 'absolute',
    right: 12,
    top: 6,
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
  },
  searchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginHorizontal: 16,
    marginTop: 4,
    marginBottom: 4,
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
  },
  searchInput: {
    flex: 1,
    fontSize: 14,
    paddingVertical: 0,
  },
  list: {
    paddingHorizontal: 12,
    paddingTop: 4,
    paddingBottom: 32,
  },
  sectionHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 6,
    paddingTop: 16,
    paddingBottom: 8,
    gap: 8,
  },
  flag: {
    width: 18,
    height: 14,
    borderRadius: 2,
  },
  sectionHeaderText: {
    fontSize: 11,
    fontWeight: '800',
    letterSpacing: 0.7,
    flexShrink: 1,
  },
  countBadge: {
    minWidth: 20,
    height: 18,
    paddingHorizontal: 6,
    borderRadius: 9,
    alignItems: 'center',
    justifyContent: 'center',
  },
  countText: {
    fontSize: 10,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  // Each row borders all 4 sides; first/last clip the rounded corners so
  // the section reads as one stacked card. Inner rows omit the top
  // border to avoid double-line stripes between siblings.
  leagueRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderWidth: StyleSheet.hairlineWidth,
  },
  cardWrapFirst: {
    borderTopLeftRadius: 14,
    borderTopRightRadius: 14,
  },
  cardWrapLast: {
    borderBottomLeftRadius: 14,
    borderBottomRightRadius: 14,
  },
  leagueLogo: {
    width: 28,
    height: 28,
  },
  leagueLogoPlaceholder: {
    width: 28,
    height: 28,
    borderRadius: 4,
  },
  leagueName: {
    flex: 1,
    fontSize: 14,
    fontWeight: '600',
  },
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 32,
    gap: 8,
  },
  emptyIcon: {
    width: 64,
    height: 64,
    borderRadius: 32,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 4,
  },
  errorTitle: {
    fontSize: 16,
    fontWeight: '700',
  },
  errorMessage: {
    fontSize: 13,
    textAlign: 'center',
    fontWeight: '500',
  },
});
