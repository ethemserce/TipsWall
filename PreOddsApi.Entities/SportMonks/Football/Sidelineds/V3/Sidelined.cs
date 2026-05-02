using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Sidelined : SportMonksBaseEntity
    {
        [JsonProperty("fixture_id")]
        public long fixtureId { get; set; }

        [JsonProperty("sideline_id")]
        public long SidelineId { get; set; }

        [JsonProperty("participant_id")]
        public long ParticipantId { get; set; }

        [JsonProperty("participant")] // if add to Query Params "include=team"
        public Team Participant { get; set; }

        [JsonProperty("fixture")] // if add to Query Params "include=fixture"
        public Fixture Fixture { get; set; }

        [JsonProperty("sideline")] // if add to Query Params "include=sideline"
        public Sideline Sideline { get; set; }
    }
}
