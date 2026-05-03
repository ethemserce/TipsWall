using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.Statistics.V3;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Referee : SportMonksBaseEntity
    {

        [JsonProperty("sport_id")]
        public long? SportId { get; set; }

        [JsonProperty("country_id")]
        public long? CountryId { get; set; }
        [JsonProperty("nationality_id")]
        public long? NationalityId { get; set; }

        [JsonProperty("city_id")]
        public long? CityId { get; set; }

        [JsonProperty("common_name")]
        public string CommonName { get; set; }

        [JsonProperty("firstname")]
        public string FirstName { get; set; }

        [JsonProperty("lastname")]
        public string LastName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("image_path")]
        public string ImagePath { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("weight")]
        public int? Weight { get; set; }

        [JsonProperty("date_of_birth")]
        public DateOnly? DateOfBirth { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("sport")] // if add to Query Params "include=sport"
        public Sport Sport { get; set; }

        [JsonProperty("country")] // if add to Query Params "include=country"
        public Country Country { get; set; }

        [JsonProperty("city")] // if add to Query Params "include=city"
        public City City { get; set; }

        [JsonProperty("statistics")] // if add to Query Params "include=statistics"
        public List<Statistic> Statistics { get; set; }

        //[JsonProperty("nationality")] // if add to Query Params "include=nationality"
        //public Nationality Nationality { get; set; }
    }
}
