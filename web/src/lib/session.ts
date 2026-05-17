import { cookies } from 'next/headers';

const ACCESS_COOKIE = 'tw_admin_access';
const REFRESH_COOKIE = 'tw_admin_refresh';

interface JwtPayload {
  exp?: number;
  uid?: string;
  admin?: string;
  tier?: string;
  email?: string;
}

/**
 * httpOnly cookie-backed session. The mobile app holds tokens in
 * SecureStore; the web admin holds them in cookies so a logged-in
 * browser doesn't expose the token to client JS. Both halves of the
 * pair are stamped: access (short-lived, ~15 min) drives requests,
 * refresh stays signed and is sent back to /auth/refresh when the
 * access expires.
 *
 * `admin` claim must be `"true"` for any /ops route to load — middleware
 * enforces this at the edge AND every server-fetched read endpoint
 * re-validates server-side because the backend's [Authorize(Policy =
 * "AdminOnly")] is the actual source of truth.
 */
export async function getSession(): Promise<{
  accessToken: string | null;
  refreshToken: string | null;
  isAdmin: boolean;
  email: string | null;
}> {
  const store = await cookies();
  const accessToken = store.get(ACCESS_COOKIE)?.value ?? null;
  const refreshToken = store.get(REFRESH_COOKIE)?.value ?? null;
  if (!accessToken) {
    return { accessToken: null, refreshToken: null, isAdmin: false, email: null };
  }
  const payload = decodeJwt(accessToken);
  return {
    accessToken,
    refreshToken,
    isAdmin: payload?.admin === 'true',
    email: payload?.email ?? null,
  };
}

export async function setSession(access: string, refresh: string): Promise<void> {
  const store = await cookies();
  const accessPayload = decodeJwt(access);
  // Access expiry from JWT exp; fall back to 15 min if the claim isn't
  // present. Cookie expiry follows the token's own lifetime so a stale
  // cookie can't outlive its signed payload.
  const accessExpires = accessPayload?.exp
    ? new Date(accessPayload.exp * 1000)
    : new Date(Date.now() + 15 * 60 * 1000);
  // Refresh is opaque to the client; the backend tracks its expiry. 30
  // days is generous but lines up with the mobile setup.
  const refreshExpires = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000);
  const baseOpts = {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax' as const,
    path: '/',
  };
  store.set(ACCESS_COOKIE, access, { ...baseOpts, expires: accessExpires });
  store.set(REFRESH_COOKIE, refresh, { ...baseOpts, expires: refreshExpires });
}

export async function clearSession(): Promise<void> {
  const store = await cookies();
  store.delete(ACCESS_COOKIE);
  store.delete(REFRESH_COOKIE);
}

function decodeJwt(token: string): JwtPayload | null {
  try {
    const parts = token.split('.');
    if (parts.length < 2) return null;
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64 + '==='.slice((base64.length + 3) % 4);
    const json = Buffer.from(padded, 'base64').toString('utf8');
    return JSON.parse(json) as JwtPayload;
  } catch {
    return null;
  }
}
