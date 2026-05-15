using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresUserMarketPreferencesReader : IUserMarketPreferencesReader
    {
        private readonly NpgsqlDataSource _ds;

        public PostgresUserMarketPreferencesReader(NpgsqlDataSource ds)
        {
            _ds = ds;
        }

        public async Task<IReadOnlyList<long>> GetAsync(Guid userId, CancellationToken ct = default)
        {
            const string sql = """
                select market_id
                from app.user_market_preferences
                where user_id = @user_id
                order by market_id;
                """;
            await using var cn = await _ds.OpenConnectionAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = userId });
            var items = new List<long>();
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                items.Add(r.GetInt64(0));
            }
            return items;
        }

        public async Task<IReadOnlyList<long>> ReplaceAsync(
            Guid userId, IReadOnlyList<long> marketIds, CancellationToken ct = default)
        {
            // One round-trip: wipe existing rows then insert the new
            // selection. INSERT ... SELECT FROM odds.markets enforces that
            // the caller can't smuggle in unknown market ids (no FK
            // violation surfaced to the user — just silently dropped).
            var deduped = marketIds.Distinct().ToArray();
            const string sql = """
                begin;
                delete from app.user_market_preferences where user_id = @user_id;
                insert into app.user_market_preferences (user_id, market_id)
                select @user_id, m.id
                from odds.markets m
                where m.id = any(@ids);
                commit;

                select market_id
                from app.user_market_preferences
                where user_id = @user_id
                order by market_id;
                """;
            await using var cn = await _ds.OpenConnectionAsync(ct);
            await using var cmd = new NpgsqlCommand(sql, cn);
            cmd.Parameters.Add(new NpgsqlParameter("user_id", NpgsqlDbType.Uuid) { Value = userId });
            cmd.Parameters.Add(new NpgsqlParameter("ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = deduped });
            var items = new List<long>();
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                items.Add(r.GetInt64(0));
            }
            return items;
        }
    }
}
