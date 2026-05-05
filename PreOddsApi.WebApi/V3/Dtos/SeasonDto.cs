using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class SeasonDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("league_id")]
        public long LeagueId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; } = string.Empty;

        [JsonProperty("finished")]
        public bool Finished { get; init; }

        [JsonProperty("pending")]
        public bool Pending { get; init; }

        [JsonProperty("is_current")]
        public bool IsCurrent { get; init; }

        [JsonProperty("starting_at")]
        public DateTime? StartingAt { get; init; }

        [JsonProperty("ending_at")]
        public DateTime? EndingAt { get; init; }
    }
}
