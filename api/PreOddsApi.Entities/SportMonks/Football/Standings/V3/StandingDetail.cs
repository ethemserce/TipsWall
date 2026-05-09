using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;

namespace PreOddsApi.Entities.SportMonks.Football.Standings.V3
{
    public class StandingDetail : SportMonksBaseEntity
    {
        [JsonProperty("standing_type")]
        public string StandingType { get; set; }

        [JsonProperty("standing_id")]
        public long? StandingId { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("type")] // if add to Query Params "include=type"
        public Types Type { get; set; }
    }
}
