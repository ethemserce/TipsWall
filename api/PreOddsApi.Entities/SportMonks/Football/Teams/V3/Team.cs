using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Team : SportMonksBaseEntity
    {

        [JsonProperty("sport_id")]
        public long? SportId { get; set; }

        [JsonProperty("country_id")]
        public long? CountryId { get; set; }

        [JsonProperty("venue_id")]
        public long? VenueId { get; set; }

        [JsonProperty("gender")]
        public string? Gender { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("short_code")]
        public string? ShortCode { get; set; }

        [JsonProperty("image_path")]
        public string? ImagePath { get; set; }

        [JsonProperty("founded")]
        public long? Founded { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("placeholder")]
        public bool? Placeholder { get; set; }

        [JsonProperty("last_played_at")]
        public string? LastPlayedAt { get; set; }

        [JsonProperty("sport")] // if add to Query Params "include=sport"
        public Sport Sport { get; set; }

        [JsonProperty("country")] // if add to Query Params "include=country"
        public Country Country { get; set; }

        [JsonProperty("venue")] // if add to Query Params "include=venue"
        public Venue Venue { get; set; }

        [JsonProperty("coaches")] // if add to Query Params "include=coaches"
        public List<Coach> Coaches { get; set; }

        [JsonProperty("rivals")] // if add to Query Params "include=rivals"
        public List<Rival> Rivals { get; set; }

        [JsonProperty("players")] // if add to Query Params "include=players"
        public List<Player> Players { get; set; }

        [JsonProperty("latest")] // if add to Query Params "include=latest"
        public Fixture Latest { get; set; }

        [JsonProperty("upcoming")] // if add to Query Params "include=upcoming"
        public Fixture Upcoming { get; set; }

        [JsonProperty("seasons")] // if add to Query Params "include=seasons"
        public List<Season> Seasons { get; set; }

        [JsonProperty("activeSeasons")] // if add to Query Params "include=activeSeasons"
        public List<Season> ActiveSeasons { get; set; }

        [JsonProperty("sidelined")] // if add to Query Params "include=sidelined"
        public Sidelined Sidelined { get; set; }

        [JsonProperty("sidelinedHistory")] // if add to Query Params "include=sidelinedHistory"
        public Sidelined SidelinedHistory { get; set; }

        //[JsonProperty("socials")] // if add to Query Params "include=socials"
        //public List<Social> Socials { get; set; }

        //[JsonProperty("trophies")] // if add to Query Params "include=trophies"
        //public List<ParticipantTrophy> ParticipantTrophies { get; set; }

        //[JsonProperty("statistics")] // if add to Query Params "include=statistics"
        //public List<Statistic> Stages { get; set; }
    }
}
