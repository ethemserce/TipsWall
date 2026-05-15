import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { router } from 'expo-router';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ActivityIndicator,
  FlatList,
  Pressable,
  StyleSheet,
  TextInput,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { useMarketPreferences } from '@/src/hooks/useMarketPreferences';
import { useMarkets } from '@/src/hooks/useMarkets';
import { useTier } from '@/src/lib/auth/authStore';
import { marketPreferencesStore } from '@/src/lib/marketPreferences/store';
import { useTheme } from '@/src/lib/useTheme';

/**
 * Multi-select picker for the user's preferred markets. Persists via
 * the marketPreferencesStore — logged-in users get backend sync, guests
 * stay local. Cap is read from the store (server-authoritative for
 * registered users; defaults to free 5 for guests).
 */
export function MarketPreferencesScreen() {
  const c = useTheme();
  const { t } = useTranslation();
  const { marketIds, cap } = useMarketPreferences();
  const tier = useTier();
  const { lookup, isLoading } = useMarkets();
  const [search, setSearch] = useState('');
  const [draft, setDraft] = useState<Set<number>>(() => new Set(marketIds));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Keep the local draft in sync when the upstream store changes
  // (e.g. backend hydrate finishes after this screen mounted).
  useEffect(() => {
    setDraft(new Set(marketIds));
  }, [marketIds]);

  const markets = useMemo(() => Array.from(lookup.values()), [lookup]);
  const normalizedQuery = search.trim().toLocaleLowerCase('tr-TR');

  const filtered = useMemo(() => {
    if (!normalizedQuery) return markets;
    return markets.filter((m) =>
      (m.name ?? '').toLocaleLowerCase('tr-TR').includes(normalizedQuery),
    );
  }, [markets, normalizedQuery]);

  const dirty = useMemo(() => {
    if (draft.size !== marketIds.length) return true;
    for (const id of marketIds) if (!draft.has(id)) return true;
    return false;
  }, [draft, marketIds]);

  const overCap = draft.size > cap;

  const toggle = (id: number) => {
    setDraft((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else if (next.size >= cap) {
        // Block the add at the cap; tier upgrade prompt handled in Settings.
        return prev;
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const handleSave = async () => {
    setError(null);
    setSaving(true);
    const result = await marketPreferencesStore.replace(Array.from(draft));
    setSaving(false);
    if (!result.ok) {
      setError(
        result.error === 'over-cap'
          ? t('marketPrefs.errors.overCap', { defaultValue: 'Seçim sınırı aşıldı.' })
          : t('marketPrefs.errors.network', {
              defaultValue: 'Kaydedilemedi. İnternet bağlantını kontrol et.',
            }),
      );
      return;
    }
    router.back();
  };

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={[styles.headerBar, { borderBottomColor: c.border }]}>
        <Pressable onPress={() => router.back()} hitSlop={12} style={styles.headerBack}>
          <MaterialCommunityIcons name="chevron-left" size={24} color={c.text} />
        </Pressable>
        <ThemedText style={[styles.headerTitle, { color: c.text }]}>
          {t('marketPrefs.title', { defaultValue: 'Takip ettiğim oran tipleri' })}
        </ThemedText>
        <View style={styles.headerBack} />
      </View>

      <View
        style={[
          styles.summaryBar,
          { backgroundColor: overCap ? c.dangerSoft ?? c.brandSoft : c.brandSoft },
        ]}>
        <ThemedText style={[styles.summaryText, { color: c.text }]}>
          {t('marketPrefs.counter', {
            defaultValue: '{{count}}/{{cap}} seçili',
            count: draft.size,
            cap,
          })}
        </ThemedText>
        <ThemedText style={[styles.summaryHint, { color: c.textMuted }]}>
          {tier === 'premium'
            ? t('marketPrefs.tier.premium', { defaultValue: 'Premium: 30 market' })
            : t('marketPrefs.tier.free', {
                defaultValue: 'Free: 5 market · Premium ile artırılır',
              })}
        </ThemedText>
      </View>

      <View style={[styles.searchRow, { backgroundColor: c.surface, borderColor: c.borderSoft }]}>
        <MaterialCommunityIcons name="magnify" size={18} color={c.textMuted} />
        <TextInput
          value={search}
          onChangeText={setSearch}
          placeholder={t('marketPrefs.searchPlaceholder', { defaultValue: 'Market ara' })}
          placeholderTextColor={c.textMuted}
          autoCapitalize="none"
          autoCorrect={false}
          style={[styles.searchInput, { color: c.text }]}
        />
        {search.length > 0 ? (
          <Pressable onPress={() => setSearch('')} hitSlop={10}>
            <MaterialCommunityIcons name="close-circle" size={18} color={c.textMuted} />
          </Pressable>
        ) : null}
      </View>

      {isLoading ? (
        <View style={styles.center}>
          <ActivityIndicator color={c.brand} />
        </View>
      ) : (
        <FlatList
          data={filtered}
          keyExtractor={(m) => String(m.id)}
          keyboardShouldPersistTaps="handled"
          renderItem={({ item }) => {
            const checked = draft.has(item.id);
            const disabled = !checked && draft.size >= cap;
            return (
              <Pressable
                onPress={() => toggle(item.id)}
                disabled={disabled}
                style={({ pressed }) => [
                  styles.row,
                  { borderBottomColor: c.borderSoft },
                  pressed && { backgroundColor: c.brandSoft },
                  disabled && { opacity: 0.4 },
                ]}>
                <View
                  style={[
                    styles.checkbox,
                    {
                      backgroundColor: checked ? c.brand : 'transparent',
                      borderColor: checked ? c.brand : c.border,
                    },
                  ]}>
                  {checked ? (
                    <MaterialCommunityIcons name="check" size={14} color={c.textInverse} />
                  ) : null}
                </View>
                <ThemedText
                  numberOfLines={1}
                  style={[styles.rowName, { color: c.text }]}>
                  {item.name ?? `Market #${item.id}`}
                </ThemedText>
              </Pressable>
            );
          }}
          contentContainerStyle={styles.list}
          ListEmptyComponent={
            <ThemedText style={[styles.empty, { color: c.textMuted }]}>
              {t('marketPrefs.empty', { defaultValue: 'Bu aramada market yok.' })}
            </ThemedText>
          }
        />
      )}

      <View
        style={[
          styles.footer,
          { backgroundColor: c.surface, borderTopColor: c.border },
        ]}>
        {error ? (
          <ThemedText style={[styles.errorText, { color: c.danger }]}>{error}</ThemedText>
        ) : null}
        <Pressable
          onPress={handleSave}
          disabled={!dirty || saving || overCap}
          style={({ pressed }) => [
            styles.saveBtn,
            {
              backgroundColor: !dirty || overCap ? c.borderSoft : c.brand,
              opacity: pressed ? 0.85 : 1,
            },
          ]}>
          {saving ? (
            <ActivityIndicator color={c.textInverse} />
          ) : (
            <ThemedText style={[styles.saveText, { color: c.textInverse }]}>
              {t('marketPrefs.save', { defaultValue: 'Kaydet' })}
            </ThemedText>
          )}
        </Pressable>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  headerBar: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 8,
    paddingVertical: 8,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  headerBack: { width: 36, height: 36, alignItems: 'center', justifyContent: 'center' },
  headerTitle: { flex: 1, fontSize: 15, fontWeight: '700', textAlign: 'center' },
  summaryBar: {
    paddingHorizontal: 16,
    paddingVertical: 10,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  summaryText: { fontSize: 13, fontWeight: '700' },
  summaryHint: { fontSize: 11 },
  searchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginHorizontal: 12,
    marginTop: 10,
    paddingHorizontal: 12,
    paddingVertical: 8,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
  },
  searchInput: { flex: 1, fontSize: 14, padding: 0 },
  list: { paddingTop: 8, paddingBottom: 100 },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  checkbox: {
    width: 22,
    height: 22,
    borderRadius: 5,
    borderWidth: StyleSheet.hairlineWidth,
    alignItems: 'center',
    justifyContent: 'center',
  },
  rowName: { flex: 1, fontSize: 14 },
  empty: { fontSize: 13, textAlign: 'center', padding: 32 },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 },
  footer: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 8,
  },
  saveBtn: {
    paddingVertical: 14,
    borderRadius: 10,
    alignItems: 'center',
    justifyContent: 'center',
  },
  saveText: { fontSize: 14, fontWeight: '800' },
  errorText: { fontSize: 12, textAlign: 'center' },
});
