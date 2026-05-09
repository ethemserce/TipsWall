
using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Football.V3;
using System.Collections.Generic;

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

        [JsonProperty("fixture")]
        public Fixture Fixture { get; set; }

        [JsonProperty("league")]
        public League League { get; set; }

        [JsonProperty("lines")]
        public List<NewsItemLine> Lines { get; set; }
    }
}
