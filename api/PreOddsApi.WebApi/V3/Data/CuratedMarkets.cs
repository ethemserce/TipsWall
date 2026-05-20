using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace PreOddsApi.WebApi.V3.Data
{
    /// <summary>
    /// Curated set of score-based markets the product surfaces by default.
    /// Tiered: guests see the top-3, registered users see top-10, premium
    /// gets the full 30. The order mirrors the priority list agreed with
    /// the product owner — earliest = most important / most-trafficked.
    ///
    /// Selection criteria:
    ///   * Every entry maps to a developer_name the odds.evaluate_outcome
    ///     plpgsql function already grades, so HIT/ROI/IMP columns aren't
    ///     empty when the row renders.
    ///   * No corners / cards / player props — those need fixture-event
    ///     data we don't yet roll up into snapshots.
    ///   * "Üst/Alt 2.5" + "Üst/Alt 1.5 + 3.5" collapse into one entry
    ///     (GOALS_OVER_UNDER = market_id 80) because filtering is by
    ///     market_id; the mobile UI then shows every total line under
    ///     that market header.
    /// </summary>
    public static class CuratedMarkets
    {
        // Top 3 — visible to anonymous guests + ZERO-config users.
        public static readonly IReadOnlyList<long> Guest = new long[]
        {
            1,   // FULLTIME_RESULT             — Maç Sonucu (1/X/2)
            31,  // HALF_TIME_RESULT            — İlk Yarı Sonucu
            80,  // GOALS_OVER_UNDER            — Üst/Alt (tüm hatlar)
        };

        // Top 10 — registered free users. Includes the guest set + 7 more
        // mainstream markets. CORRECT_SCORE fills the 10th slot because
        // the user's curated list collapsed two GOALS_OVER_UNDER entries
        // ("Üst/Alt 2.5" + "Üst/Alt 1.5 ve 3.5") into the same market_id.
        public static readonly IReadOnlyList<long> Registered = new long[]
        {
            1,   // FULLTIME_RESULT
            31,  // HALF_TIME_RESULT
            80,  // GOALS_OVER_UNDER
            2,   // DOUBLE_CHANCE              — Çifte Şans
            10,  // DRAW_NO_BET                — Beraberlikte İade
            14,  // BOTH_TEAMS_TO_SCORE        — Karşılıklı Gol
            28,  // 1ST_HALF_GOALS             — İY Üst/Alt
            29,  // HALF_TIME_FULL_TIME        — İY/MS
            13,  // RESULT_BOTH_TEAMS_TO_SCORE — Sonuç + KG
            57,  // CORRECT_SCORE              — Doğru Skor
        };

        // Top 30 — premium tier. Adds 20 deeper markets the evaluator
        // can grade.
        public static readonly IReadOnlyList<long> Premium = new long[]
        {
            1, 31, 80, 2, 10, 14, 28, 29, 13, 57,            // top 10
            6,   // ASIAN_HANDICAP
            9,   // 3_WAY_HANDICAP
            93,  // EXACT_TOTAL_GOALS
            15,  // BOTH_TEAMS_TO_SCORE_IN_1ST_HALF
            16,  // BOTH_TEAMS_TO_SCORE_IN_2ND_HALF
            97,  // 2ND_HALF_RESULT
            53,  // 2ND_HALF_GOALS
            18,  // HOME_TEAM_EXACT_GOALS
            19,  // AWAY_TEAM_EXACT_GOALS
            44,  // ODD_EVEN
            45,  // ODD_EVEN_1ST_HALF
            124, // 2ND_HALF_GOALS_ODD_EVEN
            50,  // CLEAN_SHEET_HOME
            51,  // CLEAN_SHEET_AWAY
            36,  // HOME_TEAM_TO_SCORE
            35,  // AWAY_TEAM_TO_SCORE
            30,  // HALF_TIME_CORRECT_SCORE
            46,  // WIN_TO_NIL
            37,  // RESULT_TOTAL_GOALS
            56,  // HANDICAP_RESULT
        };

        // Tier → cap. Used both for the upper bound on the picker and
        // for the size of the auto-filled default list when the user
        // has no saved preference yet.
        //
        // Backing fields are runtime-configurable: Program.cs calls
        // CuratedMarkets.Configure(builder.Configuration) at startup so
        // appsettings / env-var overrides take effect without a code
        // change. Defaults below are the launch-day values (guest 3 /
        // free 10 / premium 30) — production can later dial these to
        // tighten the free band (e.g. drop to 5) by setting
        // MarketCaps__Free=5 in the deploy env, no rebuild required.
        private static int _guestCap = 3;
        private static int _registeredCap = 10;
        private static int _premiumCap = 30;

        public static int GuestCap => _guestCap;
        public static int RegisteredCap => _registeredCap;
        public static int PremiumCap => _premiumCap;

        /// <summary>
        /// Reads MarketCaps:Guest / MarketCaps:Free / MarketCaps:Premium
        /// from configuration. Missing keys fall back to the launch
        /// defaults. Safe to call multiple times (e.g. test re-init);
        /// each call replaces the cached values.
        /// </summary>
        public static void Configure(IConfiguration configuration)
        {
            var section = configuration.GetSection("MarketCaps");
            _guestCap = section.GetValue("Guest", 3);
            _registeredCap = section.GetValue("Free", 10);
            _premiumCap = section.GetValue("Premium", 30);
        }

        public static int CapFor(string tier)
            => tier == "premium" ? _premiumCap
             : tier == "free" ? _registeredCap
             : _guestCap;

        /// <summary>
        /// Email-verification-aware variant. An unverified free user only
        /// gets the guest cap — the wider free-tier picker unlocks
        /// after they click the email verification link. Premium tier
        /// always trusts its own cap; the IAP flow has its own identity
        /// checks separate from this email gate.
        /// </summary>
        public static int CapFor(string tier, bool emailVerified)
        {
            if (tier == "free" && !emailVerified) return _guestCap;
            return CapFor(tier);
        }

        public static IReadOnlyList<long> DefaultsFor(string tier)
            => tier == "premium" ? Premium
             : tier == "free" ? Registered
             : Guest;
    }
}
