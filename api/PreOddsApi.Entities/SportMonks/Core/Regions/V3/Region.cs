using Newtonsoft.Json;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Core.V3
{
    public class Region : SportMonksBaseEntity
    {
        [JsonProperty("country_id")]
        public long CountryId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("cities")]
        public List<City> Cities { get; set; }
    }
}
