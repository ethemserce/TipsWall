import * as Localization from 'expo-localization';
import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import en from './locales/en.json';
import tr from './locales/tr.json';

export type Locale = 'en' | 'tr';

const SUPPORTED: Locale[] = ['en', 'tr'];

function detectLocale(): Locale {
  const candidates = Localization.getLocales();
  for (const c of candidates) {
    const tag = (c.languageTag ?? c.languageCode ?? '').toLowerCase();
    if (tag.startsWith('tr')) return 'tr';
    if (tag.startsWith('en')) return 'en';
  }
  return 'en';
}

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      tr: { translation: tr },
    },
    lng: detectLocale(),
    fallbackLng: 'en',
    supportedLngs: SUPPORTED,
    interpolation: { escapeValue: false },
    returnNull: false,
  });

export { i18n };
