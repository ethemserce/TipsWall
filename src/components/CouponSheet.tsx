import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { format, parseISO } from 'date-fns';
import { useTranslation } from 'react-i18next';
import { Modal, Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useDraftSuggestions, type DraftSuggestion } from '@/src/hooks/useDraftSuggestions';
import {
  clearDraft,
  removeSelection,
  saveDraft,
  toggleSelection,
  totalOdd,
  useCouponStore,
} from '@/src/lib/coupons/store';
import type { CouponSelection } from '@/src/lib/coupons/types';
import { useTheme } from '@/src/lib/useTheme';

interface CouponSheetProps {
  visible: boolean;
  onClose: () => void;
}

export function CouponSheet({ visible, onClose }: CouponSheetProps) {
  const c = useTheme();
  const { t } = useTranslation();
  const draft = useCouponStore((s) => s.draft);
  const suggestions = useDraftSuggestions(draft.selections);

  const handleSave = () => {
    saveDraft();
    onClose();
  };

  const handleClear = () => {
    clearDraft();
    onClose();
  };

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
            { backgroundColor: c.surface, borderColor: c.border },
          ]}>
          <View style={styles.handle}>
            <View style={[styles.handleBar, { backgroundColor: c.border }]} />
          </View>

          <View style={styles.header}>
            <ThemedText style={[styles.title, { color: c.text }]}>
              {t('coupons.sheet.title', { count: draft.selections.length })}
            </ThemedText>
            <Pressable onPress={handleClear} hitSlop={8}>
              <ThemedText style={[styles.clearText, { color: c.textMuted }]}>
                {t('coupons.sheet.clear')}
              </ThemedText>
            </Pressable>
          </View>

          <ScrollView contentContainerStyle={styles.body}>
            {draft.selections.length === 0 ? (
              <ThemedText style={[styles.empty, { color: c.textMuted }]}>
                {t('coupons.sheet.empty')}
              </ThemedText>
            ) : (
              draft.selections.map((s) => (
                <SelectionRow key={s.id} selection={s} />
              ))
            )}

            {suggestions.length > 0 ? (
              <View style={styles.suggestSection}>
                <ThemedText style={[styles.suggestLabel, { color: c.textMuted }]}>
                  {t('coupons.sheet.suggestions')}
                </ThemedText>
                {suggestions.map((sg) => (
                  <SuggestionRow key={sg.signal.id} suggestion={sg} />
                ))}
              </View>
            ) : null}
          </ScrollView>

          {draft.selections.length > 0 ? (
            <View style={[styles.footer, { borderTopColor: c.border }]}>
              <View style={styles.totals}>
                <ThemedText style={[styles.totalLabel, { color: c.textMuted }]}>
                  {t('coupons.sheet.totalOdd')}
                </ThemedText>
                <ThemedText style={[styles.totalValue, { color: c.text }]}>
                  {totalOdd(draft).toFixed(2)}
                </ThemedText>
              </View>
              <Pressable
                onPress={handleSave}
                style={[styles.saveBtn, { backgroundColor: c.brand }]}>
                <MaterialCommunityIcons
                  name="content-save-outline"
                  size={16}
                  color={c.textInverse}
                />
                <ThemedText style={[styles.saveText, { color: c.textInverse }]}>
                  {t('coupons.sheet.save')}
                </ThemedText>
              </Pressable>
            </View>
          ) : null}
        </Pressable>
      </Pressable>
    </Modal>
  );
}

function SelectionRow({ selection }: { selection: CouponSelection }) {
  const c = useTheme();
  const time =
    selection.startingAt
      ? format(parseISO(selection.startingAt), 'dd.MM HH:mm')
      : null;
  const isValue =
    selection.dso != null &&
    selection.iko != null &&
    selection.dso > selection.iko;

  return (
    <View style={[styles.row, { borderBottomColor: c.border }]}>
      <View style={styles.rowMain}>
        <View style={styles.rowTop}>
          <ThemedText
            style={[styles.fixtureName, { color: c.text }]}
            numberOfLines={1}>
            {selection.fixtureName}
          </ThemedText>
          {time ? (
            <ThemedText style={[styles.fixtureTime, { color: c.textMuted }]}>
              {time}
            </ThemedText>
          ) : null}
        </View>
        <View style={styles.rowBottom}>
          <ThemedText style={[styles.tip, { color: c.brand }]}>
            {isValue ? '★ ' : ''}
            {selection.marketShort}{' '}
            {selection.outcomeDisplay ?? selection.outcomeLabel}
          </ThemedText>
          {selection.dso != null ? (
            <ThemedText style={[styles.snapshot, { color: c.textMuted }]}>
              DSO {selection.dso.toFixed(0)}%
              {selection.iko != null
                ? ` · İKO ${selection.iko.toFixed(0)}%`
                : ''}
            </ThemedText>
          ) : null}
        </View>
      </View>
      <View style={styles.rowOdd}>
        <ThemedText style={[styles.oddValue, { color: c.text }]}>
          {selection.oddValue.toFixed(2)}
        </ThemedText>
      </View>
      <Pressable
        onPress={() => removeSelection(selection.id)}
        hitSlop={8}
        style={styles.removeBtn}>
        <MaterialCommunityIcons name="close" size={16} color={c.textMuted} />
      </Pressable>
    </View>
  );
}

