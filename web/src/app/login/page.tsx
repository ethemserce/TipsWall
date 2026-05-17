import { redirect } from 'next/navigation';

import { env } from '@/lib/env';
import { getSession, setSession } from '@/lib/session';

/**
 * Server action that hits the backend's /auth/token endpoint, stores
 * the access + refresh pair in httpOnly cookies, and redirects to /ops.
 * Errors render inline; success bounces straight to the dashboard.
 */
async function signIn(formData: FormData): Promise<void> {
  'use server';
  const email = String(formData.get('email') ?? '').trim();
  const password = String(formData.get('password') ?? '');
  if (!email || !password) {
    redirect('/login?error=missing');
  }

  let res: Response;
  try {
    res = await fetch(`${env.apiBaseUrl}/api/v3/auth/token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      // Backend's V3 LoginRequest binds `username` + `password` (it
      // accepts either the username or the email in that field via
      // IUserIdentityService.AuthenticateAsync). Earlier draft sent
      // `username_or_email` which silently bound to nothing and the
      // controller responded 400 with "username and password are
      // required".
      body: JSON.stringify({ username: email, password }),
      cache: 'no-store',
    });
  } catch {
    redirect('/login?error=network');
  }

  if (!res.ok) {
    redirect(`/login?error=${res.status === 401 ? 'invalid' : 'server'}`);
  }
  const body = (await res.json()) as {
    success?: boolean;
    data?: { access_token?: string; refresh_token?: string };
  };
  const access = body.data?.access_token;
  const refresh = body.data?.refresh_token;
  if (!body.success || !access || !refresh) {
    redirect('/login?error=invalid');
  }
  await setSession(access, refresh);
  redirect('/ops');
}

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string }>;
}) {
  const session = await getSession();
  if (session.isAdmin) {
    redirect('/ops');
  }
  const { error } = await searchParams;
  return (
    <main className="min-h-screen flex items-center justify-center bg-bg-subtle px-4">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <h1 className="text-2xl font-semibold tracking-tight">TipsWall Admin</h1>
          <p className="mt-2 text-sm text-fg-muted">Ops dashboard girişi</p>
        </div>

        <form
          action={signIn}
          className="bg-bg border border-border rounded-lg p-6 shadow-sm space-y-4">
          <div>
            <label htmlFor="email" className="block text-sm font-medium mb-1.5">
              E-posta veya kullanıcı adı
            </label>
            <input
              id="email"
              name="email"
              type="text"
              autoComplete="username"
              required
              className="w-full px-3 py-2 text-sm border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-accent/40 focus:border-accent"
            />
          </div>
          <div>
            <label htmlFor="password" className="block text-sm font-medium mb-1.5">
              Şifre
            </label>
            <input
              id="password"
              name="password"
              type="password"
              autoComplete="current-password"
              required
              className="w-full px-3 py-2 text-sm border border-border rounded-md focus:outline-none focus:ring-2 focus:ring-accent/40 focus:border-accent"
            />
          </div>
          {error ? (
            <p className="text-xs text-danger">
              {error === 'invalid'
                ? 'Kullanıcı adı veya şifre hatalı.'
                : error === 'network'
                  ? 'Sunucuya ulaşılamadı. Tekrar dene.'
                  : error === 'missing'
                    ? 'Tüm alanları doldur.'
                    : 'Beklenmeyen bir hata oluştu.'}
            </p>
          ) : null}
          <button
            type="submit"
            className="w-full py-2 text-sm font-semibold bg-fg text-bg rounded-md hover:bg-fg/90 transition-colors">
            Giriş yap
          </button>
        </form>

        <p className="mt-4 text-center text-xs text-fg-subtle">
          Hesap yalnızca admin yetkilisi olan kullanıcılar için. Yetkin yoksa giriş
          sonrası bir uyarı göreceksin.
        </p>
      </div>
    </main>
  );
}
