using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.Common.V3;

namespace PreOddsApi.Entities.SportMonks
{
    public class SportMonksBase<T>
    {
        [JsonProperty("data")]
        public T Data { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("subscription")]
        public Subscription[] Subscription { get; set; }

        [JsonProperty("rate_limit")]
        public RateLimit RateLimit { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; }
    }
}
