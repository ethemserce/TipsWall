import { useMemo } from 'react';

import { useColorScheme } from '@/hooks/use-color-scheme';
import { buildColors, type ThemeColors } from '@/src/lib/theme';
import { resolveScheme, useThemeMode } from '@/src/lib/settings/settingsStore';

// themeMode='system' (default) follows the device. 'light'/'dark' force
// the override regardless of OS appearance — used by the Settings screen
// when the user wants to pin one mode.
export function useTheme(): ThemeColors {
  const deviceScheme = useColorScheme();
  const mode = useThemeMode();
  const effective = resolveScheme(mode, deviceScheme);
  return useMemo(() => buildColors(effective), [effective]);
}
