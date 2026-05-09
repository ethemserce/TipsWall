using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class ScoreGoal
    {
        [JsonProperty("goals")]
        public int Goals { get; set; }

        [JsonProperty("participant")]
        public string Participant { get; set; }
    }
}
