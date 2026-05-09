using System.Collections.Generic;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class CouponDetailDto
    {
        public CouponSummaryDto Coupon { get; init; } = new();

        public IReadOnlyList<CouponItemDto> Items { get; init; } = new List<CouponItemDto>();
    }
}
