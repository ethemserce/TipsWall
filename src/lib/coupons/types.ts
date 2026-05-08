export interface CouponSelection {
  // Local id used to remove a single selection from a coupon.
  id: string;

  fixtureId: number;
  fixtureName: string;
  startingAt: string | null;

  bookmakerId: number;
  marketId: number;
  marketShort: string; // 'MS', 'KG', etc. — display label
  // Canonical raw label as it appears in the bookmaker's odds feed
  // ("Home", "Yes", "AGF Aarhus - 3+ Goals"). Used for key matching and
  // settlement so we never lose the round-trip identity.
  outcomeLabel: string;
  // Translated/shortened version for UI ("1", "Var", "3+"). Optional for
  // backwards compatibility with coupons saved before this field existed.
  outcomeDisplay?: string;
  total: string | null;
  handicap: string | null;
  oddValue: number;

  // Snapshot at pick time so the user can later judge their own intuition.
  dso: number | null;
  vbet: number | null;
  iko: number | null;
  sampleCount: number | null;

  // Filled once the fixture settles (Phase 2 — auto-grading).
  betWinning?: boolean | null;
}

export type CouponStatus = 'draft' | 'saved' | 'settled';

export interface Coupon {
  id: string;
  name: string;
  createdAt: string;
  updatedAt: string;
  status: CouponStatus;
  selections: CouponSelection[];
}

export function selectionKey(s: {
  fixtureId: number;
  marketId: number;
  outcomeLabel: string;
  total: string | null;
  handicap: string | null;
  oddValue: number;
}): string {
  return [
    s.fixtureId,
    s.marketId,
    s.outcomeLabel.toLowerCase(),
    s.total ?? '-',
    s.handicap ?? '-',
    s.oddValue.toFixed(4),
  ].join('|');
}
