using System;
using System.Collections.Generic;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class CreateCouponRequest
    {
        public string? Title { get; set; }

        public string Visibility { get; set; } = "public";

        public DateTimeOffset? StartsAt { get; set; }

        public DateTimeOffset? EndsAt { get; set; }

        public List<CreateCouponItemRequest> Items { get; set; } = new();
    }
}
