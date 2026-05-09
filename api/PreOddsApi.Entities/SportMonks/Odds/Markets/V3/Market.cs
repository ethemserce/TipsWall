using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Odds.V3
{
    public class Market : SportMonksBaseEntity
    {

        [JsonProperty("legacy_id")]
        public long? LegacyId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("developer_name")]
        public string DeveloperName { get; set; }

        [JsonProperty("has_winning_calculations")]
        public bool HasWinningCalculations { get; set; }
    }
}
