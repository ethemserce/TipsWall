using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Contracts;
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

        Task<TipOutcome> CreateTipAsync(
            Guid userId,
            CreateTipRequest request,
            CancellationToken ct = default);

        Task<bool> DeleteTipAsync(
            Guid userId,
            Guid tipId,
            CancellationToken ct = default);

        Task<CouponOutcome> CreateCouponAsync(
            Guid userId,
            CreateCouponRequest request,
            CancellationToken ct = default);

        Task<bool> DeleteCouponAsync(
            Guid userId,
            Guid couponId,
            CancellationToken ct = default);
    }

    public sealed class TipOutcome
    {
        public TipDto? Tip { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        public bool Succeeded => Tip != null && ErrorCode == null;

        public static TipOutcome Ok(TipDto tip) => new() { Tip = tip };
        public static TipOutcome Fail(string code, string message) =>
            new() { ErrorCode = code, ErrorMessage = message };
    }

    public sealed class CouponOutcome
    {
        public CouponDetailDto? Coupon { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        public bool Succeeded => Coupon != null && ErrorCode == null;

        public static CouponOutcome Ok(CouponDetailDto coupon) => new() { Coupon = coupon };
        public static CouponOutcome Fail(string code, string message) =>
            new() { ErrorCode = code, ErrorMessage = message };
    }
}
