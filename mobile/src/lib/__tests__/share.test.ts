import { fixtureDeepLink, shareCoupon, shareFixture } from '@/src/lib/share';
import type { Coupon } from '@/src/lib/coupons/types';

// Mock RN Share + expo-linking with deterministic outputs so we can
// assert the message body — share.ts builds and we just check the
// arguments to Share.share.
const shareSpy = jest.fn().mockResolvedValue({ action: 'sharedAction' });

jest.mock('react-native', () => ({
  Share: { share: (...args: unknown[]) => shareSpy(...args) },
}));

jest.mock('expo-linking', () => ({
  createURL: (path: string) => `preoddsmobile://${path.replace(/^\/+/, '')}`,
}));

beforeEach(() => {
  shareSpy.mockClear();
});

const PAST = '2020-01-01T00:00:00Z';
const baseCoupon: Coupon = {
  id: 'c1',
  name: 'Cumartesi Kuponum',
  createdAt: PAST,
  updatedAt: PAST,
  status: 'saved',
  selections: [
    {
      id: 's1',
      fixtureId: 100,
      fixtureName: 'Galatasaray - Fenerbahçe',
      startingAt: PAST,
      bookmakerId: 2,
      marketId: 1,
      marketShort: 'MS',
      outcomeLabel: 'Home',
      outcomeDisplay: '1',
      total: null,
      handicap: null,
      oddValue: 1.85,
      dso: null,
      vbet: null,
      iko: null,
      sampleCount: null,
    },
    {
      id: 's2',
      fixtureId: 200,
      fixtureName: 'Beşiktaş - Trabzonspor',
      startingAt: PAST,
      bookmakerId: 2,
      marketId: 14,
      marketShort: 'KG',
      outcomeLabel: 'Yes',
      outcomeDisplay: 'Var',
      total: null,
      handicap: null,
      oddValue: 1.6,
      dso: null,
      vbet: null,
      iko: null,
      sampleCount: null,
    },
  ],
};

describe('fixtureDeepLink', () => {
  test('builds an app-scheme URL containing the fixture id', () => {
    expect(fixtureDeepLink(123)).toBe('preoddsmobile://fixture/123');
  });

  test('accepts string ids the same way', () => {
    expect(fixtureDeepLink('abc')).toBe('preoddsmobile://fixture/abc');
  });
});

describe('shareFixture', () => {
  test('passes title, message and url to native Share', async () => {
    await shareFixture(100, 'Galatasaray - Fenerbahçe');
    expect(shareSpy).toHaveBeenCalledTimes(1);
    const arg = shareSpy.mock.calls[0][0];
    expect(arg.title).toBe('Galatasaray - Fenerbahçe');
    expect(arg.message).toContain('Galatasaray - Fenerbahçe');
    expect(arg.message).toContain('preoddsmobile://fixture/100');
    expect(arg.message).toContain("TipsWall'da incele");
    expect(arg.url).toBe('preoddsmobile://fixture/100');
  });
});

describe('shareCoupon', () => {
  test('emits one line per pick with market + outcome (no odd)', async () => {
    await shareCoupon(baseCoupon);
    expect(shareSpy).toHaveBeenCalledTimes(1);
    const message = shareSpy.mock.calls[0][0].message as string;
    // First pick: MS 1
    expect(message).toContain('MS 1');
    // Second pick: KG Var (translated outcomeDisplay)
    expect(message).toContain('KG Var');
    // Pick count summary
    expect(message).toContain('Toplam 2 tahmin');
    expect(shareSpy.mock.calls[0][0].title).toBe('Cumartesi Kuponum');
  });

  test('falls back to outcomeLabel when outcomeDisplay is missing', async () => {
    const coupon: Coupon = {
      ...baseCoupon,
      selections: [
        {
          ...baseCoupon.selections[0],
          outcomeDisplay: undefined,
          outcomeLabel: 'Home',
        },
      ],
    };
    await shareCoupon(coupon);
    const message = shareSpy.mock.calls[0][0].message as string;
    expect(message).toContain('MS Home');
  });
});
