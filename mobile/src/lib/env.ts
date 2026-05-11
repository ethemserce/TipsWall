import { Platform } from 'react-native';

const PORT = 28333;

function defaultBaseUrl(): string {
  // Android emulator routes the host's localhost through 10.0.2.2.
  // Browser, iOS simulator and macOS/Windows desktop reach it directly.
  if (Platform.OS === 'android') return `http://10.0.2.2:${PORT}`;
  return `http://localhost:${PORT}`;
}

const explicit = process.env.EXPO_PUBLIC_API_BASE_URL?.trim();

// Google OAuth client IDs (Google Cloud → Credentials). Set as
// EXPO_PUBLIC_GOOGLE_CLIENT_ID_IOS / _ANDROID / _WEB. Missing values
// keep the Google sign-in button hidden so the app still ships in
// dev environments where OAuth isn't configured yet.
const googleIosId = process.env.EXPO_PUBLIC_GOOGLE_CLIENT_ID_IOS?.trim() ?? '';
const googleAndroidId = process.env.EXPO_PUBLIC_GOOGLE_CLIENT_ID_ANDROID?.trim() ?? '';
const googleWebId = process.env.EXPO_PUBLIC_GOOGLE_CLIENT_ID_WEB?.trim() ?? '';

export const env = {
  apiBaseUrl: (explicit && explicit.length > 0 ? explicit : defaultBaseUrl()).replace(
    /\/+$/,
    '',
  ),
  googleClientIdIos: googleIosId,
  googleClientIdAndroid: googleAndroidId,
  googleClientIdWeb: googleWebId,
  hasGoogleSignIn:
    googleIosId.length > 0 || googleAndroidId.length > 0 || googleWebId.length > 0,
} as const;
