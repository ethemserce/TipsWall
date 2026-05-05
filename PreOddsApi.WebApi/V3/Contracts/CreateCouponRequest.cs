using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class CreateCouponRequest
    {
        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("visibility")]
        public string Visibility { get; set; } = "public";

        [JsonProperty("starts_at")]
        public DateTimeOffset? StartsAt { get; set; }

        [JsonProperty("ends_at")]
        public DateTimeOffset? EndsAt { get; set; }

        [JsonProperty("items")]
        public List<CreateCouponItemRequest> Items { get; set; } = new();
    }
}
