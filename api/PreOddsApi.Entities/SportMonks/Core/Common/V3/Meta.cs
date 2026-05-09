using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Common.Enums;

namespace PreOddsApi.Entities.SportMonks.Core.Common.V3
{
    public class Meta
    {
        [JsonProperty("location")]
        public Location? Location { get; set; }

        [JsonProperty("winner")]
        public bool? Winner { get; set; }

        [JsonProperty("position")]
        public int? Position { get; set; }
    }
}
