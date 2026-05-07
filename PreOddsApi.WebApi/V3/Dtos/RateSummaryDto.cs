using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    /// <summary>
    /// Aggregate stats across the filtered rate_result rows. Mirrors the
    /// legacy "SummaryResult" panel: total signals + samples + win/fail
    /// breakdown computed against odds.prematch_odds_current.winning.
    /// </summary>
    public sealed class RateSummaryDto
    {
        [JsonProperty("total_signals")]
        public int TotalSignals { get; init; }

        [JsonProperty("total_samples")]
        public int TotalSamples { get; init; }

        [JsonProperty("avg_winning_percent")]
        public decimal? AvgWinningPercent { get; init; }

        [JsonProperty("avg_earning_percent")]
        public decimal? AvgEarningPercent { get; init; }

        [JsonProperty("avg_odd_value")]
        public decimal? AvgOddValue { get; init; }

        // Outcome verification (only matches that have actually settled count).
        [JsonProperty("bet_total")]
        public int BetTotal { get; init; }

        [JsonProperty("success_count")]
        public int SuccessCount { get; init; }

        [JsonProperty("fail_count")]
        public int FailCount { get; init; }

        [JsonProperty("earning_total")]
        public decimal? EarningTotal { get; init; }
    }

    /// <summary>
    /// Wrapper returned by every /api/v3/{kind}-rate endpoint. Carries the
    /// page items together with the aggregate summary and the freshness
    /// stamp so mobile can invalidate its cache when analytics has refreshed.
    /// </summary>
    public sealed class RateListResponseDto
    {
        [JsonProperty("items")]
        public System.Collections.Generic.IReadOnlyList<RateResultDto> Items { get; init; }
            = System.Array.Empty<RateResultDto>();

        [JsonProperty("summary")]
        public RateSummaryDto Summary { get; init; } = new();

        [JsonProperty("as_of_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? AsOfDate { get; init; }
    }
}
