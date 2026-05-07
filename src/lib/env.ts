import { Platform } from 'react-native';

const PORT = 28333;

function defaultBaseUrl(): string {
  // Android emulator routes the host's localhost through 10.0.2.2.
  // Browser, iOS simulator and macOS/Windows desktop reach it directly.
  if (Platform.OS === 'android') return `http://10.0.2.2:${PORT}`;
  return `http://localhost:${PORT}`;
}

const explicit = process.env.EXPO_PUBLIC_API_BASE_URL?.trim();

export const env = {
  apiBaseUrl: (explicit && explicit.length > 0 ? explicit : defaultBaseUrl()).replace(
    /\/+$/,
    '',
  ),
} as const;
