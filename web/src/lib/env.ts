/**
 * Browser-visible env. `NEXT_PUBLIC_*` is the only prefix that survives
 * the client bundle, so the dashboard's API base URL has to be wired
 * through this prefix. Defaults match production; override per-env via
 * .env.local or the deploy compose file.
 */
export const env = {
  apiBaseUrl: process.env.NEXT_PUBLIC_API_BASE_URL ?? 'https://api.tipswall.com',
} as const;
