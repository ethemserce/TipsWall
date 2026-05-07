export const palette = {
  brand: '#16a34a',
  brandDark: '#0f7c39',
  live: '#ef4444',

  bg: {
    light: '#ffffff',
    dark: '#0b0f14',
  },
  surface: {
    light: '#f5f6f8',
    dark: '#11161d',
  },
  surfaceElevated: {
    light: '#ffffff',
    dark: '#161c25',
  },
  border: {
    light: '#e5e7eb',
    dark: '#1f2733',
  },
  text: {
    light: '#0b0f14',
    dark: '#f1f5f9',
  },
  textMuted: {
    light: '#6b7280',
    dark: '#94a3b8',
  },
  textInverse: '#ffffff',
} as const;

export const radius = {
  sm: 6,
  md: 10,
  lg: 14,
  pill: 999,
} as const;

export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 24,
  xxl: 32,
} as const;

export const typography = {
  caption: { fontSize: 11, lineHeight: 14 },
  small: { fontSize: 12, lineHeight: 16 },
  body: { fontSize: 14, lineHeight: 20 },
  bodyStrong: { fontSize: 14, lineHeight: 20, fontWeight: '600' as const },
  title: { fontSize: 22, lineHeight: 28, fontWeight: '700' as const },
} as const;

export interface ThemeColors {
  bg: string;
  surface: string;
  surfaceElevated: string;
  border: string;
  text: string;
  textMuted: string;
  textInverse: string;
  brand: string;
  live: string;
}

export function buildColors(scheme: 'light' | 'dark'): ThemeColors {
  return {
    bg: palette.bg[scheme],
    surface: palette.surface[scheme],
    surfaceElevated: palette.surfaceElevated[scheme],
    border: palette.border[scheme],
    text: palette.text[scheme],
    textMuted: palette.textMuted[scheme],
    textInverse: palette.textInverse,
    brand: palette.brand,
    live: palette.live,
  };
}
