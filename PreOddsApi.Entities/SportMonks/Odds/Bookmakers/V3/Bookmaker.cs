using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Odds.V3
{
    public class Bookmaker : SportMonksBaseEntity
    {
        [JsonProperty("legacy_id")]
        public long? LegacyId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
