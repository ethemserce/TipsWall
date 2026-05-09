using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Core.V3
{
    public class Sport : SportMonksBaseEntity
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }
}
