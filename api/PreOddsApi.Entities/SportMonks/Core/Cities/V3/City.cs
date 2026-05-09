using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Core.V3
{
    public class City : SportMonksBaseEntity
    {

        [JsonProperty("country_id")]
        public long? CountryId { get; set; }

        [JsonProperty("region_id")]
        public long? RegionId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("latitude")]
        public string Latitude { get; set; }

        [JsonProperty("longitude")]
        public string Longitude { get; set; }
    }
}
