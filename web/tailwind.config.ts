import type { Config } from 'tailwindcss';

const config: Config = {
  content: [
    './src/app/**/*.{ts,tsx}',
    './src/components/**/*.{ts,tsx}',
  ],
  theme: {
    extend: {
      colors: {
        // Vercel-dashboard-style neutral palette. Light by default,
        // dark variants ride on the `dark` class (CSS prefers-color-scheme).
        bg: {
          DEFAULT: 'hsl(0 0% 100%)',
          subtle: 'hsl(0 0% 98%)',
        },
        fg: {
          DEFAULT: 'hsl(0 0% 9%)',
          muted: 'hsl(0 0% 45%)',
          subtle: 'hsl(0 0% 64%)',
        },
        border: {
          DEFAULT: 'hsl(0 0% 90%)',
          subtle: 'hsl(0 0% 94%)',
        },
        accent: 'hsl(220 90% 56%)',
        success: 'hsl(142 70% 38%)',
        warning: 'hsl(38 92% 50%)',
        danger: 'hsl(0 84% 50%)',
      },
      fontFamily: {
        sans: ['var(--font-geist-sans)', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['var(--font-geist-mono)', 'ui-monospace', 'monospace'],
      },
    },
  },
  plugins: [],
};

export default config;
