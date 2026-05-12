using System.Collections.Generic;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureOddsRatesDto
    {
        public long MarketId { get; init; }

        public string? MarketName { get; init; }

        public IReadOnlyList<FixtureOddOutcomeDto> Outcomes { get; init; }
            = new List<FixtureOddOutcomeDto>();
    }

    public sealed class FixtureOddOutcomeDto
    {
        public string Label { get; init; } = string.Empty;

        public string? Total { get; init; }

        public string? Handicap { get; init; }

        public string? Participants { get; init; }

        public int? SortOrder { get; init; }

        public int WinCount { get; init; }

        public int LostCount { get; init; }

        public int SampleCount { get; init; }

        public decimal? WinningPercent { get; init; }

        public decimal? EarningPercent { get; init; }

        /// <summary>
        /// No-vig implied probability — bookmaker's view of the chance after
        /// stripping the vig. Computed server-side now that the raw odd value
        /// is no longer exposed; mobile cards render it as the IMP gauge.
        /// </summary>
        public decimal? Iko { get; init; }

        /// <summary>
        /// Raw decimal odd value as the bookmaker quotes it. Re-added behind
        /// a settings-toggle on the mobile side: by default the UI keeps the
        /// no-betting framing and hides this, but a user who wants the
        /// number can flip it on under Settings → Display.
        /// </summary>
        public decimal? Value { get; init; }

        /// <summary>
        /// Whether SportMonks has settled this outcome as winning. Null when
        /// the match isn't finished or the market isn't auto-graded — the
        /// mobile UI uses this as a fallback when score-based grading can't
        /// resolve the outcome (e.g. half-only markets).
        /// </summary>
        public bool? Winning { get; init; }
    }
}
