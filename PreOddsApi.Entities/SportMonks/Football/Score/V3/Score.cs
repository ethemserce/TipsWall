using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Score : SportMonksBaseEntity
    {

        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("type_id")]
        public long TypeId { get; set; }

        [JsonProperty("participant_id")]
        public long ParticipantId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("score")]
        public ScheduleScoreGoal Goal { get; set; }
    }
}
