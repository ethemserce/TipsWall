using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Core.Common.V3
{
    public class Plan
    {
        [JsonProperty("plan")]
        public string PlanPlan { get; set; }

        [JsonProperty("sport")]
        public string Sport { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }
    }
}
