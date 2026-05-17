// Maps SportMonks-supplied English country names to their Turkish
// equivalents via ISO 3166-1 alpha-2 codes (the most stable id —
// SportMonks ships consistent iso2 values per country).
//
// Coverage focuses on football-active countries; the long tail (small
// nations rarely seen in SportMonks fixtures) keeps the English
// fallback for now. Adding a code is a single line in TR_BY_ISO2 + an
// EN entry would only be needed if we ever wanted to override
// SportMonks' English (we don't).
//
// Why iso2 instead of name-matching: SportMonks occasionally tweaks
// country names ("South Korea" vs "Korea Republic", "Czechia" vs
// "Czech Republic"); ISO codes don't drift.

import i18nInstance from 'i18next';

const TR_BY_ISO2: Record<string, string> = {
  // Europe
  TR: 'Türkiye',
  GB: 'İngiltere',          // SportMonks uses GB for England fixtures
  ENG: 'İngiltere',         // some sources ship ENG instead of GB
  DE: 'Almanya',
  ES: 'İspanya',
  IT: 'İtalya',
  FR: 'Fransa',
  NL: 'Hollanda',
  PT: 'Portekiz',
  BE: 'Belçika',
  CH: 'İsviçre',
  AT: 'Avusturya',
  PL: 'Polonya',
  SE: 'İsveç',
  NO: 'Norveç',
  DK: 'Danimarka',
  FI: 'Finlandiya',
  IE: 'İrlanda',
  GR: 'Yunanistan',
  CZ: 'Çekya',
  SK: 'Slovakya',
  HU: 'Macaristan',
  RO: 'Romanya',
  BG: 'Bulgaristan',
  HR: 'Hırvatistan',
  RS: 'Sırbistan',
  SI: 'Slovenya',
  UA: 'Ukrayna',
  RU: 'Rusya',
  BY: 'Belarus',
  IS: 'İzlanda',
  AL: 'Arnavutluk',
  CY: 'Kıbrıs',
  // Americas
  US: 'ABD',
  CA: 'Kanada',
  MX: 'Meksika',
  BR: 'Brezilya',
  AR: 'Arjantin',
  CL: 'Şili',
  CO: 'Kolombiya',
  UY: 'Uruguay',
  PE: 'Peru',
  EC: 'Ekvador',
  // Asia / Oceania
  JP: 'Japonya',
  KR: 'Güney Kore',
  CN: 'Çin',
  IN: 'Hindistan',
  AU: 'Avustralya',
  NZ: 'Yeni Zelanda',
  SA: 'Suudi Arabistan',
  AE: 'Birleşik Arap Emirlikleri',
  QA: 'Katar',
  IR: 'İran',
  IL: 'İsrail',
  ID: 'Endonezya',
  TH: 'Tayland',
  VN: 'Vietnam',
  // Africa
  EG: 'Mısır',
  MA: 'Fas',
  TN: 'Tunus',
  DZ: 'Cezayir',
  NG: 'Nijerya',
  ZA: 'Güney Afrika',
  GH: 'Gana',
  SN: 'Senegal',
  CI: 'Fildişi Sahili',
  CM: 'Kamerun',
};

function activeLang(): string {
  return (i18nInstance.language ?? '') as string;
}

/**
 * Returns the country's name in the user's current locale. Falls back
 * to the SportMonks-supplied English name when:
 *   * locale is English (no translation needed)
 *   * iso2 isn't in TR_BY_ISO2 (long-tail nation)
 *   * the Country object lacks an iso2 code entirely
 *
 * Pass the whole Country object so we can reach both `iso2` and `name`
 * — the name acts as the fallback string for unmapped iso codes.
 */
export function countryName(
  country: { iso2?: string | null; name: string } | null | undefined,
): string {
  if (!country) return '';
  if (activeLang().toLowerCase().startsWith('en')) return country.name;
  const iso = country.iso2?.toUpperCase();
  if (iso && TR_BY_ISO2[iso]) return TR_BY_ISO2[iso];
  return country.name;
}
