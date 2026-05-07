using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureEventDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("minute")]
        public int? Minute { get; init; }

        [JsonProperty("extra_minute")]
        public int? ExtraMinute { get; init; }

        [JsonProperty("type_id")]
        public long? TypeId { get; init; }

        [JsonProperty("type_code")]
        public string? TypeCode { get; init; }

        [JsonProperty("type_name")]
        public string? TypeName { get; init; }

        [JsonProperty("participant_id")]
        public long? ParticipantId { get; init; }

        [JsonProperty("participant_location")]
        public string? ParticipantLocation { get; init; }

        [JsonProperty("player_id")]
        public long? PlayerId { get; init; }

        [JsonProperty("player_name")]
        public string? PlayerName { get; init; }

        [JsonProperty("related_player_name")]
        public string? RelatedPlayerName { get; init; }

        [JsonProperty("result")]
        public string? Result { get; init; }

        [JsonProperty("info")]
        public string? Info { get; init; }
    }
}
