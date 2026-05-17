import { redirect } from 'next/navigation';

import { getSession } from '@/lib/session';

/**
 * Root route — dispatches to the ops dashboard for signed-in admins
 * and to /login for everyone else. Server component, no client JS.
 */
export default async function RootPage() {
  const session = await getSession();
  if (session.isAdmin) {
    redirect('/ops');
  }
  redirect('/login');
}
