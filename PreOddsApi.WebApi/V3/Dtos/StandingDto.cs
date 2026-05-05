using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class StandingDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("participant_id")]
        public long? ParticipantId { get; init; }

        [JsonProperty("league_id")]
        public long? LeagueId { get; init; }

        [JsonProperty("season_id")]
        public long? SeasonId { get; init; }

        [JsonProperty("stage_id")]
        public long? StageId { get; init; }

        [JsonProperty("group_id")]
        public long? GroupId { get; init; }

        [JsonProperty("round_id")]
        public long? RoundId { get; init; }

        [JsonProperty("position")]
        public int? Position { get; init; }

        [JsonProperty("result")]
        public string? Result { get; init; }

        [JsonProperty("points")]
        public int? Points { get; init; }
    }
}
