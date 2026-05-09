using System;
using System.Threading;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.V3.Data
{
    /// <summary>
    /// Single-use lifecycle tokens (password reset, email verify). Mirrors
    /// the refresh-token contract: a raw token is returned exactly once at
    /// issue time and only its hash is persisted. Consume marks the row
    /// used so a stolen email link can't be replayed.
    /// </summary>
    public interface IAccountTokenService
    {
        /// <summary>Issue a fresh single-use token for the given user + purpose.</summary>
        Task<IssuedAccountToken> IssueAsync(
            Guid userId,
            AccountTokenPurpose purpose,
            TimeSpan lifetime,
            CancellationToken ct = default);

        /// <summary>
        /// Consume a raw token. Returns the user id on success; null when
        /// the token is unknown, expired, or already consumed (caller maps
        /// these to 400 / 410 / 410 as appropriate).
        /// </summary>
        Task<AccountTokenRedemption> ConsumeAsync(
            string rawToken,
            AccountTokenPurpose purpose,
            CancellationToken ct = default);
    }

    public enum AccountTokenPurpose
    {
        PasswordReset,
        EmailVerify
    }

    public sealed class IssuedAccountToken
    {
        public required string RawToken { get; init; }
        public required DateTime ExpiresAt { get; init; }
    }

    public sealed class AccountTokenRedemption
    {
        public bool Succeeded { get; init; }
        public Guid UserId { get; init; }
        public string? FailureReason { get; init; }
    }
}
