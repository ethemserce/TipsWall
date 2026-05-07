using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureSummaryDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("name")]
        public string? Name { get; init; }

        [JsonProperty("league_id")]
        public long LeagueId { get; init; }

        [JsonProperty("season_id")]
        public long? SeasonId { get; init; }

        [JsonProperty("stage_id")]
        public long? StageId { get; init; }

        [JsonProperty("round_id")]
        public long? RoundId { get; init; }

        [JsonProperty("state_id")]
        public long? StateId { get; init; }

        [JsonProperty("venue_id")]
        public long? VenueId { get; init; }

        [JsonProperty("starting_at")]
        public DateTimeOffset? StartingAt { get; init; }

        [JsonProperty("has_odds")]
        public bool HasOdds { get; init; }

        [JsonProperty("has_premium_odds")]
        public bool HasPremiumOdds { get; init; }

        [JsonProperty("length_minutes")]
        public int? LengthMinutes { get; init; }

        [JsonProperty("result_info")]
        public string? ResultInfo { get; init; }

        [JsonProperty("leg")]
        public string? Leg { get; init; }

        [JsonProperty("placeholder")]
        public bool Placeholder { get; init; }

        [JsonProperty("home_team_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? HomeTeamId { get; init; }

        [JsonProperty("home_team_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? HomeTeamName { get; init; }

        [JsonProperty("home_team_short_code", NullValueHandling = NullValueHandling.Ignore)]
        public string? HomeTeamShortCode { get; init; }

        [JsonProperty("home_team_image_path", NullValueHandling = NullValueHandling.Ignore)]
        public string? HomeTeamImagePath { get; init; }

        [JsonProperty("home_score", NullValueHandling = NullValueHandling.Ignore)]
        public int? HomeScore { get; init; }

        [JsonProperty("away_team_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? AwayTeamId { get; init; }

        [JsonProperty("away_team_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? AwayTeamName { get; init; }

        [JsonProperty("away_team_short_code", NullValueHandling = NullValueHandling.Ignore)]
        public string? AwayTeamShortCode { get; init; }

        [JsonProperty("away_team_image_path", NullValueHandling = NullValueHandling.Ignore)]
        public string? AwayTeamImagePath { get; init; }

        [JsonProperty("away_score", NullValueHandling = NullValueHandling.Ignore)]
        public int? AwayScore { get; init; }
    }
}
