// Softer, more muted teal-green brand. Less saturated than the previous
// 16a34a; reads as "calm" rather than "alert" against neutral surfaces.
// Live red and win/loss accents are also stepped down a notch.
export const palette = {
  brand: '#3a8f6f',
  brandDark: '#2c6e54',
  brandSoft: 'rgba(58, 143, 111, 0.10)',
  live: '#d97070',

  // Semantic accents — success / danger / warning / info. Single source of
  // truth for win/loss/value badges, toast accents, button states, etc.
  // Each has a `soft` variant for tinted backgrounds (10% alpha).
  success: '#4ade80',
  successSoft: 'rgba(74, 222, 128, 0.12)',
  danger: '#f87171',
  dangerSoft: 'rgba(248, 113, 113, 0.12)',
  warning: '#f59e0b',
  warningSoft: 'rgba(245, 158, 11, 0.12)',
  info: '#3b82f6',
  infoSoft: 'rgba(59, 130, 246, 0.12)',

  // Metric column colours — DSO (green), VBET (amber), İKO (blue) — pulled
  // from the semantic set so they stay coordinated.
  metricDso: '#22c55e',
  metricVbet: '#f59e0b',
  metricIko: '#3b82f6',

  bg: {
    light: '#fafafa',
    dark: '#0d1117',
  },
  surface: {
    light: '#f1f3f5',
    dark: '#141a22',
  },
  surfaceElevated: {
    light: '#ffffff',
    dark: '#1a212b',
  },
  border: {
    light: '#e4e6ea',
    dark: '#222b37',
  },
  borderSoft: {
    light: '#eef0f3',
    dark: '#1c2531',
  },
  text: {
    light: '#1a2230',
    dark: '#e6ebf2',
  },
  textMuted: {
    light: '#6b7280',
    dark: '#94a0b0',
  },
  textInverse: '#ffffff',
} as const;

export const radius = {
  sm: 6,
  md: 10,
  lg: 14,
  xl: 18,
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

/**
 * Layered shadow presets. Use `card` for everyday surface elevation and
 * `floating` for sheets / toasts that hover above the page. Shadows are
 * dropped on dark theme (looks muddy on near-black bgs); elevation alone
 * carries depth there.
 */
export const elevation = {
  card: {
    light: {
      shadowColor: '#0f172a',
      shadowOpacity: 0.06,
      shadowRadius: 8,
      shadowOffset: { width: 0, height: 2 },
      elevation: 2,
    },
    dark: {
      shadowColor: 'transparent',
      shadowOpacity: 0,
      shadowRadius: 0,
      shadowOffset: { width: 0, height: 0 },
      elevation: 0,
    },
  },
  floating: {
    light: {
      shadowColor: '#0f172a',
      shadowOpacity: 0.14,
      shadowRadius: 14,
      shadowOffset: { width: 0, height: 6 },
      elevation: 6,
    },
    dark: {
      shadowColor: '#000000',
      shadowOpacity: 0.5,
      shadowRadius: 14,
      shadowOffset: { width: 0, height: 6 },
      elevation: 6,
    },
  },
} as const;

export interface ThemeShadow {
  shadowColor: string;
  shadowOpacity: number;
  shadowRadius: number;
  shadowOffset: { width: number; height: number };
  elevation: number;
}

export interface ThemeColors {
  bg: string;
  surface: string;
  surfaceElevated: string;
  border: string;
  borderSoft: string;
  text: string;
  textMuted: string;
  textInverse: string;
  brand: string;
  brandSoft: string;
  live: string;
  // Semantic
  success: string;
  successSoft: string;
  danger: string;
  dangerSoft: string;
  warning: string;
  warningSoft: string;
  info: string;
  infoSoft: string;
  // Metric tints used in cards
  metricDso: string;
  metricVbet: string;
  metricIko: string;
  // Shadow presets
  shadowCard: ThemeShadow;
  shadowFloating: ThemeShadow;
  // Whether the theme is dark — useful for components that need to flip
  // tone-only choices (e.g. tinted soft backgrounds at higher opacity).
  isDark: boolean;
}

export function buildColors(scheme: 'light' | 'dark'): ThemeColors {
  return {
    bg: palette.bg[scheme],
    surface: palette.surface[scheme],
    surfaceElevated: palette.surfaceElevated[scheme],
    border: palette.border[scheme],
    borderSoft: palette.borderSoft[scheme],
    text: palette.text[scheme],
    textMuted: palette.textMuted[scheme],
    textInverse: palette.textInverse,
    brand: palette.brand,
    brandSoft: palette.brandSoft,
    live: palette.live,
    success: palette.success,
    successSoft: palette.successSoft,
    danger: palette.danger,
    dangerSoft: palette.dangerSoft,
    warning: palette.warning,
    warningSoft: palette.warningSoft,
    info: palette.info,
    infoSoft: palette.infoSoft,
    metricDso: palette.metricDso,
    metricVbet: palette.metricVbet,
    metricIko: palette.metricIko,
    shadowCard: elevation.card[scheme],
    shadowFloating: elevation.floating[scheme],
    isDark: scheme === 'dark',
  };
}
