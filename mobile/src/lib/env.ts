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

// AdMob ad unit IDs. Production IDs are baked in below for both
// platforms (TipsWall AdMob account, pub-8392820482432358). An env
// override (EXPO_PUBLIC_ADMOB_BANNER_UNIT_ID_IOS / _ANDROID) wins
// when set — useful for staging or A/B testing a different
// inventory line without a redeploy.
const PROD_BANNER_ID_ANDROID = 'ca-app-pub-8392820482432358/2162535611';
const PROD_BANNER_ID_IOS = 'ca-app-pub-8392820482432358/1770958840';
const realBannerIos = process.env.EXPO_PUBLIC_ADMOB_BANNER_UNIT_ID_IOS?.trim() ?? '';
const realBannerAndroid = process.env.EXPO_PUBLIC_ADMOB_BANNER_UNIT_ID_ANDROID?.trim() ?? '';
const bannerIos = realBannerIos.length > 0 ? realBannerIos : PROD_BANNER_ID_IOS;
const bannerAndroid = realBannerAndroid.length > 0 ? realBannerAndroid : PROD_BANNER_ID_ANDROID;

// Native Advanced units — reserved for future native-ad placements
// (e.g. a sponsored card slot inside the fixture detail flow). Not
// rendered today; the constants live here so the implementation can
// pick them up without another round-trip to the AdMob console.
const PROD_NATIVE_ID_ANDROID = 'ca-app-pub-8392820482432358/4397122185';
const PROD_NATIVE_ID_IOS = 'ca-app-pub-8392820482432358/8556544800';

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
  // AdMob — banner + native unit ids per platform. Both platforms
  // ship with real production units from the TipsWall AdMob account.
  // `useTestAds` is false in production; flips true only if a future
  // build wants to force test inventory (no env hook yet, would be a
  // boolean override added here).
  admobBannerUnitId: Platform.OS === 'ios' ? bannerIos : bannerAndroid,
  admobNativeUnitId: Platform.OS === 'ios' ? PROD_NATIVE_ID_IOS : PROD_NATIVE_ID_ANDROID,
  useTestAds: false,
} as const;
