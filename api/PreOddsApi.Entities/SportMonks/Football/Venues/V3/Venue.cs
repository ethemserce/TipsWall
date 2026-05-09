using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Venue : SportMonksBaseEntity
    {

        [JsonProperty("country_id")]
        public long? CountryId { get; set; }

        [JsonProperty("city_id")]
        public long? CityId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("zipcode")]
        public string Zipcode { get; set; }

        [JsonProperty("latitude")]
        public string Latitude { get; set; }

        [JsonProperty("longitude")]
        public string Longitude { get; set; }

        [JsonProperty("capacity")]
        public int? Capacity { get; set; }

        [JsonProperty("image_path")]
        public string? ImagePath { get; set; }

        [JsonProperty("city_name")]
        public string CityName { get; set; }

        [JsonProperty("surface")]
        public string Surface { get; set; }

        [JsonProperty("national_team")]
        public bool NationalTeam { get; set; }

        [JsonProperty("country")] // if add to Query Params "include=country"
        public Country Country { get; set; }

        [JsonProperty("city")] // if add to Query Params "include=city"
        public City City { get; set; }

        [JsonProperty("fixtures")] // if add to Query Params "include=fixtures"
        public List<Fixture> Fixtures { get; set; }
    }
}
