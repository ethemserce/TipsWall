using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    /// <summary>
    /// Pivoted standings row enriched with team info and overall stats —
    /// the shape the mobile fixture-detail standings tab consumes directly.
    /// </summary>
    public sealed class LeagueTableRowDto
    {
        [JsonProperty("team_id")]
        public long? TeamId { get; init; }

        [JsonProperty("team_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? TeamName { get; init; }

        [JsonProperty("team_image_path", NullValueHandling = NullValueHandling.Ignore)]
        public string? TeamImagePath { get; init; }

        [JsonProperty("position")]
        public int? Position { get; init; }

        [JsonProperty("played")]
        public int Played { get; init; }

        [JsonProperty("wins")]
        public int Wins { get; init; }

        [JsonProperty("draws")]
        public int Draws { get; init; }

        [JsonProperty("losses")]
        public int Losses { get; init; }

        [JsonProperty("goals_for")]
        public int GoalsFor { get; init; }

        [JsonProperty("goals_against")]
        public int GoalsAgainst { get; init; }

        [JsonProperty("goal_difference")]
        public int GoalDifference { get; init; }

        [JsonProperty("points")]
        public int Points { get; init; }
    }
}
