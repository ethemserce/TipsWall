using Newtonsoft.Json;
using System;

namespace PreOddsApi.Entities.SportMonks.Core.Common.V3
{
    public class Pagination
    {
        [JsonProperty("count")]
        public long Count { get; set; }

        [JsonProperty("per_page")]
        public long PerPage { get; set; }

        [JsonProperty("current_page")]
        public long CurrentPage { get; set; }

        [JsonProperty("next_page")]
        public Uri NextPage { get; set; }

        [JsonProperty("has_more")]
        public bool HasMore { get; set; }
    }
}
