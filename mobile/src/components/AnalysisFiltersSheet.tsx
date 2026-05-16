import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal, Pressable, ScrollView, StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { Slider } from '@/src/components/Slider';
import type { WindowCode } from '@/src/components/RateFilterBar';
import { useTheme } from '@/src/lib/useTheme';

/**
 * User-facing risk category. The backend still filters by min/max rate
 * (it's the only thing the snapshots key on), but the user never sees the
 * raw number — they pick a risk tier and the screen translates it.
 *
 * - low (Düşük):    maxRate 1.8 — favourites only, frequent hits
 * - mid (Dengeli):  1.8 ≤ rate ≤ 3.0 — middle of the spread
 * - high (Cüretkâr): minRate 3.0 — long shots, rare but big
 */
export type RiskCategory = 'low' | 'mid' | 'high';

export const RISK_THRESHOLDS: Record<
  RiskCategory,
  { minRate?: number; maxRate?: number }
> = {
  low: { maxRate: 1.8 },
  mid: { minRate: 1.8, maxRate: 3.0 },
  high: { minRate: 3.0 },
};

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
  riskCategory: RiskCategory | null;
}

export const DEFAULT_FILTERS: AnalysisFilterState = {
  dsoMin: 0,
  vbetMin: 0,
  kzMin: 3,
  valueOnly: false,
  topPerFixture: 3,
  // Default window now respects natural season boundaries — per-league
  // current season filter. Time-based '1m'/'3m'/'6m' kept as opt-in
  // chips; the deprecated 'all' / '1y' codes still type-check for old
  // persisted state but never become the default.
  window: 'season_current',
  riskCategory: null,
};

/**
 * Preset starting points the user can one-tap into the filter form. Each
 * preset writes a complete filter set (everything not listed falls back to
 * DEFAULT_FILTERS) — the user can then tweak and tap UYGULA. We don't
 * auto-apply because the preset is a *starting point*, not a verdict.
 */
export type StrategyPreset = 'banker' | 'value' | 'longshot';

// Preset definitions live as filter shapes only — labels + descriptions
// come from the i18n bundle at render time so the same preset speaks the
// user's language without having to duplicate the structure.
export const PRESETS: Record<StrategyPreset, { filters: AnalysisFilterState }> = {
  banker: {
    filters: {
      ...DEFAULT_FILTERS,
      dsoMin: 60,
      kzMin: 5,
      valueOnly: true,
      window: '6m',
      riskCategory: 'low',
    },
  },
  value: {
    filters: {
      ...DEFAULT_FILTERS,
      dsoMin: 50,
      kzMin: 5,
      valueOnly: true,
      // 2-season sample size for the value preset — broader history,
      // still respecting current bookmaker regime (no 5+ year decay).
      window: 'season_2y',
    },
  },
  longshot: {
    filters: {
      ...DEFAULT_FILTERS,
      dsoMin: 30,
      kzMin: 3,
      valueOnly: true,
      window: 'season_2y',
      riskCategory: 'high',
    },
  },
};

// Rule A: with "Sadece Değer" on, DSO > İKO claims need real samples to
// avoid noise. 3 is the system floor — anything less is a coin flip.
const KZ_MIN_WITH_VALUE = 3;
// Rule B: short windows can't fill big sample buckets, so cap the KZ slider
// upper bound to a reachable value per window. Beyond these caps the user
// would be filtering for outcomes that mathematically can't exist.
const KZ_MAX_BY_WINDOW: Record<WindowCode, number> = {
  '1m': 5,
  '3m': 7,
  '6m': 8,
  '1y': 10,
  all: 10,
  // Season windows carry meaningfully larger samples than 1-3 months
  // but the rolling cap stays modest — most users want "≥5 samples"
  // type asks; >15 is rarely reachable per outcome.
  season_current: 10,
  season_2y: 15,
};

