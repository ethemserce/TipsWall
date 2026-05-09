using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Rival
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("sport_id")]
        public long SportId { get; set; }

        [JsonProperty("team_id")]
        public long TeamId { get; set; }

        [JsonProperty("rival_id")]
        public long RivalId { get; set; }

        [JsonProperty("team")] // if add to Query Params "include=team"
        public Team Team { get; set; }

        [JsonProperty("rival")] // if add to Query Params "include=rival"
        public Team RivalTeam { get; set; }
    }
}
