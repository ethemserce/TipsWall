using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Sidelined : SportMonksBaseEntity
    {
        [JsonProperty("fixture_id")]
        public long? FixtureId { get; set; }

        [JsonProperty("sideline_id")]
        public long? SidelineId { get; set; }

        [JsonProperty("participant_id")]
        public long? ParticipantId { get; set; }

        [JsonProperty("player_id")]
        public long? PlayerId { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

        [JsonProperty("participant")] // if add to Query Params "include=team"
        public Team Participant { get; set; }

        [JsonProperty("fixture")] // if add to Query Params "include=fixture"
        public Fixture Fixture { get; set; }

        [JsonProperty("sideline")] // if add to Query Params "include=sideline"
        public Sideline Sideline { get; set; }

        [JsonProperty("player")] // if add to Query Params "include=player"
        public Player Player { get; set; }

        [JsonProperty("type")] // if add to Query Params "include=type"
        public Types Type { get; set; }
    }
}