// Hard floor of 3 — below that the win/loss split isn't statistically
// meaningful (0/1 or 1/0 makes the hit-rate jump 0 → 100%). Same number
// as KZ_MIN_WITH_VALUE so value-only mode falls through to the same
// minimum; user feedback was that lower samples produced noisy picks.
const KZ_MIN_BASE = 3;

function kzFloor(state: AnalysisFilterState): number {
  return Math.max(KZ_MIN_BASE, state.valueOnly ? KZ_MIN_WITH_VALUE : 0);
}

function kzCeiling(state: AnalysisFilterState): number {
  return KZ_MAX_BY_WINDOW[state.window];
}

function clampKz(state: AnalysisFilterState): AnalysisFilterState {
  const floor = kzFloor(state);
  const ceiling = kzCeiling(state);
  const next = Math.min(ceiling, Math.max(floor, state.kzMin));
  return next === state.kzMin ? state : { ...state, kzMin: next };
}

function filtersEqual(a: AnalysisFilterState, b: AnalysisFilterState): boolean {
  return (
    a.dsoMin === b.dsoMin &&
    a.vbetMin === b.vbetMin &&
    a.kzMin === b.kzMin &&
    a.valueOnly === b.valueOnly &&
    a.topPerFixture === b.topPerFixture &&
    a.window === b.window &&
    a.riskCategory === b.riskCategory
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

// Period chip labels come from rate.filters.windows.* — same abbreviation
// scheme the legacy RateFilterBar uses. Resolved lazily inside the
// component so the active language wins on re-render.
// Five user-visible windows. Time-based first (sample by recency),
// then season-based (sample by competition cycle). '1y' / 'all' are
// dropped here because they conflate stale bookmaker pricing regimes;
// the codes still exist in the WindowCode union for back-compat with
// older persisted filter state.
const WINDOW_KEYS: WindowCode[] = [
  '1m',
  '3m',
  '6m',
  'season_current',
  'season_2y',
];

const RISK_CATEGORIES: RiskCategory[] = ['low', 'mid', 'high'];

export function AnalysisFiltersSheet({
  visible,
  filters,
  onApply,
  onClose,
}: AnalysisFiltersSheetProps) {
  const c = useTheme();
  const { t } = useTranslation();
  // Android gesture / nav bar lives inside the modal frame, so the sheet's
  // own paddingBottom has to clear it. iOS reports 34 here for the home
  // indicator; both flow through useSafeAreaInsets.
  const insets = useSafeAreaInsets();

  // Edits live in a local draft. Parent (committed) state only updates when
  // the user taps UYGULA; closing via backdrop / handle discards. Each open
  // syncs the draft to whatever the parent currently has.
  const [draft, setDraft] = useState<AnalysisFilterState>(filters);
  // Rule C: remember which preset the user last tapped so we can mark its
  // chip as "modified" once they've started tweaking. activePreset(draft)
  // alone can't tell us that — it returns null the moment any field drifts.
  const [appliedPreset, setAppliedPreset] = useState<StrategyPreset | null>(
    null,
  );
  useEffect(() => {
    if (visible) {
      setDraft(filters);
      setAppliedPreset(activePreset(filters));
    }
  }, [visible, filters]);

  const set = <K extends keyof AnalysisFilterState>(
    key: K,
    value: AnalysisFilterState[K],
  ) =>
    setDraft((prev) => {
      const next = { ...prev, [key]: value };
      // Rule A/B: KZ floor depends on valueOnly, ceiling on window — touch
      // either and the KZ slider may need to slide along.
      return clampKz(next);
    });

  const applyPreset = (key: StrategyPreset) => {
    setAppliedPreset(key);
    setDraft(clampKz(PRESETS[key].filters));
  };

  const reset = () => {
    setAppliedPreset(null);
    setDraft(DEFAULT_FILTERS);
  };
  const apply = () => {
    onApply(draft);
    onClose();
  };

  const currentPreset = activePreset(draft);
  const draftKzFloor = kzFloor(draft);
  const draftKzCeiling = kzCeiling(draft);

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
              {t('filters.title')}
            </ThemedText>
            <Pressable onPress={reset} hitSlop={8}>
              <ThemedText style={[styles.resetText, { color: c.textMuted }]}>
                {t('filters.reset')}
              </ThemedText>
            </Pressable>
          </View>

          <ScrollView contentContainerStyle={styles.body}>
            {/* Strategy presets — one-tap starting points. Filling the draft
                lets the user fine-tune before applying. */}
            <View style={styles.group}>
              <ThemedText style={[styles.groupLabel, { color: c.textMuted }]}>
                {t('filters.strategy')}
              </ThemedText>
              <View style={styles.presetRow}>
                {(Object.keys(PRESETS) as StrategyPreset[]).map((key) => {
                  const active = currentPreset === key;
                  // Rule C: chip shows three visual states — inactive
                  // (plain), active (filled brand), and "modified" (last
                  // applied but the user has since edited a field). The
                  // modified state carries a dot so the user can tell at a
                  // glance that the preset is the *starting point*, not
                  // the verdict.
                  const modified = !active && appliedPreset === key;
                  return (
                    <Pressable
                      key={key}
                      onPress={() => applyPreset(key)}
                      style={[
                        styles.presetChip,
                        {
                          borderColor: active || modified ? c.brand : c.border,
                          backgroundColor: active ? c.brand : 'transparent',
                          borderStyle: modified ? 'dashed' : 'solid',
                        },
                      ]}>
                      <View style={styles.presetLabelRow}>
                        <ThemedText
                          style={[
                            styles.presetLabel,
                            { color: active ? c.textInverse : c.text },
                          ]}>
                          {t(`filters.presets.${key}.label`)}
                        </ThemedText>
                        {modified ? (
                          <View
                            style={[
                              styles.modifiedDot,
                              { backgroundColor: c.brand },
                            ]}
                          />
                        ) : null}
                      </View>
                      <ThemedText
                        style={[
                          styles.presetDesc,
                          {
                            color: active ? c.textInverse : c.textMuted,
                          },
                        ]}
                        numberOfLines={2}>
                        {modified
                          ? t('filters.presets.modifiedHint')
                          : t(`filters.presets.${key}.description`)}
                      </ThemedText>
                    </Pressable>
                  );
                })}
              </View>
            </View>

            {/* Top-3 per fixture toggle — keeps the headline list digestible */}
            <Pressable
              onPress={() =>
                set('topPerFixture', draft.topPerFixture == null ? 3 : null)
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
                  {t('filters.topPerFixture.title')}
                </ThemedText>
                <ThemedText style={[styles.valueSubtitle, { color: c.textMuted }]}>
                  {t('filters.topPerFixture.subtitle')}
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
                  {t('filters.valueOnly.title')}
                </ThemedText>
                <ThemedText
                  style={[
                    styles.valueSubtitle,
                    {
                      color: draft.valueOnly ? c.textInverse : c.textMuted,
                    },
                  ]}>
                  {t('filters.valueOnly.subtitle')}
                </ThemedText>
              </View>
              <View
                style={[
                  styles.toggle,
                  {
                    backgroundColor: draft.valueOnly
                      ? c.textInverse
                      : 'transparent',
                    borderColor: draft.valueOnly ? c.textInverse : c.border,
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
              label={t('filters.sliders.dsoMin')}
              value={draft.dsoMin}
              min={0}
              max={100}
              step={5}
              format={(v) => `≥ %${v}`}
              hint={draft.dsoMin === 0 ? t('filters.sliders.off') : null}
              onChange={(v) => set('dsoMin', v)}
            />
            <SliderRow
              label={t('filters.sliders.vbetMin')}
              value={draft.vbetMin}
              min={0}
              max={100}
              step={5}
              format={(v) => `≥ %${v}`}
              hint={draft.vbetMin === 0 ? t('filters.sliders.off') : null}
              onChange={(v) => set('vbetMin', v)}
            />
            <SliderRow
              label={t('filters.sliders.kzMin')}
              value={draft.kzMin}
              min={draftKzFloor}
              max={draftKzCeiling}
              step={1}
              format={(v) => `≥ ${v}`}
              hint={
                draft.valueOnly && draft.kzMin === KZ_MIN_WITH_VALUE
                  ? t('filters.sliders.kzMinValueLocked')
                  : draftKzCeiling < 10
                    ? t('filters.sliders.kzMaxWindowLocked', {
                        max: draftKzCeiling,
                      })
                    : null
              }
              onChange={(v) => set('kzMin', v)}
            />

            {/* Period */}
            <View style={styles.group}>
              <ThemedText style={[styles.groupLabel, { color: c.textMuted }]}>
                {t('filters.period')}
              </ThemedText>
              <View style={[styles.segmented, { borderColor: c.border, backgroundColor: c.bg }]}>
                {WINDOW_KEYS.map((key) => {
                  const active = draft.window === key;
                  return (
                    <Pressable
                      key={key}
                      onPress={() => set('window', key)}
                      style={[
                        styles.segment,
                        active && { backgroundColor: c.brand },
                      ]}>
                      <ThemedText
                        style={[
                          styles.segmentText,
                          { color: active ? c.textInverse : c.text },
                        ]}>
                        {t(`rate.filters.windows.${key}`)}
                      </ThemedText>
                    </Pressable>
                  );
                })}
              </View>
            </View>

            {/* Risk kategorisi — the user-visible replacement for the old
                min/max oran chips. Each option maps to a min/max rate range
                on the wire (see RISK_THRESHOLDS) without ever surfacing the
                raw oran number. */}
            <View style={styles.group}>
              <ThemedText style={[styles.groupLabel, { color: c.textMuted }]}>
                {t('filters.risk.header')}
              </ThemedText>
              <View style={styles.riskRow}>
                <Pressable
                  onPress={() => set('riskCategory', null)}
                  style={[
                    styles.riskChip,
                    { borderColor: c.border },
                    draft.riskCategory == null && {
                      backgroundColor: c.brand,
                      borderColor: c.brand,
                    },
                  ]}>
                  <ThemedText
                    style={[
                      styles.riskLabel,
                      {
                        color:
                          draft.riskCategory == null
                            ? c.textInverse
                            : c.text,
                      },
                    ]}>
                    {t('filters.risk.all.label')}
                  </ThemedText>
                </Pressable>
                {RISK_CATEGORIES.map((key) => {
                  const active = draft.riskCategory === key;
                  return (
                    <Pressable
                      key={key}
                      onPress={() => set('riskCategory', key)}
                      style={[
                        styles.riskChip,
                        { borderColor: c.border },
                        active && {
                          backgroundColor: c.brand,
                          borderColor: c.brand,
                        },
                      ]}>
                      <ThemedText
                        style={[
                          styles.riskLabel,
                          { color: active ? c.textInverse : c.text },
                        ]}>
                        {t(`filters.risk.${key}.label`)}
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
              {t('filters.apply')}
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
  if (f.riskCategory != null) n++;
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
  riskRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 6,
    marginTop: 4,
  },
  riskChip: {
    flexGrow: 1,
    flexBasis: 0,
    minWidth: 80,
    paddingHorizontal: 10,
    paddingVertical: 6,
    borderRadius: 10,
    borderWidth: StyleSheet.hairlineWidth,
    alignItems: 'center',
  },
  riskLabel: {
    fontSize: 12,
    fontWeight: '800',
    letterSpacing: 0.3,
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
  presetLabelRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  presetLabel: {
    fontSize: 12,
    fontWeight: '800',
    letterSpacing: 0.3,
  },
  modifiedDot: {
    width: 5,
    height: 5,
    borderRadius: 2.5,
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
