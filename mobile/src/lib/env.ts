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

// AdMob ad unit IDs. Production IDs are baked in below; an env
// override (EXPO_PUBLIC_ADMOB_BANNER_UNIT_ID_IOS / _ANDROID) wins
// when set, useful for staging a different inventory line. iOS unit
// id is still Google's universal banner test ID until the iOS AdMob
// app + unit are minted — `useTestAds` flips true for iOS so the
// BannerAd component can flag the slot as non-revenue.
const PROD_BANNER_ID_ANDROID = 'ca-app-pub-8392820482432358/2162535611';
const TEST_BANNER_ID_IOS = 'ca-app-pub-3940256099942544/2934735716';
const realBannerIos = process.env.EXPO_PUBLIC_ADMOB_BANNER_UNIT_ID_IOS?.trim() ?? '';
const realBannerAndroid = process.env.EXPO_PUBLIC_ADMOB_BANNER_UNIT_ID_ANDROID?.trim() ?? '';
const bannerIos = realBannerIos.length > 0 ? realBannerIos : TEST_BANNER_ID_IOS;
const bannerAndroid = realBannerAndroid.length > 0 ? realBannerAndroid : PROD_BANNER_ID_ANDROID;

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
  // AdMob — banner unit id per platform. Android ships with the
  // real production unit baked in; iOS falls back to a test unit
  // until the iOS AdMob app is provisioned. `useTestAds` flips true
  // when we're serving Google's universal test inventory so the
  // BannerAd can tag impressions as non-revenue if needed.
  admobBannerUnitId: Platform.OS === 'ios' ? bannerIos : bannerAndroid,
  useTestAds: Platform.OS === 'ios',
} as const;
