using System.Collections.Generic;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureOddsRatesDto
    {
        [JsonProperty("market_id")]
        public long MarketId { get; init; }

        [JsonProperty("market_name")]
        public string? MarketName { get; init; }

        [JsonProperty("outcomes")]
        public IReadOnlyList<FixtureOddOutcomeDto> Outcomes { get; init; }
            = new List<FixtureOddOutcomeDto>();
    }

    public sealed class FixtureOddOutcomeDto
    {
        [JsonProperty("label")]
        public string Label { get; init; } = string.Empty;

        [JsonProperty("value")]
        public decimal? Value { get; init; }

        [JsonProperty("total")]
        public string? Total { get; init; }

        [JsonProperty("handicap")]
        public string? Handicap { get; init; }

        [JsonProperty("participants")]
        public string? Participants { get; init; }

        [JsonProperty("sort_order")]
        public int? SortOrder { get; init; }

        [JsonProperty("win_count")]
        public int WinCount { get; init; }

        [JsonProperty("lost_count")]
        public int LostCount { get; init; }

        [JsonProperty("sample_count")]
        public int SampleCount { get; init; }

        [JsonProperty("winning_percent")]
        public decimal? WinningPercent { get; init; }

        [JsonProperty("earning_percent")]
        public decimal? EarningPercent { get; init; }

        /// <summary>
        /// Whether SportMonks has settled this outcome as winning. Null when
        /// the match isn't finished or the market isn't auto-graded — the
        /// mobile UI uses this as a fallback when score-based grading can't
        /// resolve the outcome (e.g. half-only markets).
        /// </summary>
        [JsonProperty("winning", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Winning { get; init; }
    }
}
