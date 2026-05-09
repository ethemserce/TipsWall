using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Football.V3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.Entities.SportMonks.Football.Standings.V3
{
    public class StandingForm : SportMonksBaseEntity
    {
        [JsonProperty("standing_type")]
        public string StandingType { get; set; }

        [JsonProperty("standing_id")]
        public long StandingId { get; set; }

        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("form")]
        public string Form { get; set; }

        [JsonProperty("sort_order")]
        public int SortOrder { get; set; }

        [JsonProperty("fixture")] // if add to Query Params "include=fixture"
        public Fixture Fixture { get; set; }
    }
}
