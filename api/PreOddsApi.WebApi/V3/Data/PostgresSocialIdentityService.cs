using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresSocialIdentityService : ISocialIdentityService
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<PostgresSocialIdentityService> _logger;

        public PostgresSocialIdentityService(
            NpgsqlDataSource dataSource,
            ILogger<PostgresSocialIdentityService> logger)
        {
            _dataSource = dataSource;
            _logger = logger;
        }

        public async Task<UserDto> UpsertFromProviderAsync(
            string provider,
            string providerSubject,
            string? email,
            string? displayName,
            CancellationToken ct = default)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            await using var tx = await connection.BeginTransactionAsync(ct);

            // 1) Try the (provider, subject) → user lookup first. The
            //    unique index keeps this O(1). When found we touch
            //    last_login_at + refresh raw_profile fields, then return
            //    the (already-existing) user row.
            const string findSql = """
                select user_id from app.user_auth_identities
                where provider = @provider and provider_subject = @subject
                limit 1;
                """;

            Guid? existingUserId = null;
            await using (var find = new NpgsqlCommand(findSql, connection, tx))
            {
                find.Parameters.AddWithValue("provider", provider);
                find.Parameters.AddWithValue("subject", providerSubject);
                var raw = await find.ExecuteScalarAsync(ct);
                if (raw is Guid id) existingUserId = id;
            }

            Guid userId;
            if (existingUserId.HasValue)
            {
                userId = existingUserId.Value;
                await TouchIdentityAsync(connection, tx, userId, provider, providerSubject, email, ct);
            }
            else
            {
                userId = await CreateUserAndIdentityAsync(
                    connection, tx, provider, providerSubject, email, displayName, ct);
            }

            // 2) Re-read the user record so we return tier + role
            //    consistently — the social sign-in flow may have just
            //    inserted a brand-new user, or surfaced a long-existing
            //    premium one. Either way the caller gets the canonical
            //    shape.
            const string userSql = """
                select id, username, email, display_name, role, tier, tier_expires_at,
                       (email_verified_at is not null) as email_verified
                from app.users
                where id = @id;
                """;

            UserDto? user = null;
            await using (var read = new NpgsqlCommand(userSql, connection, tx))
            {
                read.Parameters.AddWithValue("id", userId);
                await using var reader = await read.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    user = new UserDto
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
            }

            await tx.CommitAsync(ct);
            if (user == null)
                throw new InvalidOperationException("User row vanished mid-transaction.");
            return user;
        }

        private static async Task TouchIdentityAsync(
            NpgsqlConnection connection, NpgsqlTransaction tx,
            Guid userId, string provider, string subject, string? email,
            CancellationToken ct)
        {
            const string sql = """
                update app.user_auth_identities
                set provider_email = coalesce(@email, provider_email),
                    last_login_at = now(),
                    updated_at = now()
                where provider = @provider and provider_subject = @subject;
                """;
            await using var cmd = new NpgsqlCommand(sql, connection, tx);
            cmd.Parameters.AddWithValue("provider", provider);
            cmd.Parameters.AddWithValue("subject", subject);
            cmd.Parameters.AddWithValue("email", (object?)email ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        private async Task<Guid> CreateUserAndIdentityAsync(
            NpgsqlConnection connection, NpgsqlTransaction tx,
            string provider, string subject, string? email, string? displayName,
            CancellationToken ct)
        {
            // The user row's email is best-effort — Apple often returns
            // a relay address that changes; Google returns the real one
            // verified=true. We never use email as the primary identifier
            // here, only the (provider, subject) tuple. Social accounts
            // skip the email-verify step: both Apple and Google certify
            // ownership before issuing an id_token, so we stamp the
            // verified-at column up front rather than spamming them with
            // a "click to verify" email they don't need.
            const string insertUser = """
                insert into app.users (username, email, display_name, role, status, password_algorithm,
                                       email_verified_at)
                values (@username, @email, @display, 'user', 'active', 'social', now())
                returning id;
                """;

            // Username has to be unique; we synthesize a stable one from
            // the provider + subject's prefix. The user can rename later
            // from the profile screen (Faz 8+ work).
            var username = SynthesizeUsername(provider, subject);

            Guid newUserId;
            await using (var ins = new NpgsqlCommand(insertUser, connection, tx))
            {
                ins.Parameters.AddWithValue("username", username);
                ins.Parameters.AddWithValue("email", (object?)email ?? DBNull.Value);
                ins.Parameters.AddWithValue("display", (object?)displayName ?? DBNull.Value);
                var raw = await ins.ExecuteScalarAsync(ct);
                newUserId = (Guid)raw!;
            }

            const string insertIdentity = """
                insert into app.user_auth_identities
                    (user_id, provider, provider_subject, provider_email, last_login_at)
                values (@user, @provider, @subject, @email, now());
                """;
            await using (var ins = new NpgsqlCommand(insertIdentity, connection, tx))
            {
                ins.Parameters.AddWithValue("user", newUserId);
                ins.Parameters.AddWithValue("provider", provider);
                ins.Parameters.AddWithValue("subject", subject);
                ins.Parameters.AddWithValue("email", (object?)email ?? DBNull.Value);
                await ins.ExecuteNonQueryAsync(ct);
            }

            _logger.LogInformation(
                "Created new user {UserId} via {Provider} sign-in (subject prefix {Prefix}).",
                newUserId, provider, subject.Length > 8 ? subject[..8] : subject);
            return newUserId;
        }

        private static string SynthesizeUsername(string provider, string subject)
        {
            // 'g_' / 'a_' prefix + 12 chars of the subject keeps the
            // synthesized name short, mostly stable, and visibly
            // social-sourced if it ever surfaces in the UI. The unique
            // index on lower(username) ensures collisions fail loud
            // (they'd retry via a different provider_subject anyway,
            // not a real risk in practice).
            var prefix = provider switch
            {
                "apple" => "a_",
                "google" => "g_",
                _ => "s_",
            };
            var tail = subject.Length > 12 ? subject[..12] : subject;
            // Strip anything that wouldn't pass the username regex on
            // the email/password signup path — keep parity.
            var clean = new string([.. tail.ToLowerInvariant()
                .Where(static ch => char.IsLetterOrDigit(ch) || ch == '_')]);
            return prefix + clean;
        }

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
