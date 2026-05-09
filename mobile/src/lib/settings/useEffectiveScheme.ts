import { useColorScheme } from '@/hooks/use-color-scheme';
import { resolveScheme, useThemeMode } from './settingsStore';

// Returns the active 'light' | 'dark' scheme — same logic useTheme uses to
// pick colours, exposed separately so callers can swap raster assets
// (e.g. light-mode vs dark-mode logo) without going through ThemeColors.
export function useEffectiveScheme(): 'light' | 'dark' {
  const device = useColorScheme();
  const mode = useThemeMode();
  return resolveScheme(mode, device);
}
