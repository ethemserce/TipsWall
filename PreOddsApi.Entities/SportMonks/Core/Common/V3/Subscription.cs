using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Core.Common.V3
{
    public class Subscription
    {
        [JsonProperty("meta")]
        public object[] Meta { get; set; }

        [JsonProperty("plans")]
        public Plan[] Plans { get; set; }

        [JsonProperty("add_ons")]
        public object[] AddOns { get; set; }

        [JsonProperty("widgets")]
        public object[] Widgets { get; set; }
    }
}
