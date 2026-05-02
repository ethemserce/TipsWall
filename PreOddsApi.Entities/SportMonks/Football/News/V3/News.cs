
using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football
{
    public class News : SportMonksBaseEntity
    {
        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }
        [JsonProperty("league_id")]
        public long? LeagueId { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}