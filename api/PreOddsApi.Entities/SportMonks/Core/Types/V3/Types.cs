using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Core.V3
{
    public class Types : SportMonksBaseEntity
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("developer_name")]
        public string DeveloperName { get; set; }

        [JsonProperty("model_type")]
        public string ModelType { get; set; }

        [JsonProperty("stat_group")]
        public string StatGroup { get; set; }
    }
}
