using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PreOddsApi.WebApi.V3.Auth;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresRefreshTokenService : IRefreshTokenService
    {
        public static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(30);

        private readonly NpgsqlDataSource _dataSource;

        public PostgresRefreshTokenService(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<IssuedRefreshToken> IssueAsync(
            Guid userId,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default)
        {
            await using var connection = await OpenAsync(ct);
            var (token, _) = await InsertTokenAsync(connection, userId, userAgent, ipAddress, ct);
            return token;
        }

        public async Task<RefreshLookupResult> RotateAsync(
            string rawToken,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return RefreshLookupResult.Fail("missing_token");

            var hash = RefreshTokenGenerator.Hash(rawToken);

            await using var connection = await OpenAsync(ct);
            await using var transaction = await connection.BeginTransactionAsync(ct);

            Guid currentTokenId;
            Guid userId;

            await using (var lookup = new NpgsqlCommand(
                """
                select id, user_id, expires_at, revoked_at
                from app.refresh_tokens
                where token_hash = @hash
                for update;
                """, connection, transaction))
            {
                lookup.Parameters.Add(new NpgsqlParameter("hash", hash));
                await using var reader = await lookup.ExecuteReaderAsync(ct);
                if (!await reader.ReadAsync(ct))
                    return RefreshLookupResult.Fail("not_found");

                if (!reader.IsDBNull(reader.GetOrdinal("revoked_at")))
                    return RefreshLookupResult.Fail("revoked");

                var expiresAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("expires_at"));
                if (expiresAt <= DateTimeOffset.UtcNow)
                    return RefreshLookupResult.Fail("expired");

                currentTokenId = reader.GetGuid(reader.GetOrdinal("id"));
                userId = reader.GetGuid(reader.GetOrdinal("user_id"));
            }

            var (newToken, newTokenId) = await InsertTokenAsync(
                connection, userId, userAgent, ipAddress, ct, transaction);

            await using (var revoke = new NpgsqlCommand(
                """
                update app.refresh_tokens
                set revoked_at = now(),
                    revoked_reason = 'rotated',
                    rotated_to_id = @new_id
                where id = @current_id;
                """, connection, transaction))
            {
                revoke.Parameters.Add(new NpgsqlParameter("current_id", currentTokenId));
                revoke.Parameters.Add(new NpgsqlParameter("new_id", newTokenId));
                await revoke.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);

            return RefreshLookupResult.Ok(userId, newToken);
        }

        public async Task<bool> RevokeAsync(
            string rawToken,
            string reason,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return false;

            var hash = RefreshTokenGenerator.Hash(rawToken);

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(
                """
                update app.refresh_tokens
                set revoked_at = now(),
                    revoked_reason = @reason
                where token_hash = @hash and revoked_at is null;
                """, connection);

            command.Parameters.Add(new NpgsqlParameter("hash", hash));
            command.Parameters.Add(new NpgsqlParameter("reason", reason));

            var rows = await command.ExecuteNonQueryAsync(ct);
            return rows > 0;
        }

        private static async Task<(IssuedRefreshToken Token, Guid Id)> InsertTokenAsync(
            NpgsqlConnection connection,
            Guid userId,
            string? userAgent,
            string? ipAddress,
            CancellationToken ct,
            NpgsqlTransaction? transaction = null)
        {
            var raw = RefreshTokenGenerator.CreateRaw();
            var hash = RefreshTokenGenerator.Hash(raw);
            var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);

            await using var command = transaction == null
                ? new NpgsqlCommand(InsertSql, connection)
                : new NpgsqlCommand(InsertSql, connection, transaction);

            command.Parameters.Add(new NpgsqlParameter("user_id", userId));
            command.Parameters.Add(new NpgsqlParameter("token_hash", hash));
            command.Parameters.Add(new NpgsqlParameter("expires_at", expiresAt));
            command.Parameters.Add(new NpgsqlParameter("user_agent",
                (object?)userAgent ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("ip_address",
                (object?)ipAddress ?? DBNull.Value));

            var id = (Guid)(await command.ExecuteScalarAsync(ct))!;

            return (new IssuedRefreshToken { RawToken = raw, ExpiresAt = expiresAt }, id);
        }

        private const string InsertSql = """
            insert into app.refresh_tokens (user_id, token_hash, expires_at, user_agent, ip_address)
            values (@user_id, @token_hash, @expires_at, @user_agent, @ip_address::inet)
            returning id;
            """;

        private Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
            => _dataSource.OpenConnectionAsync(ct).AsTask();
    }
}
