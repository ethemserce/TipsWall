using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Sideline : SportMonksBaseEntity
    {
        [JsonProperty("player_id")]
        public long? PlayerId { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

        [JsonProperty("team_id")]
        public long? TeamId { get; set; }

        [JsonProperty("season_id")]
        public long? SeasonId { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("start_date")]
        public string StartDate { get; set; }

        [JsonProperty("end_date")]
        public string EndDate { get; set; }

        [JsonProperty("games_missed")]
        public int? GamesMissed { get; set; }

        [JsonProperty("completed")]
        public bool Completed { get; set; }

        [JsonProperty("player")] // if add to Query Params "include=player"
        public Player Player { get; set; }

        [JsonProperty("type")] // if add to Query Params "include=type"
        public Types Type { get; set; }

        [JsonProperty("team")] // if add to Query Params "include=team"
        public Team Team { get; set; }

        [JsonProperty("season")] // if add to Query Params "include=season"
        public Season Season { get; set; }
    }
}
