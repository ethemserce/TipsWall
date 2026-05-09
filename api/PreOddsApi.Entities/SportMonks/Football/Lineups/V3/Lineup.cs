using Newtonsoft.Json;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Lineup : SportMonksBaseEntity
    {
        [JsonProperty("sport_id")]
        public long? SportId { get; set; }

        [JsonProperty("fixture_id")]
        public long? FixtureId { get; set; }

        [JsonProperty("player_id")]
        public long? PlayerId { get; set; }

        [JsonProperty("team_id")]
        public long? TeamId { get; set; }

        [JsonProperty("position_id")]
        public long? PositionId { get; set; }

        [JsonProperty("formation_field")]
        public string? FormationField { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

        [JsonProperty("jersey_number")]
        public int? JerseyNumber { get; set; }

        [JsonProperty("formation_position")]
        public int? FormationPosition { get; set; }

        [JsonProperty("player_name")]
        public string PlayerName { get; set; }

        [JsonProperty("player")] // if add to Query Params "include=lineups"
        public Player Player { get; set; }

        [JsonProperty("details")]
        public List<LineupDetail> Details { get; set; }
    }
}
