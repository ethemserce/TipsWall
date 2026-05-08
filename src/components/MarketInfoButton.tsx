import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useState } from 'react';
import { Modal, Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

interface MarketDoc {
  short: string;
  title: string;
  blurb: string;
  outcomes: { key: string; meaning: string }[];
}

// Market explanations the mobile app surfaces. Keep aligned with the
// market shortcodes used in RateMatchCard.formatLabel.
const MARKETS: Record<number, MarketDoc> = {
  1: {
    short: 'MS',
    title: 'Maç Sonucu',
    blurb: 'Maçın 90 dakika bitimindeki sonucu. Uzatma ve penaltılar sayılmaz.',
    outcomes: [
      { key: '1', meaning: 'Ev sahibi galip' },
      { key: 'X', meaning: 'Beraberlik' },
      { key: '2', meaning: 'Deplasman galip' },
    ],
  },
  10: {
    short: 'DNB',
    title: 'Beraberlikte İade',
    blurb: 'Maç berabere biterse bahis iade edilir; aksi halde tahmin kazanır ya da kaybeder.',
    outcomes: [
      { key: '1', meaning: 'Ev sahibi galip' },
      { key: '2', meaning: 'Deplasman galip' },
    ],
  },
  14: {
    short: 'KG',
    title: 'Karşılıklı Gol',
    blurb: 'Maçta iki takımın da gol atıp atmayacağı.',
    outcomes: [
      { key: 'Var', meaning: 'İki takım da en az 1 gol atar' },
      { key: 'Yok', meaning: 'En az bir takım gol atamaz' },
    ],
  },
  18: {
    short: 'EV',
    title: 'Ev Sahibi Tam Skor',
    blurb: 'Ev sahibinin tam olarak kaç gol attığı.',
    outcomes: [
      { key: '0 / 1 / 2 / 3+', meaning: 'Ev sahibinin attığı gol sayısı (3+ üç ve üstü)' },
    ],
  },
  19: {
    short: 'DEP',
    title: 'Deplasman Tam Skor',
    blurb: 'Deplasman takımının tam olarak kaç gol attığı.',
    outcomes: [
      { key: '0 / 1 / 2 / 3+', meaning: 'Deplasmanın attığı gol sayısı' },
    ],
  },
  31: {
    short: 'İY MS',
    title: 'İlk Yarı Sonucu',
    blurb: 'İlk yarı (45 dk) bitimindeki sonuç.',
    outcomes: [
      { key: '1', meaning: 'Ev sahibi önde' },
      { key: 'X', meaning: 'Beraberlik' },
      { key: '2', meaning: 'Deplasman önde' },
    ],
  },
  33: {
    short: 'İY',
    title: '1. Yarı Tam Skor',
    blurb: 'İlk yarıda atılan toplam gol sayısı.',
    outcomes: [
      { key: '0 / 1 / 2 / 3 / 4 / 5+', meaning: 'İlk yarıda toplam gol' },
    ],
  },
  38: {
    short: '2Y',
    title: '2. Yarı Tam Skor',
    blurb: 'İkinci yarıda atılan toplam gol sayısı.',
    outcomes: [
      { key: '0 / 1 / 2 / 3 / 4 / 5+', meaning: 'İkinci yarıda toplam gol' },
    ],
  },
  44: {
    short: 'T/Ç',
    title: 'Tek / Çift',
    blurb: 'Maçtaki toplam gol sayısının tek mi çift mi olduğu.',
    outcomes: [
      { key: 'Tek', meaning: 'Toplam gol 1, 3, 5...' },
      { key: 'Çift', meaning: 'Toplam gol 0, 2, 4...' },
    ],
  },
  52: {
    short: 'EV/DEP',
    title: 'Ev Sahibi / Deplasman',
    blurb: 'Beraberlik dikkate alınmaz; sadece galibiyet.',
    outcomes: [
      { key: 'Ev', meaning: 'Ev sahibi galip' },
      { key: 'Dep', meaning: 'Deplasman galip' },
    ],
  },
  80: {
    short: 'A/Ü',
    title: 'Toplam Gol Alt / Üst',
    blurb: 'Maçtaki toplam gol sayısının verilen çizgiyi geçip geçmediği.',
    outcomes: [
      { key: 'Üst (Over)', meaning: 'Toplam gol > çizgi' },
      { key: 'Alt (Under)', meaning: 'Toplam gol < çizgi' },
    ],
  },
};

interface MarketInfoButtonProps {
  marketId: number;
  fallbackName?: string | null;
}

export function MarketInfoButton({
  marketId,
  fallbackName,
}: MarketInfoButtonProps) {
  const c = useTheme();
  const [open, setOpen] = useState(false);
  const doc = MARKETS[marketId];

  return (
    <>
      <Pressable
        onPress={() => setOpen(true)}
        hitSlop={8}
        style={({ pressed }) => [
          styles.iconBtn,
          { backgroundColor: pressed ? c.bg : 'transparent' },
        ]}>
        <MaterialCommunityIcons
          name="information-outline"
          size={16}
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
              <View style={styles.titleBlock}>
                <ThemedText style={[styles.sheetTitle, { color: c.text }]}>
                  {doc?.title ?? fallbackName ?? `Market #${marketId}`}
                </ThemedText>
                {doc ? (
                  <ThemedText style={[styles.sheetShort, { color: c.brand }]}>
                    {doc.short}
                  </ThemedText>
                ) : null}
              </View>
              <Pressable onPress={() => setOpen(false)} hitSlop={12}>
                <MaterialCommunityIcons name="close" size={20} color={c.textMuted} />
              </Pressable>
            </View>

            {doc ? (
              <>
                <ThemedText style={[styles.blurb, { color: c.text }]}>
                  {doc.blurb}
                </ThemedText>
                {doc.outcomes.map((o, i) => (
                  <View
                    key={i}
                    style={[styles.outcomeRow, { borderTopColor: c.border }]}>
                    <ThemedText style={[styles.outcomeKey, { color: c.text }]}>
                      {o.key}
                    </ThemedText>
                    <ThemedText style={[styles.outcomeMeaning, { color: c.textMuted }]}>
                      {o.meaning}
                    </ThemedText>
                  </View>
                ))}
              </>
            ) : (
              <ThemedText style={[styles.blurb, { color: c.textMuted }]}>
                Bu market için açıklama henüz eklenmemiş.
              </ThemedText>
            )}
          </Pressable>
        </Pressable>
      </Modal>
    </>
  );
}

const styles = StyleSheet.create({
  iconBtn: {
    width: 26,
    height: 26,
    borderRadius: 13,
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
    maxWidth: 360,
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    overflow: 'hidden',
  },
  sheetHeader: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingTop: 14,
    paddingBottom: 10,
  },
  titleBlock: {
    flexShrink: 1,
    paddingRight: 8,
  },
  sheetTitle: {
    fontSize: 15,
    fontWeight: '700',
  },
  sheetShort: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.5,
    marginTop: 2,
  },
  blurb: {
    fontSize: 13,
    lineHeight: 19,
    paddingHorizontal: 16,
    paddingBottom: 10,
  },
  outcomeRow: {
    flexDirection: 'row',
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderTopWidth: StyleSheet.hairlineWidth,
    gap: 12,
  },
  outcomeKey: {
    width: 110,
    fontSize: 12,
    fontWeight: '700',
  },
  outcomeMeaning: {
    flex: 1,
    fontSize: 12,
    lineHeight: 18,
  },
});
