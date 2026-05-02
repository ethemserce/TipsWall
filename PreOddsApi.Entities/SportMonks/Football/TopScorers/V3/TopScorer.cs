using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.Common.V3;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.Standings.V3;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class TopScorer
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("stage_id")]
        public long StageId { get; set; }

        [JsonProperty("player_id")]
        public long PlayerId { get; set; }

        [JsonProperty("type_id")]
        public long TypeId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("participant_type")]
        public string ParticipantType { get; set; }

        [JsonProperty("participant_id")]
        public long ParticipantId { get; set; }

        [JsonProperty("season")] // if add to Query Params "include=season"
        public Season Season { get; set; }

        [JsonProperty("stage")] // if add to Query Params "include=stage"
        public Stage Stage { get; set; }

        [JsonProperty("player")] // if add to Query Params "include=player"
        public Player Player { get; set; }

        [JsonProperty("participant")] // if add to Query Params "include=participant"
        public Participant Participant { get; set; }

        [JsonProperty("type")] // if add to Query Params "include=type"
        public Types Type { get; set; }
    }
}
