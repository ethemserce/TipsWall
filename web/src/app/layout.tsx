import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'TipsWall Admin',
  description: 'Operational dashboard for the TipsWall stack.',
  robots: 'noindex, nofollow',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
