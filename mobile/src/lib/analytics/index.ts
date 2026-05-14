/**
 * Single entry point for analytics in the app. Call sites import `analytics`
 * from here — never import `@react-native-firebase/*` directly. This keeps
 * the dependency removable: delete `./firebase.ts`, swap its imports for
 * no-ops, drop the package — the rest of the app keeps working unchanged.
 *
 * Every call is gated on consent (`consentStore`). Pre-consent or
 * post-deny calls become silent no-ops, so we never ship KVKK-relevant
 * events without explicit opt-in.
 */

import { consentStore, type ConsentState } from './consent';
import * as Firebase from './firebase';

async function track(name: string, params?: Record<string, unknown>): Promise<void> {
  if (!consentStore.isGranted()) return;
  await Firebase.logEvent(name, params);
}

async function screen(screenName: string, screenClass?: string): Promise<void> {
  if (!consentStore.isGranted()) return;
  await Firebase.logScreenView(screenName, screenClass);
}

async function setUserId(id: string | null): Promise<void> {
  if (!consentStore.isGranted()) return;
  await Firebase.setUserId(id);
}

async function setUserProperty(name: string, value: string | null): Promise<void> {
  if (!consentStore.isGranted()) return;
  await Firebase.setUserProperty(name, value);
}

/**
 * Apply the persisted/current consent state to the underlying SDK. Called
 * once at app boot after `consentStore.hydrate()`, and again every time
 * the user flips the Settings toggle.
 */
async function syncCollectionFromConsent(): Promise<void> {
  await Firebase.setCollectionEnabled(consentStore.isGranted());
}

async function grantConsent(): Promise<void> {
  await consentStore.set('granted');
  await syncCollectionFromConsent();
}

async function denyConsent(): Promise<void> {
  await consentStore.set('denied');
  await syncCollectionFromConsent();
  // Clear user identifier from Firebase when the user opts out — we don't
  // want any further events tied to them even if a stray call slips past
  // the consent gate.
  await Firebase.setUserId(null);
}

export const analytics = {
  track,
  screen,
  setUserId,
  setUserProperty,
  grantConsent,
  denyConsent,
  syncCollectionFromConsent,
  consentState: (): ConsentState => consentStore.getState(),
  subscribe: consentStore.subscribe,
  hydrate: consentStore.hydrate,
  isAvailable: Firebase.isAvailable,
};

export { consentStore };
export type { ConsentState };
