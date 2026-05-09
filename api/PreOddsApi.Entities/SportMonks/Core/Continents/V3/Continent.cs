using Newtonsoft.Json;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Core.V3
{
    public class Continent : SportMonksBaseEntity
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("countries")]
        public List<Country> Countries { get; set; }
    }
}
