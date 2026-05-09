using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    /// <summary>
    /// Aggregate stats across the filtered rate_result rows. Mirrors the
    /// legacy "SummaryResult" panel: total signals + samples + win/fail
    /// breakdown computed against odds.prematch_odds_current.winning.
    /// </summary>
    public sealed class RateSummaryDto
    {
        public int TotalSignals { get; init; }

        public int TotalSamples { get; init; }

        public decimal? AvgWinningPercent { get; init; }

        public decimal? AvgEarningPercent { get; init; }

        public decimal? AvgOddValue { get; init; }

        // Outcome verification (only matches that have actually settled count).
        public int BetTotal { get; init; }

        public int SuccessCount { get; init; }

        public int FailCount { get; init; }

        public decimal? EarningTotal { get; init; }
    }

    /// <summary>
    /// Wrapper returned by every /api/v3/{kind}-rate endpoint. Carries the
    /// page items together with the aggregate summary and the freshness
    /// stamp so mobile can invalidate its cache when analytics has refreshed.
    /// </summary>
    public sealed class RateListResponseDto
    {
        public System.Collections.Generic.IReadOnlyList<RateResultDto> Items { get; init; }
            = System.Array.Empty<RateResultDto>();

        public RateSummaryDto Summary { get; init; } = new();

        public DateTime? AsOfDate { get; init; }
    }
}
