using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Player : SportMonksBaseEntity
    {
        [JsonProperty("sport_id")]
        public long? SportId { get; set; }

        [JsonProperty("country_id")]
        public long? CountryId { get; set; }

        [JsonProperty("nationality_id")]
        public long? NationalityId { get; set; }

        [JsonProperty("city_id")]
        public long? CityId { get; set; }

        [JsonProperty("position_id")]
        public long? PositionId { get; set; }

        [JsonProperty("detailed_position_id")]
        public long? DetailedPositionId { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

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

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("image_path")]
        public string ImagePath { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("weight")]
        public int? Weight { get; set; }
        
        [JsonProperty("date_of_birth")]
        public DateOnly DateOfBirth { get; set; }

        [JsonProperty("sport")] // if add to Query Params "include=sport"
        public Sport Sport { get; set; }

        [JsonProperty("country")] // if add to Query Params "include=country"
        public Country Country { get; set; }

        [JsonProperty("city")] // if add to Query Params "include=city"
        public City City { get; set; }

        [JsonProperty("nationality")] // if add to Query Params "include=nationality"
        public Continent Nationality { get; set; }

        [JsonProperty("transfers")] // if add to Query Params "include=transfers "
        public List<Transfer> Transfers { get; set; }

        [JsonProperty("pendingTransfers")] // if add to Query Params "include=pendingTransfers "
        public List<Transfer> PendingTransfers { get; set; }

        [JsonProperty("teams")] // if add to Query Params "include=teams"
        public List<Team> Teams { get; set; }

        [JsonProperty("latest")] // if add to Query Params "include=latest"
        public Fixture Latest { get; set; }

        [JsonProperty("lineups")] // if add to Query Params "include=lineups"
        public List<Lineup> Lineups { get; set; }

        [JsonProperty("metadata")] // if add to Query Params "include=metadata"
        public Metadata Metadata { get; set; }

        //[JsonProperty("trophies")] // if add to Query Params "include=trophies"
        //public List<ParticipantTrophy> ParticipantTrophies { get; set; }

        //[JsonProperty("position")] // if add to Query Params "include=position"
        //public string Position { get; set; }

        //[JsonProperty("detailedPosition")] // if add to Query Params "include=detailedPosition "
        //public string DetailedPosition  { get; set; }

        //[JsonProperty("statistics")] // if add to Query Params "include=statistics"
        //public List<Statistic> Stages { get; set; }
    }
}
