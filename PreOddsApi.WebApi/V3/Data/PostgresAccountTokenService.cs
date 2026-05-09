using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresAccountTokenService : IAccountTokenService
    {
        private readonly NpgsqlDataSource _dataSource;

        public PostgresAccountTokenService(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<IssuedAccountToken> IssueAsync(
            Guid userId,
            AccountTokenPurpose purpose,
            TimeSpan lifetime,
            CancellationToken ct = default)
        {
            // 32 random bytes → URL-safe base64; long enough that brute
            // force is not a viable attack inside the lifetime window.
            var raw = GenerateRawToken();
            var hash = HashToken(raw);
            var expiresAt = DateTime.UtcNow.Add(lifetime);

            const string sql = @"
                insert into app.account_tokens (user_id, purpose, token_hash, expires_at)
                values (@user_id, @purpose, @hash, @expires_at);";

            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("user_id", userId);
            command.Parameters.AddWithValue("purpose", PurposeToWire(purpose));
            command.Parameters.AddWithValue("hash", hash);
            command.Parameters.AddWithValue("expires_at", expiresAt);
            await command.ExecuteNonQueryAsync(ct);

            return new IssuedAccountToken { RawToken = raw, ExpiresAt = expiresAt };
        }

        public async Task<AccountTokenRedemption> ConsumeAsync(
            string rawToken,
            AccountTokenPurpose purpose,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return new AccountTokenRedemption { Succeeded = false, FailureReason = "missing_token" };

            var hash = HashToken(rawToken);

            // Atomic select+update so two concurrent redemptions can't
            // both succeed: the UPDATE only matches when consumed_at is
            // still null, and we treat a zero-row return as "already
            // consumed or unknown".
            const string sql = @"
                update app.account_tokens
                set consumed_at = now()
                where token_hash = @hash
                  and purpose    = @purpose
                  and consumed_at is null
                  and expires_at > now()
                returning user_id;";

            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("hash", hash);
            command.Parameters.AddWithValue("purpose", PurposeToWire(purpose));

            var result = await command.ExecuteScalarAsync(ct);
            if (result is Guid userId)
                return new AccountTokenRedemption { Succeeded = true, UserId = userId };

            return new AccountTokenRedemption { Succeeded = false, FailureReason = "invalid_or_expired" };
        }

        private static string GenerateRawToken()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private static string HashToken(string raw)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();

        private static string PurposeToWire(AccountTokenPurpose p) => p switch
        {
            AccountTokenPurpose.PasswordReset => "password_reset",
            AccountTokenPurpose.EmailVerify => "email_verify",
            _ => throw new ArgumentOutOfRangeException(nameof(p))
        };
    }
}
