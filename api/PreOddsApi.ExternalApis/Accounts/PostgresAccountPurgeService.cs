using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace PreOddsApi.ExternalApis.Accounts
{
    public sealed class PostgresAccountPurgeService : IAccountPurgeService
    {
        private readonly NpgsqlDataSource _dataSource;
        private readonly ILogger<PostgresAccountPurgeService> _logger;

        public PostgresAccountPurgeService(
            NpgsqlDataSource dataSource,
            ILogger<PostgresAccountPurgeService> logger)
        {
            _dataSource = dataSource;
            _logger = logger;
        }

        public async Task<int> PurgeStaleAccountsAsync(
            int olderThanDays,
            CancellationToken cancellationToken = default)
        {
            // Two-step inside a transaction:
            //  1) delete from app.users (cascades to refresh_tokens,
            //     user_preferences, devices, etc. — every FK to app.users
            //     uses ON DELETE CASCADE or SET NULL).
            //  2) stamp purged_at on the audit rows so the next nightly
            //     tick doesn't re-process the same ids.
            //
            // The interval guard is on the audit table — soft-deletes
            // start the clock. Already-purged rows are skipped by the
            // purged_at filter so the SELECT is small even after months.
            const string sql = """
                with stale as (
                    select user_id
                    from app.account_deletions
                    where purged_at is null
                      and deleted_at < now() - (@days || ' days')::interval
                ),
                deleted as (
                    delete from app.users
                    where id in (select user_id from stale)
                    returning id
                )
                update app.account_deletions
                set purged_at = now()
                where user_id in (select id from deleted);
                """;

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("days", olderThanDays));

            var rows = await command.ExecuteNonQueryAsync(cancellationToken);
            if (rows > 0)
            {
                _logger.LogInformation(
                    "Account purge removed {Count} soft-deleted accounts older than {Days} days.",
                    rows, olderThanDays);
            }
            return rows;
        }
    }
}
