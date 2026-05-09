import MaterialCommunityIcons from '@expo/vector-icons/MaterialCommunityIcons';
import { useTranslation } from 'react-i18next';
import {
  Pressable,
  ScrollView,
  StyleSheet,
  Switch,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ThemedText } from '@/components/themed-text';
import { AppBrand } from '@/src/components/AppBrand';
import {
  setLanguageMode,
  setOddsHidden,
  setThemeMode,
  useSettings,
  type LanguageMode,
  type ThemeMode,
} from '@/src/lib/settings/settingsStore';
import { useTheme } from '@/src/lib/useTheme';

const THEME_MODES: { mode: ThemeMode; icon: 'cellphone' | 'white-balance-sunny' | 'weather-night' }[] = [
  { mode: 'system', icon: 'cellphone' },
  { mode: 'light', icon: 'white-balance-sunny' },
  { mode: 'dark', icon: 'weather-night' },
];

const LANGUAGE_MODES: { mode: LanguageMode }[] = [
  { mode: 'system' },
  { mode: 'en' },
  { mode: 'tr' },
];

export function SettingsScreen() {
  const c = useTheme();
  const { t } = useTranslation();
  const { themeMode, languageMode, oddsHidden } = useSettings();

  return (
    <SafeAreaView style={[styles.flex, { backgroundColor: c.bg }]} edges={['top']}>
      <View style={styles.headerRow}>
        <AppBrand />
      </View>

      <ScrollView contentContainerStyle={styles.scroll}>
        {/* Appearance — theme switcher */}
        <SectionHeader label={t('settings.appearance.header')} />
        <View
          style={[
            styles.card,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <View style={styles.rowHeader}>
            <ThemedText style={[styles.rowTitle, { color: c.text }]}>
              {t('settings.appearance.theme')}
            </ThemedText>
            <ThemedText style={[styles.rowHint, { color: c.textMuted }]}>
              {t('settings.appearance.themeHint')}
            </ThemedText>
          </View>
          <View style={[styles.segment, { backgroundColor: c.bg, borderColor: c.borderSoft }]}>
            {THEME_MODES.map(({ mode, icon }) => {
              const active = themeMode === mode;
              return (
                <Pressable
                  key={mode}
                  onPress={() => setThemeMode(mode)}
                  style={[
                    styles.segmentBtn,
                    active && { backgroundColor: c.brand },
                  ]}
                  accessibilityRole="button"
                  accessibilityState={{ selected: active }}>
                  <MaterialCommunityIcons
                    name={icon}
                    size={16}
                    color={active ? c.textInverse : c.textMuted}
                  />
                  <ThemedText
                    style={[
                      styles.segmentText,
                      { color: active ? c.textInverse : c.textMuted },
                    ]}>
                    {t(`settings.appearance.modes.${mode}`)}
                  </ThemedText>
                </Pressable>
              );
            })}
          </View>
        </View>

        {/* Language */}
        <SectionHeader label={t('settings.language.header')} />
        <View
          style={[
            styles.card,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <View style={styles.rowHeader}>
            <ThemedText style={[styles.rowTitle, { color: c.text }]}>
              {t('settings.language.label')}
            </ThemedText>
            <ThemedText style={[styles.rowHint, { color: c.textMuted }]}>
              {t('settings.language.hint')}
            </ThemedText>
          </View>
          <View style={[styles.segment, { backgroundColor: c.bg, borderColor: c.borderSoft }]}>
            {LANGUAGE_MODES.map(({ mode }) => {
              const active = languageMode === mode;
              return (
                <Pressable
                  key={mode}
                  onPress={() => setLanguageMode(mode)}
                  style={[
                    styles.segmentBtn,
                    active && { backgroundColor: c.brand },
                  ]}
                  accessibilityRole="button"
                  accessibilityState={{ selected: active }}>
                  <ThemedText
                    style={[
                      styles.segmentText,
                      { color: active ? c.textInverse : c.textMuted },
                    ]}>
                    {t(`settings.language.modes.${mode}`)}
                  </ThemedText>
                </Pressable>
              );
            })}
          </View>
        </View>

        {/* Privacy — odds visibility */}
        <SectionHeader label={t('settings.privacy.header')} />
        <View
          style={[
            styles.card,
            { backgroundColor: c.surfaceElevated, borderColor: c.borderSoft },
          ]}>
          <View style={styles.toggleRow}>
            <View style={styles.toggleText}>
              <ThemedText style={[styles.rowTitle, { color: c.text }]}>
                {t('settings.privacy.oddsHidden')}
              </ThemedText>
              <ThemedText style={[styles.rowHint, { color: c.textMuted }]}>
                {t('settings.privacy.oddsHiddenHint')}
              </ThemedText>
            </View>
            <Switch
              value={oddsHidden}
              onValueChange={setOddsHidden}
              trackColor={{ false: c.border, true: c.brand }}
              thumbColor={c.textInverse}
            />
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

function SectionHeader({ label }: { label: string }) {
  const c = useTheme();
  return (
    <ThemedText style={[styles.sectionHeader, { color: c.textMuted }]}>
      {label.toLocaleUpperCase('tr-TR')}
    </ThemedText>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1 },
  headerRow: {
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 4,
  },
  scroll: {
    paddingHorizontal: 16,
    paddingTop: 8,
    paddingBottom: 32,
    gap: 8,
  },
  sectionHeader: {
    fontSize: 11,
    fontWeight: '700',
    letterSpacing: 0.6,
    marginTop: 16,
    marginBottom: 6,
    paddingHorizontal: 4,
  },
  card: {
    borderRadius: 14,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 14,
    gap: 12,
  },
  rowHeader: {
    gap: 2,
  },
  rowTitle: {
    fontSize: 15,
    fontWeight: '600',
  },
  rowHint: {
    fontSize: 12,
    lineHeight: 16,
  },
  segment: {
    flexDirection: 'row',
    borderRadius: 999,
    borderWidth: StyleSheet.hairlineWidth,
    padding: 3,
    gap: 2,
  },
  segmentBtn: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 6,
    paddingVertical: 8,
    borderRadius: 999,
  },
  segmentText: {
    fontSize: 12,
    fontWeight: '600',
  },
  toggleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
  },
  toggleText: {
    flex: 1,
    gap: 2,
  },
});
