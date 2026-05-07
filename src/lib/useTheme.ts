import { useMemo } from 'react';

import { useColorScheme } from '@/hooks/use-color-scheme';
import { buildColors, type ThemeColors } from '@/src/lib/theme';

export function useTheme(): ThemeColors {
  const scheme = useColorScheme();
  return useMemo(() => buildColors(scheme === 'dark' ? 'dark' : 'light'), [scheme]);
}
