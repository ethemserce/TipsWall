import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal, Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { useTheme } from '@/src/lib/useTheme';

interface MarketDoc {
  short: string;
  title: string;
  blurb: string;
  outcomes: { key: string; meaning: string }[];
}

// Shortcodes are language-neutral betting glyphs (MS, KG, T/Ç) — they stay
// in code and pair with a language-specific lookup for title / blurb / outcomes
// pulled from the i18n bundle.
const MARKET_SHORTS: Record<number, string> = {
  1: 'MS',
  10: 'DNB',
  14: 'KG',
  18: 'EV',
  19: 'DEP',
  31: 'İY MS',
  33: 'İY',
  38: '2Y',
  44: 'T/Ç',
  52: 'EV/DEP',
  80: 'A/Ü',
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
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const short = MARKET_SHORTS[marketId];
  // i18next allows arrays/objects through with returnObjects. We default to
  // null when the key is missing so unknown marketIds render the fallback.
  const i18nKey = `markets.info.${marketId}`;
  const titleKey = `${i18nKey}.title`;
  const titleResolved = t(titleKey, { defaultValue: '' });
  const doc: MarketDoc | null =
    short && titleResolved
      ? {
          short,
          title: titleResolved,
          blurb: t(`${i18nKey}.blurb`, { defaultValue: '' }),
          outcomes: t(`${i18nKey}.outcomes`, {
            returnObjects: true,
            defaultValue: [],
          }) as { key: string; meaning: string }[],
        }
      : null;

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
                  {doc?.title ?? fallbackName ?? t('markets.fallback', { id: marketId })}
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
                {t('markets.noInfo')}
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
