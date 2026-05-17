import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { Image } from 'expo-image';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { countryName } from '@/src/lib/countryName';
import { useTheme } from '@/src/lib/useTheme';
import type { Country } from '@/src/types/country';
import type { League } from '@/src/types/league';

export interface LeagueScopeRow {
  leagueId: number;
  league?: League;
  country?: Country;
  matchCount: number;
}

interface LeagueScopeSheetProps {
  visible: boolean;
  onClose: () => void;
  /** Leagues currently shown on the date — derived after search + state filter. */
  rows: LeagueScopeRow[];
  /** League ids currently selected; empty set means "no scope, show all". */
  selectedLeagueIds: Set<number>;
  onChange: (next: Set<number>) => void;
}

/**
 * Bottom-sheet league multi-select for the analysis screen. Lists exactly
 * the leagues that have at least one fixture under the user's current
 * date + search + state filter, with per-league checkbox toggles and
 * Hepsi / Hiçbiri shortcuts at the top.
 *
 * Empty selection is the implicit "all" state — the screen passes
 * selectedLeagueIds straight to the filter pipeline and treats size==0
 * as "no scope". That keeps the default behaviour zero-config and means
 * the user can clear the scope by tapping "Hiçbiri" once.
 */
export function LeagueScopeSheet({
  visible,
  onClose,
  rows,
  selectedLeagueIds,
  onChange,
}: LeagueScopeSheetProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();

  const allSelected = useMemo(
    () => rows.length > 0 && rows.every((r) => selectedLeagueIds.has(r.leagueId)),
    [rows, selectedLeagueIds],
  );
  const noneSelected = selectedLeagueIds.size === 0;

  const toggleOne = (leagueId: number) => {
    const next = new Set(selectedLeagueIds);
    if (next.has(leagueId)) next.delete(leagueId);
    else next.add(leagueId);
    onChange(next);
  };
  const selectAll = () => onChange(new Set(rows.map((r) => r.leagueId)));
  const clear = () => onChange(new Set());

  return (
    <Modal
      visible={visible}
      transparent
      animationType="slide"
      onRequestClose={onClose}>
      <Pressable style={styles.backdrop} onPress={onClose}>
        <Pressable
          onPress={(e) => e.stopPropagation()}
          style={[
            styles.sheet,
            {
              backgroundColor: c.surface,
              borderColor: c.border,
              paddingBottom: Math.max(12, insets.bottom + 8),
            },
          ]}>
          <View style={styles.handle}>
            <View style={[styles.handleBar, { backgroundColor: c.border }]} />
          </View>

          <View style={styles.header}>
            <ThemedText style={[styles.title, { color: c.text }]}>
              {t('leagueScope.title')}
            </ThemedText>
            <ThemedText style={[styles.subtitle, { color: c.textMuted }]}>
              {t('leagueScope.summary', {
                selected: noneSelected ? rows.length : selectedLeagueIds.size,
                total: rows.length,
              })}
            </ThemedText>
          </View>

          <View style={styles.bulkRow}>
            <Pressable
              onPress={selectAll}
              disabled={allSelected || rows.length === 0}
              style={[
                styles.bulkChip,
                {
                  borderColor: allSelected ? c.brand : c.border,
                  backgroundColor: allSelected ? c.brand : 'transparent',
                  opacity: rows.length === 0 ? 0.5 : 1,
                },
              ]}>
              <ThemedText
                style={[
                  styles.bulkText,
                  { color: allSelected ? c.textInverse : c.text },
                ]}>
                {t('leagueScope.selectAll')}
              </ThemedText>
            </Pressable>
            <Pressable
              onPress={clear}
              disabled={noneSelected}
              style={[
                styles.bulkChip,
                {
                  borderColor: c.border,
                  backgroundColor: 'transparent',
                  opacity: noneSelected ? 0.5 : 1,
                },
              ]}>
              <ThemedText style={[styles.bulkText, { color: c.text }]}>
                {t('leagueScope.clear')}
              </ThemedText>
            </Pressable>
          </View>

          <ScrollView contentContainerStyle={styles.list}>
            {rows.length === 0 ? (
              <View style={styles.empty}>
                <ThemedText style={[styles.emptyText, { color: c.textMuted }]}>
                  {t('leagueScope.empty')}
                </ThemedText>
              </View>
            ) : (
              rows.map((row) => {
                const checked = selectedLeagueIds.has(row.leagueId);
                const name = row.league?.name ?? `League #${row.leagueId}`;
                // `noneSelected` is the implicit "all" state — visually mark
                // every row as included so the user understands the current
                // filter scope without having to read the header subtitle.
                const visuallyOn = checked || noneSelected;
                return (
                  <Pressable
                    key={row.leagueId}
                    onPress={() => toggleOne(row.leagueId)}
                    style={({ pressed }) => [
                      styles.row,
                      {
                        backgroundColor: pressed ? c.brandSoft : 'transparent',
                        borderBottomColor: c.borderSoft,
                      },
                    ]}>
                    <View style={[styles.logoWrap, { backgroundColor: c.bg }]}>
                      {row.league?.image_path ? (
                        <Image
                          source={{ uri: row.league.image_path }}
                          style={styles.logo}
                          contentFit="contain"
                          transition={120}
                        />
                      ) : (
                        <MaterialCommunityIcons
                          name="trophy-outline"
                          size={16}
                          color={c.textMuted}
                        />
                      )}
                    </View>
                    <View style={styles.meta}>
                      <ThemedText
                        style={[styles.leagueName, { color: c.text }]}
                        numberOfLines={1}>
                        {name}
                      </ThemedText>
                      <ThemedText
                        style={[styles.country, { color: c.textMuted }]}
                        numberOfLines={1}>
                        {row.country
                          ? countryName(row.country)
                          : row.league?.short_code ?? '—'}
                      </ThemedText>
                    </View>
                    <View
                      style={[
                        styles.matchBadge,
                        {
                          backgroundColor: visuallyOn ? c.brandSoft : c.surface,
                          borderColor: c.borderSoft,
                        },
                      ]}>
                      <ThemedText
                        style={[
                          styles.matchBadgeText,
                          { color: visuallyOn ? c.brand : c.textMuted },
                        ]}>
                        {row.matchCount}
                      </ThemedText>
                    </View>
                    <View
                      style={[
                        styles.checkbox,
                        {
                          borderColor: checked ? c.brand : c.border,
                          backgroundColor: checked ? c.brand : 'transparent',
                        },
                      ]}>
                      {checked ? (
                        <MaterialCommunityIcons
                          name="check"
                          size={14}
                          color={c.textInverse}
                        />
                      ) : null}
                    </View>
                  </Pressable>
                );
              })
            )}
          </ScrollView>

          <Pressable
            onPress={onClose}
            style={[styles.doneBtn, { backgroundColor: c.brand }]}>
            <ThemedText style={[styles.doneText, { color: c.textInverse }]}>
              {t('leagueScope.done')}
            </ThemedText>
          </Pressable>
        </Pressable>
      </Pressable>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    justifyContent: 'flex-end',
    backgroundColor: 'rgba(0,0,0,0.45)',
  },
  sheet: {
    borderTopLeftRadius: 16,
    borderTopRightRadius: 16,
    borderTopWidth: StyleSheet.hairlineWidth,
    maxHeight: '85%',
    paddingHorizontal: 16,
    paddingTop: 8,
  },
  handle: {
    alignItems: 'center',
    paddingVertical: 6,
  },
  handleBar: {
    width: 36,
    height: 4,
    borderRadius: 2,
  },
  header: {
    paddingVertical: 8,
    gap: 2,
  },
  title: {
    fontSize: 16,
    fontWeight: '800',
  },
  subtitle: {
    fontSize: 12,
    fontWeight: '500',
  },
  bulkRow: {
    flexDirection: 'row',
    gap: 8,
    paddingVertical: 8,
  },
  bulkChip: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
  },
  bulkText: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
  list: {
    paddingBottom: 8,
  },
  empty: {
    paddingVertical: 24,
    alignItems: 'center',
  },
  emptyText: {
    fontSize: 13,
    fontWeight: '500',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingVertical: 10,
    paddingHorizontal: 4,
    borderBottomWidth: StyleSheet.hairlineWidth,
  },
  logoWrap: {
    width: 32,
    height: 32,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  logo: {
    width: 24,
    height: 24,
  },
  meta: {
    flex: 1,
    gap: 2,
  },
  leagueName: {
    fontSize: 14,
    fontWeight: '700',
  },
  country: {
    fontSize: 11,
    fontWeight: '500',
  },
  matchBadge: {
    minWidth: 24,
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 8,
    borderWidth: StyleSheet.hairlineWidth,
    alignItems: 'center',
    justifyContent: 'center',
  },
  matchBadgeText: {
    fontSize: 11,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  checkbox: {
    width: 22,
    height: 22,
    borderRadius: 6,
    borderWidth: 2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  doneBtn: {
    marginTop: 8,
    paddingVertical: 12,
    borderRadius: 12,
    alignItems: 'center',
  },
  doneText: {
    fontSize: 14,
    fontWeight: '800',
    letterSpacing: 0.3,
  },
});
