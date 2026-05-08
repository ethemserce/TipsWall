using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class RateResultDto
    {
        [JsonProperty("id")]
        public Guid Id { get; init; }

        [JsonProperty("fixture_id")]
        public long FixtureId { get; init; }

        [JsonProperty("fixture_signal_id")]
        public Guid? FixtureSignalId { get; init; }

        [JsonProperty("bookmaker_id")]
        public long BookmakerId { get; init; }

        [JsonProperty("market_id")]
        public long MarketId { get; init; }

        [JsonProperty("window_code")]
        public string WindowCode { get; init; } = string.Empty;

        [JsonProperty("outcome_key")]
        public string OutcomeKey { get; init; } = string.Empty;

        [JsonProperty("label")]
        public string Label { get; init; } = string.Empty;

        [JsonProperty("odd_value")]
        public decimal? OddValue { get; init; }

        [JsonProperty("total")]
        public string? Total { get; init; }

        [JsonProperty("handicap")]
        public string? Handicap { get; init; }

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

        [JsonProperty("rank_order")]
        public int RankOrder { get; init; }

        [JsonProperty("match_state")]
        public int? MatchState { get; init; }

        /// <summary>
        /// Whether this specific outcome won or lost on the matched fixture.
        /// Null when the bet is not yet settled (match still upcoming or
        /// odds not yet resolved by SportMonks).
        /// </summary>
        [JsonProperty("bet_winning", NullValueHandling = NullValueHandling.Ignore)]
        public bool? BetWinning { get; init; }
    }
}
