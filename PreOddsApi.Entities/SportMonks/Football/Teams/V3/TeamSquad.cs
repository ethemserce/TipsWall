using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class TeamSquad : SportMonksBaseEntity
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("transfer_id")]
        public long? TransferId { get; set; }

        [JsonProperty("player_id")]
        public long PlayerId { get; set; }

        [JsonProperty("team_id")]
        public long TeamId { get; set; }

        [JsonProperty("position_id")]
        public int PositionId { get; set; }

        [JsonProperty("detailed_position_id")]
        public int DetailedPositionId { get; set; }

        [JsonProperty("jersey_number")]
        public int JerseyNumber { get; set; }

        [JsonProperty("start")]
        public DateTimeOffset Start { get; set; }

        [JsonProperty("end")]
        public DateTimeOffset End { get; set; }

        [JsonProperty("team")] // if add to Query Params "include=team"
        public Team Team { get; set; }

        [JsonProperty("player")] // if add to Query Params "include=player"
        public Player Player { get; set; }

        [JsonProperty("transfer")] // if add to Query Params "include=transfer"
        public Transfer Transfer { get; set; }

        //[JsonProperty("position")] // if add to Query Params "include=position"
        //public Position Position { get; set; }

        //[JsonProperty("detailedPosition")] // if add to Query Params "include=detailedPosition"
        //public Position Position { get; set; }
    }
}
