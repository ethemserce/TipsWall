using System.Collections.Generic;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class CouponDetailDto
    {
        [JsonProperty("coupon")]
        public CouponSummaryDto Coupon { get; init; } = new();

        [JsonProperty("items")]
        public IReadOnlyList<CouponItemDto> Items { get; init; } = new List<CouponItemDto>();
    }
}
