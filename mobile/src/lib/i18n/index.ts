import * as Localization from 'expo-localization';
import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import {
  getSettings,
  subscribeSettings,
  type LanguageMode,
} from '@/src/lib/settings/settingsStore';

import en from './locales/en.json';
import tr from './locales/tr.json';

export type Locale = 'en' | 'tr';

const SUPPORTED: Locale[] = ['en', 'tr'];

function detectDeviceLocale(): Locale {
  const candidates = Localization.getLocales();
  for (const c of candidates) {
    const tag = (c.languageTag ?? c.languageCode ?? '').toLowerCase();
    if (tag.startsWith('tr')) return 'tr';
    if (tag.startsWith('en')) return 'en';
  }
  return 'en';
}

// Combines user override (Settings → Language) with the device locale.
// 'system' falls back to detection; explicit en/tr always wins.
function resolveLocale(mode: LanguageMode): Locale {
  if (mode === 'en' || mode === 'tr') return mode;
  return detectDeviceLocale();
}

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      tr: { translation: tr },
    },
    lng: resolveLocale(getSettings().languageMode),
    fallbackLng: 'en',
    supportedLngs: SUPPORTED,
    interpolation: { escapeValue: false },
    returnNull: false,
  });

// Re-sync i18n whenever the user picks a new language in Settings. Cheap
// no-op if the resolved locale didn't actually change.
subscribeSettings(() => {
  const target = resolveLocale(getSettings().languageMode);
  if (i18n.language !== target) {
    void i18n.changeLanguage(target);
  }
});

export { i18n };
