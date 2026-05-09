using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Aggregate : SportMonksBaseEntity
    {

        [JsonProperty("league_id")]
        public long LeagueId { get; set; }

        [JsonProperty("season_id")]
        public long SeasonId { get; set; }

        [JsonProperty("stage_id")]
        public long StageId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("fixture_ids")]
        public long[] fixtureIds { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }

        [JsonProperty("winner_participant_id")]
        public long WinnerParticipantId { get; set; }

        [JsonProperty("winner")]
        public Team Winner { get; set; }
    }
}
