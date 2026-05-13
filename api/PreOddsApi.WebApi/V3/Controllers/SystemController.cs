using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Controllers
{
    /// <summary>
    /// Out-of-band health surface for monitors. Cheap-to-call: four
    /// COUNT / MAX queries against indexed columns. Returns the same
    /// shape on every status; the aggregate `status` flag lets a
    /// monitor alert on a single field.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("read-heavy")]
    public sealed class SystemController : ApiControllerBase
    {
        private readonly string? _connectionString;

        public SystemController(IConfiguration configuration)
        {
            _connectionString =
                Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetHealthAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                return Ok(new SystemHealthDto
                {
                    Status = "unhealthy",
                    Timestamp = DateTimeOffset.UtcNow,
                    Issues = new[] { "No database connection configured." },
                });
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            var fixtures = await ReadFixturesAsync(connection, ct);
            var snapshot = await ReadSnapshotAsync(connection, ct);
            var jobs = await ReadJobsAsync(connection, ct);

            // Freshness rules (kept inline because they're one-liners
            // and the per-check thresholds change rarely). Anything that
            // fails goes into the `issues` list and downgrades the
            // aggregate `status`.
            var issues = new List<string>();
            var today = DateTime.UtcNow.Date;

            if (fixtures.TodayCount == 0)
                issues.Add("Today has zero fixtures in the database.");
            else if (fixtures.TodayWithOdds * 2 < fixtures.TodayCount)
                issues.Add(
                    $"Only {fixtures.TodayWithOdds}/{fixtures.TodayCount} today fixtures carry odds.");

            if (snapshot.AsOfDate == null)
                issues.Add("analytics.odd_analysis_snapshots is empty.");
            else if (snapshot.AsOfDate < today.AddDays(-1))
                issues.Add(
                    $"Snapshot as_of_date {snapshot.AsOfDate:yyyy-MM-dd} is more than a day stale.");

            // SLA per scheduler key — anything missing 24h+ flips a flag.
            // Lets us spot "transfers hasn't fired in 3 days" without
            // grepping container logs.
            var dailySlaJobs = new HashSet<string>
            {
                "worker.football.reference",
                "worker.football.standings",
                "worker.football.fixture.backlog",
                "worker.football.analytics",
            };
            foreach (var job in jobs)
            {
                if (!dailySlaJobs.Contains(job.JobKey))
                    continue;
                if (job.AgeSeconds == null || job.AgeSeconds > 26 * 3600)
                    issues.Add(
                        $"Job {job.JobKey} hasn't completed successfully in 26h+.");
            }

            var status = issues.Count == 0
                ? "healthy"
                : issues.Count >= 3 ? "unhealthy" : "degraded";

            var dto = new SystemHealthDto
            {
                Status = status,
                Timestamp = DateTimeOffset.UtcNow,
                Fixtures = fixtures,
                Snapshot = snapshot,
                Jobs = jobs,
                Issues = issues,
            };

            // Always 200 on the response itself — monitors prefer to read
            // the `status` body field. A non-2xx would be reserved for
            // "the API itself is down", which is a different signal.
            return OkResponse(dto);
        }

        private static async Task<SystemHealthFixturesDto> ReadFixturesAsync(
            NpgsqlConnection connection, CancellationToken ct)
        {
            const string sql = """
                with today as (
                    select count(*) filter (where 1=1) as total,
                           count(*) filter (
                               where exists (
                                   select 1
                                   from odds.prematch_odds_current poc
                                   where poc.fixture_id = f.id
                               )
                           ) as with_odds
                    from football.fixtures f
                    where f.starting_at >= date_trunc('day', now())
                      and f.starting_at <  date_trunc('day', now()) + interval '1 day'
                ),
                wk as (
                    select count(*) filter (where 1=1) as total,
                           count(*) filter (
                               where exists (
                                   select 1
                                   from odds.prematch_odds_current poc
                                   where poc.fixture_id = f.id
                               )
                           ) as with_odds
                    from football.fixtures f
                    where f.starting_at >= date_trunc('day', now())
                      and f.starting_at <  date_trunc('day', now()) + interval '7 days'
                )
                select today.total, today.with_odds, wk.total, wk.with_odds
                from today, wk;
                """;
            await using var cmd = new NpgsqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return new SystemHealthFixturesDto();
            return new SystemHealthFixturesDto
            {
                TodayCount = (int)reader.GetInt64(0),
                TodayWithOdds = (int)reader.GetInt64(1),
                Next7DaysCount = (int)reader.GetInt64(2),
                Next7DaysWithOdds = (int)reader.GetInt64(3),
            };
        }

        private static async Task<SystemHealthSnapshotDto> ReadSnapshotAsync(
            NpgsqlConnection connection, CancellationToken ct)
        {
            const string sql = """
                select max(as_of_date), count(*)
                from analytics.odd_analysis_snapshots;
                """;
            await using var cmd = new NpgsqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return new SystemHealthSnapshotDto();
            DateTime? asOf = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
            var total = reader.IsDBNull(1) ? 0L : reader.GetInt64(1);
            return new SystemHealthSnapshotDto { AsOfDate = asOf, TotalRows = total };
        }

        private static async Task<IReadOnlyList<SystemHealthJobDto>> ReadJobsAsync(
            NpgsqlConnection connection, CancellationToken ct)
        {
            // The partial index ix_job_runs_key_completed makes this a
            // bitmap-index scan; cheap even with months of audit history.
            const string sql = """
                select job_key, max(completed_at) as last_at
                from sync.job_runs
                where status = 'success'
                group by job_key
                order by job_key;
                """;
            await using var cmd = new NpgsqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var items = new List<SystemHealthJobDto>();
            var now = DateTimeOffset.UtcNow;
            while (await reader.ReadAsync(ct))
            {
                DateTimeOffset? lastAt = reader.IsDBNull(1)
                    ? null
                    : new DateTimeOffset(
                        DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc));
                int? ageSeconds = lastAt == null
                    ? null
                    : (int)Math.Max(0, (now - lastAt.Value).TotalSeconds);
                items.Add(new SystemHealthJobDto
                {
                    JobKey = reader.GetString(0),
                    LastSuccessAt = lastAt,
                    AgeSeconds = ageSeconds,
                });
            }
            return items;
        }
    }
}
