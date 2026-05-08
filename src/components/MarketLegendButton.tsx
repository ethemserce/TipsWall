import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useState } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

// Metric column shortcodes used in card rows. Single source of truth so
// the analysis screen and the fixture detail share one explanation.
const METRICS: { short: string; long: string; description: string }[] = [
  {
    short: 'DSO',
    long: 'Doğru Sonuç Oranı',
    description:
      'Geçmiş örneklemde bu tahminin tutma yüzdesi. 5 maçtan 3’ünde tuttuysa %60. (winning_percent)',
  },
  {
    short: 'VBET',
    long: 'Beklenen Getiri (ROI)',
    description:
      '100 birim oynanmış olsa beklenen net kâr/zarar. Pozitif değer bahisçinin oranı düşük fiyatladığını gösterir. (earning_percent)',
  },
  {
    short: 'İKO',
    long: 'İmplied Olasılık',
    description:
      'Bahisçinin oranlardan çıkardığı, marj temizlenmiş olasılık. Aynı market içinde tüm sonuçlar 100’e tamamlanır. DSO İKO’dan büyükse "değer" vardır.',
  },
  {
    short: 'KZ / KY',
    long: 'Kazanan / Kaybeden',
    description:
      'Geçmiş örneklemde tutmuş ve tutmamış maç sayıları (win_count / lost_count).',
  },
];

const MARKETS: { short: string; long: string }[] = [
  { short: 'MS', long: 'Maç Sonucu' },
  { short: 'DNB', long: 'Beraberlikte iade (Draw No Bet)' },
  { short: 'KG', long: 'Karşılıklı Gol' },
  { short: 'EV', long: 'Ev Sahibi Tam Skor' },
  { short: 'DEP', long: 'Deplasman Tam Skor' },
  { short: 'İY', long: '1. Yarı Tam Skor' },
  { short: '2Y', long: '2. Yarı Tam Skor' },
  { short: 'T/Ç', long: 'Tek / Çift Gol' },
];

export function MarketLegendButton() {
  const c = useTheme();
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
                Açıklamalar
              </ThemedText>
              <Pressable onPress={() => setOpen(false)} hitSlop={12}>
                <MaterialCommunityIcons name="close" size={20} color={c.textMuted} />
              </Pressable>
            </View>

            <ScrollView contentContainerStyle={styles.body}>
              {/* Metric explanations — what the columns in each row mean */}
              <ThemedText style={[styles.sectionLabel, { color: c.textMuted }]}>
                METRİKLER
              </ThemedText>
              {METRICS.map((m) => (
                <View
                  key={m.short}
                  style={[styles.metricRow, { borderTopColor: c.border }]}>
                  <View style={styles.metricHead}>
                    <ThemedText style={[styles.short, { color: c.brand }]}>
                      {m.short}
                    </ThemedText>
                    <ThemedText style={[styles.long, { color: c.text }]}>
                      {m.long}
                    </ThemedText>
                  </View>
                  <ThemedText
                    style={[styles.description, { color: c.textMuted }]}>
                    {m.description}
                  </ThemedText>
                </View>
              ))}

              <ThemedText
                style={[
                  styles.sectionLabel,
                  styles.sectionLabelSpaced,
                  { color: c.textMuted },
                ]}>
                MARKET KISALTMALARI
              </ThemedText>
              {MARKETS.map((e) => (
                <View
                  key={e.short}
                  style={[styles.row, { borderTopColor: c.border }]}>
                  <ThemedText style={[styles.short, { color: c.text }]}>
                    {e.short}
                  </ThemedText>
                  <ThemedText style={[styles.long, { color: c.textMuted }]}>
                    {e.long}
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
