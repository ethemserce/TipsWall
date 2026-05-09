import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal, Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

// Metric column shortcodes used in card rows. Long names + descriptions
// resolve from the i18n bundle so the same component speaks both TR and
// EN. The shortcodes themselves stay literal — they're metric symbols,
// not translations.
const METRIC_KEYS: { short: string; key: 'dso' | 'vbet' | 'iko' | 'kzky' }[] = [
  { short: 'DSO', key: 'dso' },
  { short: 'VBET', key: 'vbet' },
  { short: 'İKO', key: 'iko' },
  { short: 'KZ / KY', key: 'kzky' },
];

const MARKET_KEYS: { short: string; key: string }[] = [
  { short: 'MS', key: 'MS' },
  { short: 'DNB', key: 'DNB' },
  { short: 'KG', key: 'KG' },
  { short: 'EV', key: 'EV' },
  { short: 'DEP', key: 'DEP' },
  { short: 'İY', key: 'iy' },
  { short: '2Y', key: 'y2' },
  { short: 'T/Ç', key: 'tc' },
];

export function MarketLegendButton() {
  const c = useTheme();
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);

  return (
    <>
      <Pressable
        onPress={() => setOpen(true)}
        hitSlop={12}
        style={({ pressed }) => [
          styles.iconBtn,
          { backgroundColor: pressed ? c.surface : 'transparent' },
        ]}>
        <MaterialCommunityIcons
          name="information-outline"
          size={22}
          color={c.textMuted}
        />
      </Pressable>

      <Modal
        visible={open}
        transparent
        animationType="fade"
        onRequestClose={() => setOpen(false)}>
        <Pressable style={styles.backdrop} onPress={() => setOpen(false)}>
          <Pressable
            onPress={(e) => e.stopPropagation()}
            style={[
              styles.sheet,
              { backgroundColor: c.surface, borderColor: c.border },
            ]}>
            <View style={styles.sheetHeader}>
              <ThemedText style={[styles.sheetTitle, { color: c.text }]}>
                {t('legend.title')}
              </ThemedText>
              <Pressable onPress={() => setOpen(false)} hitSlop={12}>
                <MaterialCommunityIcons name="close" size={20} color={c.textMuted} />
              </Pressable>
            </View>

            <ScrollView contentContainerStyle={styles.body}>
              {/* Metric explanations — what the columns in each row mean */}
              <ThemedText style={[styles.sectionLabel, { color: c.textMuted }]}>
                {t('legend.metricsHeader')}
              </ThemedText>
              {METRIC_KEYS.map((m) => (
                <View
                  key={m.short}
                  style={[styles.metricRow, { borderTopColor: c.border }]}>
                  <View style={styles.metricHead}>
                    <ThemedText style={[styles.short, { color: c.brand }]}>
                      {m.short}
                    </ThemedText>
                    <ThemedText style={[styles.long, { color: c.text }]}>
                      {t(`legend.metrics.${m.key}.long`)}
                    </ThemedText>
                  </View>
                  <ThemedText
                    style={[styles.description, { color: c.textMuted }]}>
                    {t(`legend.metrics.${m.key}.description`)}
                  </ThemedText>
                </View>
              ))}

              <ThemedText
                style={[
                  styles.sectionLabel,
                  styles.sectionLabelSpaced,
                  { color: c.textMuted },
                ]}>
                {t('legend.marketsHeader')}
              </ThemedText>
              {MARKET_KEYS.map((e) => (
                <View
                  key={e.short}
                  style={[styles.row, { borderTopColor: c.border }]}>
                  <ThemedText style={[styles.short, { color: c.text }]}>
                    {e.short}
                  </ThemedText>
                  <ThemedText style={[styles.long, { color: c.textMuted }]}>
                    {t(`legend.markets.${e.key}`)}
                  </ThemedText>
                </View>
              ))}
            </ScrollView>
          </Pressable>
        </Pressable>
      </Modal>
    </>
  );
}

const styles = StyleSheet.create({
  iconBtn: {
    width: 32,
    height: 32,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
  },
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.5)',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  sheet: {
    width: '100%',
    maxWidth: 380,
    maxHeight: '85%',
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
  },
  sheetHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  sheetTitle: {
    fontSize: 15,
    fontWeight: '700',
  },
  body: {
    paddingBottom: 12,
  },
  sectionLabel: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.6,
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 4,
  },
  sectionLabelSpaced: {
    paddingTop: 16,
  },
  metricRow: {
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 4,
  },
  metricHead: {
    flexDirection: 'row',
    alignItems: 'baseline',
    gap: 8,
  },
  description: {
    fontSize: 12,
    lineHeight: 17,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 12,
  },
  short: {
    width: 56,
    fontSize: 13,
    fontWeight: '700',
    letterSpacing: 0.4,
  },
  long: {
    flex: 1,
    fontSize: 13,
  },
});
