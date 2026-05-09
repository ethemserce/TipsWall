using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.Statistics.V3
{
    public class StatisticData
    {
        [JsonProperty("value")]
        public object Value { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraData { get; set; }
    }
}
