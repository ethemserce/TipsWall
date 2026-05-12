import AsyncStorage from '@react-native-async-storage/async-storage';
import { useEffect, useState } from 'react';

export type ThemeMode = 'system' | 'light' | 'dark';
export type LanguageMode = 'system' | 'en' | 'tr';

export interface SettingsSnapshot {
  themeMode: ThemeMode;
  languageMode: LanguageMode;
  // When true, every UI surface that would otherwise expose an odd
  // value (OddsRatesCard's odd cell, AiPicksCard's odd line) hides it.
  // Tip text and stats stay visible — this isn't a "hide everything",
  // just an "no-betting framing" toggle the user can opt out of.
  oddsHidden: boolean;
  hydrated: boolean;
}

type Listener = () => void;

const STORAGE_KEY = 'preodds.settings.v1';

const DEFAULTS: Omit<SettingsSnapshot, 'hydrated'> = {
  themeMode: 'system',
  languageMode: 'system',
  oddsHidden: false,
};

let state: SettingsSnapshot = { ...DEFAULTS, hydrated: false };
const listeners = new Set<Listener>();

function emit() {
  for (const l of listeners) l();
}

async function persist() {
  try {
    const { themeMode, languageMode, oddsHidden } = state;
    await AsyncStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({ themeMode, languageMode, oddsHidden }),
    );
  } catch {
    // Persistence is best-effort; an in-memory miss is fine — defaults reload.
  }
}

async function hydrate() {
  if (state.hydrated) return;
  try {
    const raw = await AsyncStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as Partial<SettingsSnapshot>;
      state = {
        themeMode:
          parsed.themeMode === 'light' ||
          parsed.themeMode === 'dark' ||
          parsed.themeMode === 'system'
            ? parsed.themeMode
            : DEFAULTS.themeMode,
        languageMode:
          parsed.languageMode === 'en' ||
          parsed.languageMode === 'tr' ||
          parsed.languageMode === 'system'
            ? parsed.languageMode
            : DEFAULTS.languageMode,
        oddsHidden:
          typeof parsed.oddsHidden === 'boolean'
            ? parsed.oddsHidden
            : DEFAULTS.oddsHidden,
        hydrated: true,
      };
    } else {
      state = { ...DEFAULTS, hydrated: true };
    }
  } catch {
    state = { ...DEFAULTS, hydrated: true };
  }
  emit();
}

hydrate();

export function getSettings(): SettingsSnapshot {
  return state;
}

export function subscribeSettings(l: Listener): () => void {
  listeners.add(l);
  return () => {
    listeners.delete(l);
  };
}

export function setThemeMode(mode: ThemeMode): void {
  if (state.themeMode === mode) return;
  state = { ...state, themeMode: mode };
  emit();
  void persist();
}

export function setLanguageMode(mode: LanguageMode): void {
  if (state.languageMode === mode) return;
  state = { ...state, languageMode: mode };
  emit();
  void persist();
}

export function setOddsHidden(hidden: boolean): void {
  if (state.oddsHidden === hidden) return;
  state = { ...state, oddsHidden: hidden };
  emit();
  void persist();
}

export function useSettings(): SettingsSnapshot {
  const [snapshot, setSnapshot] = useState<SettingsSnapshot>(state);
  useEffect(() => {
    const unsubscribe = subscribeSettings(() => setSnapshot(getSettings()));
    setSnapshot(getSettings());
    return unsubscribe;
  }, []);
  return snapshot;
}

export function useThemeMode(): ThemeMode {
  return useSettings().themeMode;
}

export function useLanguageMode(): LanguageMode {
  return useSettings().languageMode;
}

export function useOddsHidden(): boolean {
  return useSettings().oddsHidden;
}

// Given a user-selected mode, resolves to a concrete 'light' | 'dark' the
// renderer can act on. Pulled out as a helper so app/_layout (navigation
// theme + status bar) and useTheme can stay aligned.
export function resolveScheme(
  mode: ThemeMode,
  device: 'light' | 'dark' | null | undefined,
): 'light' | 'dark' {
  if (mode === 'light' || mode === 'dark') return mode;
  return device === 'dark' ? 'dark' : 'light';
}
