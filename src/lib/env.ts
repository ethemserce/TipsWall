const rawBaseUrl = process.env.EXPO_PUBLIC_API_BASE_URL;

if (!rawBaseUrl) {
  throw new Error(
    'EXPO_PUBLIC_API_BASE_URL is not set. Copy .env.example to .env and restart the dev server.',
  );
}

export const env = {
  apiBaseUrl: rawBaseUrl.replace(/\/+$/, ''),
} as const;
