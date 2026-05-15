using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.WebApi.V3.Auth;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresUserIdentityService : IUserIdentityService
    {
        private static readonly Regex UsernamePattern =
            new("^[a-zA-Z0-9_-]{3,50}$", RegexOptions.Compiled);
        private static readonly Regex EmailPattern =
            new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled);

        private readonly NpgsqlDataSource _dataSource;
        private readonly IPrdUserService _legacyUserService;
        private readonly ILogger<PostgresUserIdentityService> _logger;

        public PostgresUserIdentityService(
            NpgsqlDataSource dataSource,
            IPrdUserService legacyUserService,
            ILogger<PostgresUserIdentityService> logger)
        {
            _dataSource = dataSource;
            _legacyUserService = legacyUserService;
            _logger = logger;
        }

        public async Task<UserDto?> AuthenticateAsync(
            string usernameOrEmail,
            string password,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
                return null;

            var key = usernameOrEmail.Trim();

            var (record, hash) = await TryLoadAppUserAsync(key, ct);
            if (record != null)
            {
                if (!string.IsNullOrWhiteSpace(hash) && PasswordHasher.Verify(password, hash))
                    return record;

                if (string.IsNullOrWhiteSpace(hash) || hash.StartsWith(PasswordHasher.LegacyBridgeAlgorithmId))
                {
                    var legacy = _legacyUserService.GetUser(key, password);
                    if (legacy != null)
                        return record;
                }

                return null;
            }

            var legacyUser = _legacyUserService.GetUser(key, password);
            if (legacyUser == null)
                return null;

            var bridged = await BridgeLegacyUserAsync(legacyUser.NickName ?? key, legacyUser.Email, ct);
            return bridged;
        }

        public async Task<SignupOutcome> SignupAsync(
            string username,
            string email,
            string password,
            string? displayName,
            CancellationToken ct = default)
        {
            username = username?.Trim() ?? string.Empty;
            email = email?.Trim() ?? string.Empty;

            if (!UsernamePattern.IsMatch(username))
                return SignupOutcome.Fail(SignupOutcome.ErrorCodes.Validation,
                    "Username must be 3-50 chars, letters/digits/dash/underscore only.");

            if (!EmailPattern.IsMatch(email))
                return SignupOutcome.Fail(SignupOutcome.ErrorCodes.Validation,
                    "Email is not in a valid format.");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return SignupOutcome.Fail(SignupOutcome.ErrorCodes.Validation,
                    "Password must be at least 8 characters.");

            await using var connection = await OpenAsync(ct);

            await using (var check = new NpgsqlCommand(
                """
                select
                    exists(select 1 from app.users where lower(username) = lower(@username)) as username_taken,
                    exists(select 1 from app.users where lower(email) = lower(@email)) as email_taken;
                """, connection))
            {
                check.Parameters.Add(new NpgsqlParameter("username", username));
                check.Parameters.Add(new NpgsqlParameter("email", email));
                await using var reader = await check.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    if (reader.GetBoolean(0))
                        return SignupOutcome.Fail(SignupOutcome.ErrorCodes.UsernameTaken,
                            "Username is already in use.");
                    if (reader.GetBoolean(1))
                        return SignupOutcome.Fail(SignupOutcome.ErrorCodes.EmailTaken,
                            "Email is already in use.");
                }
            }

            var hash = PasswordHasher.Hash(password);
            var trimmedDisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();

            await using var insert = new NpgsqlCommand(
                """
                insert into app.users (username, email, display_name, password_hash, password_algorithm, role, status)
                values (@username, @email, @display_name, @password_hash, @algorithm, 'user', 'active')
                returning id;
                """, connection);

            insert.Parameters.Add(new NpgsqlParameter("username", username));
            insert.Parameters.Add(new NpgsqlParameter("email", email));
            insert.Parameters.Add(new NpgsqlParameter("display_name",
                (object?)trimmedDisplayName ?? DBNull.Value));
            insert.Parameters.Add(new NpgsqlParameter("password_hash", hash));
            insert.Parameters.Add(new NpgsqlParameter("algorithm", PasswordHasher.AlgorithmId));

            var id = (Guid)(await insert.ExecuteScalarAsync(ct))!;

            return SignupOutcome.Ok(new UserDto
            {
                Id = id,
                Username = username,
                Email = email,
                DisplayName = trimmedDisplayName,
                Role = "user",
                Tier = "free"
            });
        }

        private async Task<(UserDto? User, string? PasswordHash)> TryLoadAppUserAsync(
            string usernameOrEmail,
            CancellationToken ct)
        {
            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(
                """
                select id, username, email, display_name, role,
                       tier, tier_expires_at, password_hash,
                       (email_verified_at is not null) as email_verified
                from app.users
                where status = 'active'
                  and (lower(username) = lower(@key) or lower(email) = lower(@key))
                limit 1;
                """, connection);
            command.Parameters.Add(new NpgsqlParameter("key", usernameOrEmail));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return (null, null);

            var user = new UserDto
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                Username = ReadNullableString(reader, "username"),
                Email = ReadNullableString(reader, "email"),
                DisplayName = ReadNullableString(reader, "display_name"),
                Role = reader.GetString(reader.GetOrdinal("role")),
                Tier = reader.GetString(reader.GetOrdinal("tier")),
                TierExpiresAt = ReadNullableDateTimeOffset(reader, "tier_expires_at"),
                EmailVerified = reader.GetBoolean(reader.GetOrdinal("email_verified"))
            };
            var hash = ReadNullableString(reader, "password_hash");
            return (user, hash);
        }

        public async Task<UserDto?> GetByIdAsync(
            Guid userId,
            CancellationToken ct = default)
        {
            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(
                """
                select id, username, email, display_name, role, tier, tier_expires_at,
                       (email_verified_at is not null) as email_verified
                from app.users
                where id = @id and status = 'active'
                limit 1;
                """, connection);
            command.Parameters.Add(new NpgsqlParameter("id", userId));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            return new UserDto
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                Username = ReadNullableString(reader, "username"),
                Email = ReadNullableString(reader, "email"),
                DisplayName = ReadNullableString(reader, "display_name"),
                Role = reader.GetString(reader.GetOrdinal("role")),
                Tier = reader.GetString(reader.GetOrdinal("tier")),
                TierExpiresAt = ReadNullableDateTimeOffset(reader, "tier_expires_at"),
                EmailVerified = reader.GetBoolean(reader.GetOrdinal("email_verified"))
            };
        }

        private async Task<UserDto> BridgeLegacyUserAsync(
            string username,
            string? email,
            CancellationToken ct)
        {
            var (existing, _) = await TryLoadAppUserAsync(username, ct);
            if (existing != null)
                return existing;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(
                """
                insert into app.users (username, email, password_algorithm, role, status)
                values (@username, @email, @algorithm, 'user', 'active')
                returning id, username, email, display_name, role, tier, tier_expires_at;
                """, connection);

            command.Parameters.Add(new NpgsqlParameter("username", username));
            command.Parameters.Add(new NpgsqlParameter("email",
                (object?)(string.IsNullOrWhiteSpace(email) ? null : email.Trim()) ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("algorithm",
                PasswordHasher.LegacyBridgeAlgorithmId));

            try
            {
                await using var reader = await command.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    return new UserDto
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("id")),
                        Username = ReadNullableString(reader, "username"),
                        Email = ReadNullableString(reader, "email"),
                        DisplayName = ReadNullableString(reader, "display_name"),
                        Role = reader.GetString(reader.GetOrdinal("role")),
                        Tier = reader.GetString(reader.GetOrdinal("tier")),
                        TierExpiresAt = ReadNullableDateTimeOffset(reader, "tier_expires_at")
                    };
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                _logger.LogInformation(
                    "Concurrent bridge insert raced; loading existing app.users row for {Username}.",
                    username);
            }

            var (raceWinner, _) = await TryLoadAppUserAsync(username, ct);
            return raceWinner ?? throw new InvalidOperationException(
                "Failed to bridge legacy user to app.users.");
        }

        public async Task<Guid?> FindUserIdByEmailOrUsernameAsync(
            string emailOrUsername,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(emailOrUsername)) return null;
            var key = emailOrUsername.Trim().ToLowerInvariant();

            // app.users has functional unique indexes on lower(email) /
            // lower(username); match those expressions exactly so the
            // planner picks them up.
            const string sql = @"
                select id
                from app.users
                where lower(email) = @k or lower(username) = @k
                limit 1;";

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("k", key);
            var result = await command.ExecuteScalarAsync(ct);
            return result is Guid id ? id : null;
        }

        public async Task<bool> ResetPasswordAsync(
            Guid userId,
            string newPassword,
            CancellationToken ct = default)
        {
            // Caller has already proven identity (consumed an account token).
            // No old-password check; just rehash and store.
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
                return false;

            var hash = PasswordHasher.Hash(newPassword);

            const string sql = @"
                update app.users
                set password_hash = @hash,
                    updated_at = now()
                where id = @id;";

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", userId);
            command.Parameters.AddWithValue("hash", hash);
            var rows = await command.ExecuteNonQueryAsync(ct);
            return rows > 0;
        }

        public async Task<bool> MarkEmailVerifiedAsync(
            Guid userId,
            CancellationToken ct = default)
        {
            const string sql = @"
                update app.users
                set email_verified_at = coalesce(email_verified_at, now()),
                    updated_at = now()
                where id = @id;";

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", userId);
            var rows = await command.ExecuteNonQueryAsync(ct);
            return rows > 0;
        }

        public async Task<bool> SoftDeleteAccountAsync(
            Guid userId,
            string? reason,
            CancellationToken ct = default)
        {
            // Wrap in a transaction so the audit row and the user mutation
            // commit together. If either fails we roll back so we never end
            // up with a deletion audit pointing at a still-active account.
            await using var connection = await OpenAsync(ct);
            await using var tx = await connection.BeginTransactionAsync(ct);

            const string scrubSql = @"
                update app.users
                set status = 'deleted',
                    -- Scrub identifying fields so future signups can reuse
                    -- the email. The DB row stays for foreign-key parents
                    -- (refresh tokens, coupons, etc.) until the nightly
                    -- purge job hard-deletes after 30 days.
                    email = null,
                    username = null,
                    display_name = null,
                    first_name = null,
                    last_name = null,
                    password_hash = null,
                    password_algorithm = null,
                    updated_at = now()
                where id = @id and status <> 'deleted';";

            await using (var scrub = new NpgsqlCommand(scrubSql, connection, tx))
            {
                scrub.Parameters.AddWithValue("id", userId);
                var rows = await scrub.ExecuteNonQueryAsync(ct);
                if (rows == 0)
                {
                    await tx.RollbackAsync(ct);
                    return false;
                }
            }

            const string auditSql = @"
                insert into app.account_deletions (user_id, reason)
                values (@id, @reason)
                on conflict (user_id) do update set
                    deleted_at = excluded.deleted_at,
                    reason = excluded.reason;";

            await using (var audit = new NpgsqlCommand(auditSql, connection, tx))
            {
                audit.Parameters.AddWithValue("id", userId);
                audit.Parameters.AddWithValue("reason",
                    (object?)(string.IsNullOrWhiteSpace(reason) ? null : reason.Trim())
                        ?? DBNull.Value);
                await audit.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
            return true;
        }

        private Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
            => _dataSource.OpenConnectionAsync(ct).AsTask();

        private static string? ReadNullableString(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        private static DateTimeOffset? ReadNullableDateTimeOffset(
            NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetFieldValue<DateTimeOffset>(i);
        }
    }
}
