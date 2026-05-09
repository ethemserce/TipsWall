using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class RateResultDto
    {
        public Guid Id { get; init; }

        public long FixtureId { get; init; }

        public Guid? FixtureSignalId { get; init; }

        public long BookmakerId { get; init; }

        public long MarketId { get; init; }

        public string WindowCode { get; init; } = string.Empty;

        public string OutcomeKey { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;

        public decimal? OddValue { get; init; }

        public string? Total { get; init; }

        public string? Handicap { get; init; }

        public int WinCount { get; init; }

        public int LostCount { get; init; }

        public int SampleCount { get; init; }

        public decimal? WinningPercent { get; init; }

        public decimal? EarningPercent { get; init; }

        public decimal? ConfidenceScore { get; init; }

        /// <summary>
        /// No-vig implied probability for this outcome within its market —
        /// equivalent to the İKO gauge shown on the mobile cards. Computed
        /// server-side via window function so it can drive the value-only filter.
        /// </summary>
        public decimal? Iko { get; init; }

        public int RankOrder { get; init; }

        public int? MatchState { get; init; }

        /// <summary>
        /// Whether this specific outcome won or lost on the matched fixture.
        /// Null when the bet is not yet settled (match still upcoming or
        /// odds not yet resolved by SportMonks).
        /// </summary>
        public bool? BetWinning { get; init; }
    }
}
