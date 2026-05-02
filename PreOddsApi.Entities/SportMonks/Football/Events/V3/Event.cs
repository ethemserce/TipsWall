using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Event : SportMonksBaseEntity
    {

        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("period_id")]
        public long? PeriodId { get; set; }
        [JsonProperty("section")]
        public string Section { get; set; }

        [JsonProperty("participant_id")]
        public long? ParticipantId { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

        [JsonProperty("player_id")]
        public long? PlayerId { get; set; }

        [JsonProperty("related_player_id")]
        public long? RelatedPlayerId { get; set; }

        [JsonProperty("player_name")]
        public string PlayerName { get; set; }
        [JsonProperty("related_player_name")]
        public string RelatedPlayerName { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("info")]
        public string Info { get; set; }

        [JsonProperty("addition")]
        public string Addition { get; set; }

        [JsonProperty("minute")]
        public int? Minute { get; set; }

        [JsonProperty("extra_minute")]
        public int? ExtraMinute { get; set; }

        [JsonProperty("injured")]
        public bool? Injured { get; set; }
        [JsonProperty("on_bench")]
        public bool? OnBench { get; set; }
        [JsonProperty("coach_id")]
        public long? coachId { get; set; }
        [JsonProperty("sub_type_id")]
        public long? subTypeId { get; set; }
    }
}
