import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useEffect, useState } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { Slider } from '@/src/components/Slider';
import type { WindowCode, RateBound } from '@/src/components/RateFilterBar';
import { useTheme } from '@/src/lib/useTheme';

export interface AnalysisFilterState {
  // Numeric thresholds: 0 means "off" / no filter (handled by the screen
  // before passing to API). KZ default 3 is the system minimum sample.
  dsoMin: number;
  vbetMin: number;
  kzMin: number;
  valueOnly: boolean;
  // null = show every signal; numeric = cap each fixture to its top-N rows
  // by the active sort. Defaults to 3 so the list stays digestible on open.
  topPerFixture: number | null;
  window: WindowCode;
  rateValue: number | null;
  rateBound: RateBound;
}

export const DEFAULT_FILTERS: AnalysisFilterState = {
  dsoMin: 0,
  vbetMin: 0,
  kzMin: 3,
  valueOnly: false,
  topPerFixture: 3,
  window: 'all',
  rateValue: null,
  rateBound: 'min',
};

/**
 * Preset starting points the user can one-tap into the filter form. Each
 * preset writes a complete filter set (everything not listed falls back to
 * DEFAULT_FILTERS) — the user can then tweak and tap UYGULA. We don't
 * auto-apply because the preset is a *starting point*, not a verdict.
 */
export type StrategyPreset = 'banker' | 'value' | 'longshot';

export const PRESETS: Record<
  StrategyPreset,
  { label: string; description: string; filters: AnalysisFilterState }
> = {
  banker: {
    label: 'Banker',
    description: 'Düşük oran, yüksek isabet — sağlam ayak',
    filters: {
      ...DEFAULT_FILTERS,
      dsoMin: 60,
      kzMin: 5,
      valueOnly: true,
      window: '6m',
      rateValue: 1.8,
      rateBound: 'max',
    },
  },
  value: {
    label: 'Değer',
    description: 'Bahisçi düşük fiyatlamış — matematik avantajı',
    filters: {
      ...DEFAULT_FILTERS,
      dsoMin: 50,
      kzMin: 5,
      valueOnly: true,
      window: '1y',
    },
  },
  longshot: {
    label: 'Yüksek Oran',
    description: 'Az tutmuş ama tuttuğunda büyük katkı',
    filters: {
      ...DEFAULT_FILTERS,
      dsoMin: 30,
      kzMin: 3,
      valueOnly: true,
      window: 'all',
      rateValue: 2.5,
      rateBound: 'min',
    },
  },
};

function filtersEqual(a: AnalysisFilterState, b: AnalysisFilterState): boolean {
  return (
    a.dsoMin === b.dsoMin &&
    a.vbetMin === b.vbetMin &&
    a.kzMin === b.kzMin &&
    a.valueOnly === b.valueOnly &&
    a.topPerFixture === b.topPerFixture &&
    a.window === b.window &&
    a.rateValue === b.rateValue &&
    a.rateBound === b.rateBound
  );
}

function activePreset(f: AnalysisFilterState): StrategyPreset | null {
  for (const key of Object.keys(PRESETS) as StrategyPreset[]) {
    if (filtersEqual(f, PRESETS[key].filters)) return key;
  }
  return null;
}

interface AnalysisFiltersSheetProps {
  visible: boolean;
  filters: AnalysisFilterState;
  /** Fires only when the user taps UYGULA. Closing via backdrop discards. */
  onApply: (next: AnalysisFilterState) => void;
  onClose: () => void;
}

const WINDOWS: { key: WindowCode; label: string }[] = [
  { key: '1m', label: '1A' },
  { key: '3m', label: '3A' },
  { key: '6m', label: '6A' },
  { key: '1y', label: '1Y' },
  { key: 'all', label: 'Tümü' },
];

const RATE_VALUES: number[] = [1.5, 1.8, 2.5, 4.0, 10.0];

