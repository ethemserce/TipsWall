using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class ScheduleScore
    {
        [JsonProperty("id")]
        public long Id { get; set; }

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
