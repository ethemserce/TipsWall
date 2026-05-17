import Link from 'next/link';
import { redirect } from 'next/navigation';

import { clearSession, getSession } from '@/lib/session';

async function signOut(): Promise<void> {
  'use server';
  await clearSession();
  redirect('/login');
}

export default async function OpsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await getSession();
  if (!session.accessToken) {
    redirect('/login');
  }
  // Defence-in-depth — middleware will redirect a non-admin token away,
  // but render-time check belt-and-braces in case middleware is bypassed.
  if (!session.isAdmin) {
    redirect('/login?error=unauthorized');
  }
  return (
    <div className="min-h-screen bg-bg-subtle">
      <header className="bg-bg border-b border-border sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-6 py-3 flex items-center justify-between gap-4">
          <div className="flex items-center gap-6">
            <Link href="/ops" className="font-semibold tracking-tight">
              TipsWall <span className="text-fg-muted font-normal">/ admin</span>
            </Link>
            <nav className="flex items-center gap-1 text-sm">
              <Link
                href="/ops"
                className="px-3 py-1.5 rounded-md text-fg-muted hover:text-fg hover:bg-bg-subtle">
                Ops
              </Link>
            </nav>
          </div>
          <div className="flex items-center gap-3">
            <span className="text-xs text-fg-muted hidden sm:inline">
              {session.email ?? 'admin'}
            </span>
            <form action={signOut}>
              <button
                type="submit"
                className="text-xs text-fg-muted hover:text-fg px-2 py-1 rounded-md hover:bg-bg-subtle">
                Çıkış
              </button>
            </form>
          </div>
        </div>
      </header>
      <main className="max-w-7xl mx-auto px-6 py-8">{children}</main>
    </div>
  );
}