export function AnalysisFiltersSheet({
  visible,
  filters,
  onApply,
  onClose,
}: AnalysisFiltersSheetProps) {
  const c = useTheme();

  // Edits live in a local draft. Parent (committed) state only updates when
  // the user taps UYGULA; closing via backdrop / handle discards. Each open
  // syncs the draft to whatever the parent currently has.
  const [draft, setDraft] = useState<AnalysisFilterState>(filters);
  useEffect(() => {
    if (visible) setDraft(filters);
  }, [visible, filters]);

  const set = <K extends keyof AnalysisFilterState>(
    key: K,
    value: AnalysisFilterState[K],
  ) => setDraft((prev) => ({ ...prev, [key]: value }));

  const reset = () => setDraft(DEFAULT_FILTERS);
  const apply = () => {
    onApply(draft);
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
          // Inner pressable swallows taps so they don't dismiss the sheet.
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
              Filtreler
            </ThemedText>
            <Pressable onPress={reset} hitSlop={8}>
              <ThemedText style={[styles.resetText, { color: c.textMuted }]}>
                SIFIRLA
              </ThemedText>
            </Pressable>
          </View>

          <ScrollView contentContainerStyle={styles.body}>
            {/* Strategy presets — one-tap starting points. Filling the draft
                lets the user fine-tune before applying. */}
            <View style={styles.group}>
              <ThemedText style={[styles.groupLabel, { color: c.textMuted }]}>
                STRATEJİ
              </ThemedText>
              <View style={styles.presetRow}>
                {(Object.keys(PRESETS) as StrategyPreset[]).map((key) => {
                  const preset = PRESETS[key];
                  const active = activePreset(draft) === key;
                  return (
                    <Pressable
                      key={key}
                      onPress={() => setDraft(preset.filters)}
                      style={[
                        styles.presetChip,
                        {
                          borderColor: active ? c.brand : c.border,
                          backgroundColor: active ? c.brand : 'transparent',
                        },
                      ]}>
                      <ThemedText
                        style={[
                          styles.presetLabel,
                          { color: active ? c.textInverse : c.text },
                        ]}>
                        {preset.label}
                      </ThemedText>
                      <ThemedText
                        style={[
                          styles.presetDesc,
                          {
                            color: active
                              ? c.textInverse
                              : c.textMuted,
                          },
                        ]}
                        numberOfLines={2}>
                        {preset.description}
                      </ThemedText>
                    </Pressable>
                  );
                })}
              </View>
            </View>

            {/* Top-3 per fixture toggle — keeps the headline list digestible */}
            <Pressable
              onPress={() =>
                set(
                  'topPerFixture',
                  draft.topPerFixture == null ? 3 : null,
                )
              }
              style={[
                styles.valueRow,
                {
                  backgroundColor: c.bg,
                  borderColor: c.border,
                },
              ]}>
              <MaterialCommunityIcons
                name={
                  draft.topPerFixture != null
                    ? 'numeric-3-circle'
                    : 'numeric-3-circle-outline'
                }
                size={18}
                color={draft.topPerFixture != null ? c.brand : c.textMuted}
              />
              <View style={styles.valueText}>
                <ThemedText style={[styles.valueTitle, { color: c.text }]}>
                  Maç başına en iyi 3
                </ThemedText>
                <ThemedText style={[styles.valueSubtitle, { color: c.textMuted }]}>
                  Her maç için sıralamada en yüksek 3 öneri
                </ThemedText>
              </View>
              <View
                style={[
                  styles.toggle,
                  {
                    backgroundColor:
                      draft.topPerFixture != null ? c.brand : 'transparent',
                    borderColor:
                      draft.topPerFixture != null ? c.brand : c.border,
                  },
                ]}>
                {draft.topPerFixture != null ? (
                  <View
                    style={[
                      styles.toggleDot,
                      { backgroundColor: c.textInverse },
                    ]}
                  />
                ) : null}
              </View>
            </Pressable>

            {/* Value-only toggle */}
            <Pressable
              onPress={() => set('valueOnly', !draft.valueOnly)}
              style={[
                styles.valueRow,
                {
                  backgroundColor: draft.valueOnly ? c.brand : c.bg,
                  borderColor: draft.valueOnly ? c.brand : c.border,
                },
              ]}>
              <MaterialCommunityIcons
                name={draft.valueOnly ? 'star' : 'star-outline'}
                size={18}
                color={draft.valueOnly ? c.textInverse : c.textMuted}
              />
              <View style={styles.valueText}>
                <ThemedText
                  style={[
                    styles.valueTitle,
                    { color: draft.valueOnly ? c.textInverse : c.text },
                  ]}>
                  Sadece değer
                </ThemedText>
                <ThemedText
                  style={[
                    styles.valueSubtitle,
                    {
                      color: draft.valueOnly
                        ? c.textInverse
                        : c.textMuted,
                    },
                  ]}>
                  DSO &gt; İKO koşulunu sağlayan oranlar
                </ThemedText>
              </View>
              <View
                style={[
                  styles.toggle,
                  {
                    backgroundColor: draft.valueOnly
                      ? c.textInverse
                      : 'transparent',
                    borderColor: draft.valueOnly
                      ? c.textInverse
                      : c.border,
                  },
                ]}>
                {draft.valueOnly ? (
                  <View
                    style={[styles.toggleDot, { backgroundColor: c.brand }]}
                  />
                ) : null}
              </View>
            </Pressable>

            <SliderRow
              label="DSO en az"
              value={draft.dsoMin}
              min={0}
              max={100}
              step={5}
              format={(v) => `≥ %${v}`}
              hint={draft.dsoMin === 0 ? 'kapalı' : null}
              onChange={(v) => set('dsoMin', v)}
            />
            <SliderRow
              label="VBET en az"
              value={draft.vbetMin}
              min={0}
              max={100}
              step={5}
              format={(v) => `≥ %${v}`}
              hint={draft.vbetMin === 0 ? 'kapalı' : null}
              onChange={(v) => set('vbetMin', v)}
            />
            <SliderRow
              label="KZ (örneklem) en az"
              value={draft.kzMin}
              min={1}
              max={10}
              step={1}
              format={(v) => `≥ ${v}`}
              hint={null}
              onChange={(v) => set('kzMin', v)}
            />

            {/* Period */}
            <View style={styles.group}>
              <ThemedText style={[styles.groupLabel, { color: c.textMuted }]}>
                PERİYOT
              </ThemedText>
              <View style={[styles.segmented, { borderColor: c.border, backgroundColor: c.bg }]}>
                {WINDOWS.map((w) => {
                  const active = draft.window === w.key;
                  return (
                    <Pressable
                      key={w.key}
                      onPress={() => set('window', w.key)}
                      style={[
                        styles.segment,
                        active && { backgroundColor: c.brand },
                      ]}>
                      <ThemedText
                        style={[
                          styles.segmentText,
                          { color: active ? c.textInverse : c.text },
                        ]}>
                        {w.label}
                      </ThemedText>
                    </Pressable>
                  );
                })}
              </View>
            </View>

            {/* Oran */}
            <View style={styles.group}>
              <View style={styles.oranHeader}>
                <ThemedText style={[styles.groupLabel, { color: c.textMuted }]}>
                  ORAN
                </ThemedText>
                <View style={[styles.boundToggle, { borderColor: c.border }]}>
                  {(['min', 'max'] as const).map((bound) => {
                    const active = draft.rateBound === bound;
                    return (
                      <Pressable
                        key={bound}
                        onPress={() => set('rateBound', bound)}
                        style={[
                          styles.boundPill,
                          active && { backgroundColor: c.brand },
                        ]}>
                        <ThemedText
                          style={[
                            styles.boundText,
                            { color: active ? c.textInverse : c.text },
                          ]}>
                          {bound === 'min' ? '≥ MIN' : '≤ MAX'}
                        </ThemedText>
                      </Pressable>
                    );
                  })}
                </View>
              </View>
              <View style={styles.rateChips}>
                <Pressable
                  onPress={() => set('rateValue', null)}
                  style={[
                    styles.rateChip,
                    { borderColor: c.border },
                    draft.rateValue == null && {
                      backgroundColor: c.brand,
                      borderColor: c.brand,
                    },
                  ]}>
                  <ThemedText
                    style={[
                      styles.rateChipText,
                      {
                        color:
                          draft.rateValue == null
                            ? c.textInverse
                            : c.text,
                      },
                    ]}>
                    Tümü
                  </ThemedText>
                </Pressable>
                {RATE_VALUES.map((v) => {
                  const active = draft.rateValue === v;
                  return (
                    <Pressable
                      key={v}
                      onPress={() => set('rateValue', v)}
                      style={[
                        styles.rateChip,
                        { borderColor: c.border },
                        active && {
                          backgroundColor: c.brand,
                          borderColor: c.brand,
                        },
                      ]}>
                      <ThemedText
                        style={[
                          styles.rateChipText,
                          { color: active ? c.textInverse : c.text },
                        ]}>
                        {v.toFixed(2)}
                      </ThemedText>
                    </Pressable>
                  );
                })}
              </View>
            </View>
          </ScrollView>

          <Pressable
            onPress={apply}
            style={[styles.applyBtn, { backgroundColor: c.brand }]}>
            <ThemedText style={[styles.applyText, { color: c.textInverse }]}>
              UYGULA
            </ThemedText>
          </Pressable>
        </Pressable>
      </Pressable>
    </Modal>
  );
}

