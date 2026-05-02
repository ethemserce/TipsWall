using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Coach
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("player_id")]
        public long PlayerId { get; set; }

        [JsonProperty("sport_id")]
        public long SportId { get; set; }

        [JsonProperty("country_id")]
        public long CountryId { get; set; }

        [JsonProperty("nationality_id")]
        public long NationalityId { get; set; }

        [JsonProperty("city_id")]
        public long CityId { get; set; }

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
        public Uri ImagePath { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("weight")]
        public int Weight { get; set; }

        [JsonProperty("date_of_birth")]
        public DateOnly DateOfBirth { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("country")] // if add to Query Params "include=country"
        public Country Country { get; set; }

        [JsonProperty("teams")] // if add to Query Params "include=teams"
        public List<Team> Teams { get; set; }

        [JsonProperty("player")] // if add to Query Params "include=player"
        public Player Players { get; set; }

        [JsonProperty("fixtures")] // if add to Query Params "include=latest"
        public List<Fixture> Fixtures { get; set; }

        //[JsonProperty("nationality")] // if add to Query Params "include=nationality"
        //public Nationality Nationality { get; set; }

        //[JsonProperty("trophies")] // if add to Query Params "include=trophies"
        //public List<ParticipantTrophy> ParticipantTrophies { get; set; }

        //[JsonProperty("statistics")] // if add to Query Params "include=statistics"
        //public List<Statistic> Stages { get; set; }
    }
}
