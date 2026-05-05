using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureParticipantDto
    {
        [JsonProperty("team_id")]
        public long TeamId { get; init; }

        [JsonProperty("location")]
        public string Location { get; init; } = string.Empty;

        [JsonProperty("winner")]
        public bool? Winner { get; init; }

        [JsonProperty("position")]
        public int? Position { get; init; }
    }
}
