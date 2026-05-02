using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class State : SportMonksBaseEntity
    {

        [JsonProperty("state")]
        public string StateCode { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("short_name")]
        public string ShortName { get; set; }

        [JsonProperty("developer_name")]
        public string DeveloperName { get; set; }

        [JsonProperty("type")] // if add to Query Params "include=type"
        public Types Type { get; set; }
    }
}
