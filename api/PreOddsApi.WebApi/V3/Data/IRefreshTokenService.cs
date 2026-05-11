using System;
using System.Threading;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface IRefreshTokenService
    {
        Task<IssuedRefreshToken> IssueAsync(
            Guid userId,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default);

        Task<RefreshLookupResult> RotateAsync(
            string rawToken,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default);

        Task<bool> RevokeAsync(
            string rawToken,
            string reason,
            CancellationToken ct = default);

        /// <summary>
        /// Revokes every refresh token for the user — used by account
        /// deletion and "log out of all devices" flows. Returns the
        /// number of rows actually revoked (excludes already-revoked).
        /// </summary>
        Task<int> RevokeAllForUserAsync(
            Guid userId,
            string reason,
            CancellationToken ct = default);
    }

    public sealed class IssuedRefreshToken
    {
        public string RawToken { get; init; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; init; }
    }

    public sealed class RefreshLookupResult
    {
        public bool Succeeded { get; init; }
        public Guid UserId { get; init; }
        public IssuedRefreshToken? NewToken { get; init; }
        public string? FailureReason { get; init; }

        public static RefreshLookupResult Fail(string reason) =>
            new() { Succeeded = false, FailureReason = reason };

        public static RefreshLookupResult Ok(Guid userId, IssuedRefreshToken token) =>
            new() { Succeeded = true, UserId = userId, NewToken = token };
    }
}
