using System;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface IUserIdentityService
    {
        Task<UserDto?> AuthenticateAsync(
            string usernameOrEmail,
            string password,
            CancellationToken ct = default);

        Task<SignupOutcome> SignupAsync(
            string username,
            string email,
            string password,
            string? displayName,
            CancellationToken ct = default);

        Task<UserDto?> GetByIdAsync(
            Guid userId,
            CancellationToken ct = default);

        /// <summary>Returns the user id matching either username or email. Null when not found.</summary>
        Task<Guid?> FindUserIdByEmailOrUsernameAsync(
            string emailOrUsername,
            CancellationToken ct = default);

        /// <summary>
        /// Replaces the password without requiring the old one. Caller must
        /// have proven identity another way (consumed account token).
        /// </summary>
        Task<bool> ResetPasswordAsync(
            Guid userId,
            string newPassword,
            CancellationToken ct = default);

        /// <summary>Stamps the user record as email-verified.</summary>
        Task<bool> MarkEmailVerifiedAsync(
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Apple- and Google-mandated in-app account deletion. Soft-deletes
        /// the row (status='deleted'), scrubs PII (email, username,
        /// display_name, password_hash) so re-signup with the same email
        /// is possible later, and records an audit row in
        /// app.account_deletions. Refresh-token revocation happens
        /// separately at the controller layer.
        /// </summary>
        Task<bool> SoftDeleteAccountAsync(
            Guid userId,
            string? reason,
            CancellationToken ct = default);
    }

    public sealed class SignupOutcome
    {
        public UserDto? User { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        public bool Succeeded => User != null && ErrorCode == null;

        public static SignupOutcome Ok(UserDto user) => new() { User = user };

        public static SignupOutcome Fail(string code, string message) =>
            new() { ErrorCode = code, ErrorMessage = message };

        public static class ErrorCodes
        {
            public const string UsernameTaken = "USERNAME_TAKEN";
            public const string EmailTaken = "EMAIL_TAKEN";
            public const string Validation = "VALIDATION";
        }
    }
}
