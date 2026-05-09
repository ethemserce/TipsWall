using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Formation : SportMonksBaseEntity
    {
        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("participant_id")]
        public long ParticipantId { get; set; }

        [JsonProperty("formation")]
        public string TeamFormation { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }
    }
}
