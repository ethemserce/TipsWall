using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football
{
    public class TvStation : SportMonksBaseEntity
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("image_path")]
        public string Image_path { get; set; }

        [JsonProperty("countries")]
        public List<Country> Countries { get; set; }

        [JsonProperty("fixtures")]
        public List<Fixture> Fixtures { get; set; }

    }
}
