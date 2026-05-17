import type { NextConfig } from 'next';

const config: NextConfig = {
  // Standalone build keeps the prod image lean (~150 MB instead of the
  // ~600 MB full node_modules copy). The Dockerfile copies .next/
  // standalone/ + .next/static/ and runs `node server.js`.
  output: 'standalone',
  reactStrictMode: true,
  // The backend is exposed publicly on https://api.tipswall.com so the
  // browser hits it directly via NEXT_PUBLIC_API_BASE_URL. No proxying
  // through Next's API routes today; revisit if we ever need to hide
  // the backend behind same-origin cookies.
  poweredByHeader: false,
};

export default config;
