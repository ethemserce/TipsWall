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

        public decimal? Value { get; init; }

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
        /// Whether SportMonks has settled this outcome as winning. Null when
        /// the match isn't finished or the market isn't auto-graded — the
        /// mobile UI uses this as a fallback when score-based grading can't
        /// resolve the outcome (e.g. half-only markets).
        /// </summary>
        public bool? Winning { get; init; }
    }
}
