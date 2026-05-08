import AsyncStorage from '@react-native-async-storage/async-storage';

/**
 * Read/write token storage abstraction. Refresh tokens go to expo-secure-store
 * (Keychain on iOS, EncryptedSharedPreferences on Android) so a rooted device
 * can't trivially harvest them. Access tokens are short-lived (~15 min) and
 * fine in AsyncStorage.
 *
 * If expo-secure-store isn't available at runtime (e.g. before npm install,
 * or the web build), we transparently fall back to AsyncStorage so login
 * still works — no silent breakage during dev.
 */

const ACCESS_KEY = 'preodds.auth.access.v1';
const REFRESH_KEY = 'preodds.auth.refresh.v1';

interface SecureStoreModule {
  getItemAsync(key: string): Promise<string | null>;
  setItemAsync(key: string, value: string): Promise<void>;
  deleteItemAsync(key: string): Promise<void>;
}

let secureStore: SecureStoreModule | null = null;
try {
  // Dynamic require so a missing package degrades gracefully instead of
  // crashing module load.
  // eslint-disable-next-line @typescript-eslint/no-require-imports
  secureStore = require('expo-secure-store') as SecureStoreModule;
} catch {
  secureStore = null;
}

export async function readAccessToken(): Promise<string | null> {
  return AsyncStorage.getItem(ACCESS_KEY);
}

export async function writeAccessToken(token: string | null): Promise<void> {
  if (token == null) await AsyncStorage.removeItem(ACCESS_KEY);
  else await AsyncStorage.setItem(ACCESS_KEY, token);
}

export async function readRefreshToken(): Promise<string | null> {
  if (secureStore) {
    try {
      return await secureStore.getItemAsync(REFRESH_KEY);
    } catch {
      // Fall through to AsyncStorage on any secure-store hiccup.
    }
  }
  return AsyncStorage.getItem(REFRESH_KEY);
}

export async function writeRefreshToken(token: string | null): Promise<void> {
  if (secureStore) {
    try {
      if (token == null) await secureStore.deleteItemAsync(REFRESH_KEY);
      else await secureStore.setItemAsync(REFRESH_KEY, token);
      return;
    } catch {
      // Fall through.
    }
  }
  if (token == null) await AsyncStorage.removeItem(REFRESH_KEY);
  else await AsyncStorage.setItem(REFRESH_KEY, token);
}

export async function clearAllTokens(): Promise<void> {
  await Promise.all([writeAccessToken(null), writeRefreshToken(null)]);
}
