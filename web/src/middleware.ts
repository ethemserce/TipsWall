import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

/**
 * Edge-time gate for /ops/*. Reads the access cookie + admin claim
 * before any server component renders. Failures bounce to /login so
 * the dashboard never paints a flash of authenticated content.
 *
 * Server components inside /ops/layout.tsx re-validate as a belt-
 * and-braces check, and every API call still goes through the
 * backend's AdminOnly policy — this middleware just keeps the UX
 * crisp.
 */
const ACCESS_COOKIE = 'tw_admin_access';

export function middleware(req: NextRequest) {
  const { pathname } = req.nextUrl;
  if (!pathname.startsWith('/ops')) {
    return NextResponse.next();
  }
  const token = req.cookies.get(ACCESS_COOKIE)?.value;
  if (!token) {
    return NextResponse.redirect(new URL('/login', req.url));
  }
  // Light verification only — we trust the cookie's existence + admin
  // claim. The backend's AdminOnly policy is the real check.
  const payload = decode(token);
  if (payload?.admin !== 'true') {
    return NextResponse.redirect(new URL('/login?error=unauthorized', req.url));
  }
  return NextResponse.next();
}

export const config = {
  matcher: ['/ops/:path*'],
};

function decode(token: string): { admin?: string; exp?: number } | null {
  try {
    const parts = token.split('.');
    if (parts.length < 2) return null;
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64 + '==='.slice((base64.length + 3) % 4);
    const json = atob(padded);
    return JSON.parse(json);
  } catch {
    return null;
  }
}
