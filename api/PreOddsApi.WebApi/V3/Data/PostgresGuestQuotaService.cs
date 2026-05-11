using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresGuestQuotaService : IGuestQuotaService
    {
        // Daily picks allowed for a single guest device. Free / premium
        // users bypass this path entirely — they have their own
        // unlimited-tier logic at the controller layer.
        public const int DailyLimit = 2;

        private readonly NpgsqlDataSource _dataSource;

        public PostgresGuestQuotaService(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<GuestQuotaStatus> GetStatusAsync(
            string deviceId,
            CancellationToken ct = default)
        {
            const string sql = """
                select coalesce(picks_today, 0) as picks_today
                from app.guest_pick_quotas
                where device_id = @device_id and quota_date = current_date;
                """;

            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("device_id", deviceId));

            var result = await command.ExecuteScalarAsync(ct);
            var picksToday = result is int i ? i : 0;
            return new GuestQuotaStatus
            {
                Limit = DailyLimit,
                PicksToday = picksToday
            };
        }

        public async Task<GuestQuotaClaim> TryClaimAsync(
            string deviceId,
            CancellationToken ct = default)
        {
            // Single-statement upsert: insert a row at 1 if it doesn't
            // exist; otherwise increment. The WHERE on the DO UPDATE
            // gates by limit so PG only commits the bump when there's
            // slack — losing the race returns 0 rows and we treat that
            // as "denied". Composite primary key (device_id, quota_date)
            // makes the conflict target trivial.
            const string sql = """
                insert into app.guest_pick_quotas (device_id, quota_date, picks_today)
                values (@device_id, current_date, 1)
                on conflict (device_id, quota_date) do update
                    set picks_today = app.guest_pick_quotas.picks_today + 1
                    where app.guest_pick_quotas.picks_today < @limit
                returning picks_today;
                """;

            await using var connection = await _dataSource.OpenConnectionAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("device_id", deviceId));
            command.Parameters.Add(new NpgsqlParameter("limit", DailyLimit));

            var result = await command.ExecuteScalarAsync(ct);
            if (result is int newCount)
            {
                return new GuestQuotaClaim
                {
                    Granted = true,
                    Limit = DailyLimit,
                    PicksToday = newCount
                };
            }

            // No row returned — the row exists and is already at the
            // limit. Read it back so the caller can render an accurate
            // "0 remaining" badge.
            var status = await GetStatusAsync(deviceId, ct);
            return new GuestQuotaClaim
            {
                Granted = false,
                Limit = status.Limit,
                PicksToday = status.PicksToday
            };
        }
    }
}
