using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PreOddsApi.Entities.SportMonks.Core.Common.V3
{
    public class Subscription
    {
        // SportMonks switched `meta` from an empty array on free plans to an
        // object on paid plans (e.g. {current_timestamp, next_billing_cycle}).
        // JToken accepts both shapes so the envelope deserializes regardless.
        [JsonProperty("meta")]
        public JToken Meta { get; set; }

        [JsonProperty("plans")]
        public Plan[] Plans { get; set; }

        [JsonProperty("add_ons")]
        public JToken AddOns { get; set; }

        [JsonProperty("widgets")]
        public JToken Widgets { get; set; }

        [JsonProperty("bundles")]
        public JToken Bundles { get; set; }
    }
}
