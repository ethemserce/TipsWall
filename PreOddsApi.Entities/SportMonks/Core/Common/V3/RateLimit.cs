using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Core.Common.V3
{
    public class RateLimit
    {
        [JsonProperty("resets_in_seconds")]
        public long ResetsInSeconds { get; set; }

        [JsonProperty("remaining")]
        public long Remaining { get; set; }

        [JsonProperty("requested_entity")]
        public string RequestedEntity { get; set; }
    }
}