function SliderRow({
  label,
  value,
  min,
  max,
  step,
  format,
  hint,
  onChange,
}: {
  label: string;
  value: number;
  min: number;
  max: number;
  step: number;
  format: (v: number) => string;
  hint: string | null;
  onChange: (v: number) => void;
}) {
  const c = useTheme();
  return (
    <View style={styles.group}>
      <View style={styles.sliderHeader}>
        <ThemedText style={[styles.groupLabel, { color: c.textMuted }]}>
          {label.toUpperCase()}
        </ThemedText>
        <ThemedText
          style={[
            styles.sliderValue,
            { color: hint ? c.textMuted : c.text },
          ]}>
          {hint ?? format(value)}
        </ThemedText>
      </View>
      <Slider
        min={min}
        max={max}
        step={step}
        value={value}
        onChange={onChange}
      />
    </View>
  );
}

/**
 * How many filter dimensions are active (compared to defaults). Used to
 * render a small badge next to the Filtre button.
 */
export function countActiveFilters(f: AnalysisFilterState): number {
  let n = 0;
  if (f.valueOnly) n++;
  if (f.dsoMin > 0) n++;
  if (f.vbetMin > 0) n++;
  if (f.kzMin !== DEFAULT_FILTERS.kzMin) n++;
  if (f.topPerFixture !== DEFAULT_FILTERS.topPerFixture) n++;
  if (f.window !== DEFAULT_FILTERS.window) n++;
  if (f.rateValue != null) n++;
  return n;
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.55)',
    justifyContent: 'flex-end',
  },
  sheet: {
    maxHeight: '90%',
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
    paddingBottom: 2,
  },
  title: {
    fontSize: 15,
    fontWeight: '700',
  },
  resetText: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.6,
  },
  body: {
    paddingHorizontal: 16,
    paddingVertical: 6,
    gap: 10,
  },
  valueRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingHorizontal: 10,
    paddingVertical: 8,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
  },
  valueText: {
    flex: 1,
  },
  valueTitle: {
    fontSize: 13,
    fontWeight: '700',
  },
  valueSubtitle: {
    fontSize: 10,
    fontWeight: '500',
    marginTop: 1,
  },
  toggle: {
    width: 20,
    height: 20,
    borderRadius: 10,
    borderWidth: 2,
    alignItems: 'center',
    justifyContent: 'center',
  },
  toggleDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
  },
  group: {
    gap: 2,
  },
  groupLabel: {
    fontSize: 10,
    fontWeight: '700',
    letterSpacing: 0.6,
  },
  sliderHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  sliderValue: {
    fontSize: 12,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  segmented: {
    flexDirection: 'row',
    borderRadius: 8,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 2,
    gap: 2,
  },
  segment: {
    flex: 1,
    paddingVertical: 5,
    borderRadius: 6,
    alignItems: 'center',
  },
  segmentText: {
    fontSize: 12,
    fontWeight: '700',
    letterSpacing: 0.3,
  },
  oranHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  boundToggle: {
    flexDirection: 'row',
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: 999,
    padding: 1,
  },
  boundPill: {
    paddingHorizontal: 9,
    paddingVertical: 3,
    borderRadius: 999,
  },
  boundText: {
    fontSize: 10,
    fontWeight: '800',
    letterSpacing: 0.5,
  },
  rateChips: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 6,
  },
  rateChip: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
    minWidth: 48,
    alignItems: 'center',
  },
  rateChipText: {
    fontSize: 11,
    fontWeight: '700',
    fontVariant: ['tabular-nums'],
  },
  presetRow: {
    flexDirection: 'row',
    gap: 6,
    marginTop: 4,
  },
  presetChip: {
    flex: 1,
    paddingHorizontal: 8,
    paddingVertical: 8,
    borderRadius: 10,
    borderWidth: 1,
    gap: 2,
  },
  presetLabel: {
    fontSize: 12,
    fontWeight: '800',
    letterSpacing: 0.3,
  },
  presetDesc: {
    fontSize: 9,
    fontWeight: '500',
    lineHeight: 12,
  },
  applyBtn: {
    marginTop: 6,
    marginHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 10,
    alignItems: 'center',
  },
  applyText: {
    fontSize: 13,
    fontWeight: '800',
    letterSpacing: 0.6,
  },
});
