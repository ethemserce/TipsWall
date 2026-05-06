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
