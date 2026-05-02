using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Commentary
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("minute")]
        public int Minute { get; set; }

        [JsonProperty("extra_minute")]
        public int ExtraMinute { get; set; }

        [JsonProperty("is_goal")]
        public bool IsGoal { get; set; }

        [JsonProperty("is_important")]
        public bool IsImportant { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }
    }
}