function SuggestionRow({ suggestion }: { suggestion: DraftSuggestion }) {
  const c = useTheme();
  const time = suggestion.startingAt
    ? format(parseISO(suggestion.startingAt), 'HH:mm')
    : null;
  const handleAdd = () => {
    toggleSelection({
      fixtureId: suggestion.fixtureId,
      fixtureName: suggestion.fixtureName,
      startingAt: suggestion.startingAt,
      bookmakerId: 2,
      marketId: suggestion.signal.market_id,
      marketShort: suggestion.marketShort,
      outcomeLabel: suggestion.outcomeLabel,
      outcomeDisplay: suggestion.outcomeDisplay,
      total: suggestion.signal.total,
      handicap: suggestion.signal.handicap,
      oddValue: suggestion.oddValue,
      dso: suggestion.signal.winning_percent,
      vbet: suggestion.signal.earning_percent,
      iko: suggestion.signal.iko ?? null,
      sampleCount: suggestion.signal.sample_count,
    });
  };
  return (
    <Pressable
      onPress={handleAdd}
      style={({ pressed }) => [
        styles.suggestRow,
        {
          backgroundColor: pressed ? c.bg : c.surface,
          borderColor: c.border,
        },
      ]}>
      <View style={styles.suggestMain}>
        <View style={styles.suggestTop}>
          <ThemedText
            style={[styles.suggestFixture, { color: c.text }]}
            numberOfLines={1}>
            {suggestion.fixtureName}
          </ThemedText>
          {time ? (
            <ThemedText style={[styles.suggestTime, { color: c.textMuted }]}>
              {time}
            </ThemedText>
          ) : null}
        </View>
        <ThemedText style={[styles.suggestTip, { color: c.brand }]}>
          ★ {suggestion.marketShort} {suggestion.outcomeDisplay}
        </ThemedText>
      </View>
      <View style={styles.suggestRight}>
        <ThemedText style={[styles.suggestOdd, { color: c.text }]}>
          {suggestion.oddValue.toFixed(2)}
        </ThemedText>
        <View style={[styles.suggestAdd, { backgroundColor: c.brand }]}>
          <MaterialCommunityIcons name="plus" size={14} color={c.textInverse} />
        </View>
      </View>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'flex-end',
  },
  sheet: {
    maxHeight: '80%',
    borderTopLeftRadius: 16,
    borderTopRightRadius: 16,
    borderTopWidth: StyleSheet.hairlineWidth,
    paddingBottom: 12,
  },
  handle: {
    alignItems: 'center',
    paddingTop: 6,
    paddingBottom: 2,
  },
  handleBar: {
    width: 36,
    height: 3,
    borderRadius: 2,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingTop: 4,
    paddingBottom: 8,
  },
  title: {
    fontSize: 15,
    fontWeight: '700',
  },
  clearText: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.6,
  },
  body: {
    paddingHorizontal: 12,
  },
  suggestSection: {
    marginTop: 12,
    paddingTop: 8,
    gap: 6,
  },
  suggestLabel: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.6,
    paddingHorizontal: 4,
    paddingBottom: 4,
  },
  suggestRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingHorizontal: 10,
    paddingVertical: 8,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
  },
  suggestMain: {
    flex: 1,
    gap: 2,
  },
  suggestTop: {
    flexDirection: 'row',
    alignItems: 'baseline',
    gap: 8,
  },
  suggestFixture: {
    flex: 1,
    fontSize: 12,
    fontWeight: '600',
  },
  suggestTime: {
    fontSize: 10,
    fontVariant: ['tabular-nums'],
  },
  suggestTip: {
    fontSize: 11,
    fontWeight: '700',
  },
  suggestRight: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  suggestOdd: {
    fontSize: 13,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
    minWidth: 36,
    textAlign: 'right',
  },
  suggestAdd: {
    width: 22,
    height: 22,
    borderRadius: 11,
    alignItems: 'center',
    justifyContent: 'center',
  },
  empty: {
    textAlign: 'center',
    fontSize: 13,
    paddingVertical: 32,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 10,
    paddingHorizontal: 4,
    borderBottomWidth: StyleSheet.hairlineWidth,
    gap: 8,
  },
  rowMain: {
    flex: 1,
    gap: 2,
  },
  rowTop: {
    flexDirection: 'row',
    alignItems: 'baseline',
    gap: 8,
  },
  fixtureName: {
    flex: 1,
    fontSize: 13,
    fontWeight: '600',
  },
  fixtureTime: {
    fontSize: 11,
    fontVariant: ['tabular-nums'],
  },
  rowBottom: {
    flexDirection: 'row',
    alignItems: 'baseline',
    gap: 8,
  },
  tip: {
    fontSize: 12,
    fontWeight: '700',
  },
  snapshot: {
    fontSize: 10,
    fontVariant: ['tabular-nums'],
  },
  rowOdd: {
    minWidth: 48,
    alignItems: 'flex-end',
  },
  oddValue: {
    fontSize: 14,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  removeBtn: {
    width: 24,
    height: 24,
    alignItems: 'center',
    justifyContent: 'center',
  },
  footer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 16,
    paddingTop: 12,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  totals: {
    flex: 1,
    gap: 2,
  },
  totalLabel: {
    fontSize: 9,
    fontWeight: '700',
    letterSpacing: 0.6,
  },
  totalValue: {
    fontSize: 18,
    fontWeight: '800',
    fontVariant: ['tabular-nums'],
  },
  saveBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 10,
  },
  saveText: {
    fontSize: 12,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
});
