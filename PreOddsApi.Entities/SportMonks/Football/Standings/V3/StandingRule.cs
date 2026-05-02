using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;

namespace PreOddsApi.Entities.SportMonks.Football.Standings.V3
{
    public class StandingRule : SportMonksBaseEntity
    {
        [JsonProperty("model_type")]
        public string ModelType { get; set; }

        [JsonProperty("model_id")]
        public long ModelId { get; set; }

        [JsonProperty("type_id")]
        public long TypeId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("type")] // if add to Query Params "include=type"
        public Types Type { get; set; }
    }
}
