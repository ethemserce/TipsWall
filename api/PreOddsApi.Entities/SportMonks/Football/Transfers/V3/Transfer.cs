using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using System;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Transfer
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("sport_id")]
        public long? SportId { get; set; }

        [JsonProperty("player_id")]
        public long? PlayerId { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

        [JsonProperty("from_team_id")]
        public long? FromTeamId { get; set; }

        [JsonProperty("to_team_id")]
        public long? ToTeamId { get; set; }

        [JsonProperty("position_id")]
        public long? PositionId { get; set; }

        [JsonProperty("detailed_position_id")]
        public long? DetailedPositionId { get; set; }

        [JsonProperty("date")]
        public DateOnly? Date { get; set; }

        [JsonProperty("career_ended")]
        public bool CareerEnded { get; set; }

        [JsonProperty("completed")]
        public bool Completed { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("sport")] // if add to Query Params "include=sport"
        public Sport Sport { get; set; }

        [JsonProperty("player")] // if add to Query Params "include=player"
        public Player Player { get; set; }

        [JsonProperty("type")] // if add to Query Params "include=type"
        public Types Type { get; set; }

        [JsonProperty("position")] // if add to Query Params "include=position"
        public Types Position { get; set; }

        [JsonProperty("detailedPosition")] // if add to Query Params "include=detailedPosition"
        public Types DetailedPosition { get; set; }

        [JsonProperty("fromTeam")] // if add to Query Params "include=fromTeam"
        public Team FromTeam { get; set; }

        [JsonProperty("toTeam")] // if add to Query Params "include=toTeam"
        public Team ToTeam { get; set; }
    }
}
