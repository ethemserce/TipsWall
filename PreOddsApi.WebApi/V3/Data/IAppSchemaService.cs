using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface IAppSchemaService
    {
        Task<(IReadOnlyList<FeaturedFixtureDto> Items, int Total)> GetFeaturedFixturesAsync(
            DateTime? featureDate,
            int page,
            int perPage,
            CancellationToken ct = default);

        Task<(IReadOnlyList<TipDto> Items, int Total)> GetPublicTipsAsync(
            string? resultStatus,
            long? fixtureId,
            int page,
            int perPage,
            CancellationToken ct = default);

        Task<(IReadOnlyList<CouponSummaryDto> Items, int Total)> GetPublicCouponsAsync(
            string? status,
            int page,
            int perPage,
            CancellationToken ct = default);

        Task<CouponDetailDto?> GetCouponByPublicCodeAsync(
            string publicCode,
            CancellationToken ct = default);

        Task<Guid> SubmitContactMessageAsync(
            string name,
            string email,
            string? subject,
            string message,
            string? locale,
            string? ipAddress,
            string? userAgent,
            CancellationToken ct = default);
    }
}
