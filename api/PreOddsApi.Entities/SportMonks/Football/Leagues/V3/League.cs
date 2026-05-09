using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class League : SportMonksBaseEntity
    {

        [JsonProperty("sport_id")]
        public long SportId { get; set; }

        [JsonProperty("country_id")]
        public long CountryId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("short_code")]
        public string ShortCode { get; set; }

        [JsonProperty("image_path")]
        public string ImagePath { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("sub_type")]
        public string SubType { get; set; }

        [JsonProperty("last_played_at")]
        public DateTime LastPlayedAt { get; set; }

        [JsonProperty("category")]
        public int Category { get; set; }

        [JsonProperty("has_jerseys")]
        public bool HasJerseys { get; set; }

        [JsonProperty("sport")] // if add to Query Params "include=sport"
        public Sport Sport { get; set; }

        [JsonProperty("country")] // if add to Query Params "include=country"
        public Country Country { get; set; }

        [JsonProperty("stages")] // if add to Query Params "include=stages"
        public List<Stage> Stages { get; set; }

        [JsonProperty("latest")] // if add to Query Params "include=latest"
        public Fixture Latest { get; set; }

        [JsonProperty("upcoming")] // if add to Query Params "include=upcoming"
        public Fixture Upcoming { get; set; }

        [JsonProperty("inplay")] // if add to Query Params "include=inplay"
        public Fixture Inplay { get; set; }

        [JsonProperty("today")] // if add to Query Params "include=today"
        public Fixture Today { get; set; }

        [JsonProperty("currentSeason")] // if add to Query Params "currentSeason"
        public Season CurrentSeason { get; set; }

        [JsonProperty("seasons")] // if add to Query Params "include=seasons"
        public List<Season> Seasons { get; set; }
    }
}
