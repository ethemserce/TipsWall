using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureScoreDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("type_id")]
        public long? TypeId { get; init; }

        [JsonProperty("participant_id")]
        public long? ParticipantId { get; init; }

        [JsonProperty("participant_location")]
        public string? ParticipantLocation { get; init; }

        [JsonProperty("description")]
        public string? Description { get; init; }

        [JsonProperty("goals")]
        public int? Goals { get; init; }
    }
}
