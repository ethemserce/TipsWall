using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Football.V3;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Core.V3
{
    public class Country : SportMonksBaseEntity
    {
        public Country()
        {
            this.Regions = new List<Region>();
        }
        [JsonProperty("continent_id")]
        public long ContinentId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("official_name")]
        public string OfficialName { get; set; }

        [JsonProperty("fifa_name")]
        public string FifaName { get; set; }

        [JsonProperty("iso2")]
        public string Iso2 { get; set; }

        [JsonProperty("iso3")]
        public string Iso3 { get; set; }

        [JsonProperty("latitude")]
        public string Latitude { get; set; }

        [JsonProperty("longitude")]
        public string Longitude { get; set; }

        [JsonProperty("image_path")]
        public string ImagePath { get; set; }

        //[JsonProperty("borders")]
        //public string[] Borders { get; set; }

        [JsonProperty("continent")]
        public Continent Continent { get; set; }

        [JsonProperty("regions")]
        public List<Region> Regions { get; set; }

        //[JsonProperty("leagues")]
        //public List<League> Leagues { get; set; }
    }
}
