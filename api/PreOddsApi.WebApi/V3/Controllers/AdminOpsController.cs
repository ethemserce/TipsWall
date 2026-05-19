using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PreOddsApi.ExternalApis.Analytics;
using PreOddsApi.WebApi.V3.Admin;

namespace PreOddsApi.WebApi.V3.Controllers
{
    /// <summary>
    /// Operational dashboard endpoints — worker tier status + Postgres
    /// health. Read-only, gated on the AdminOnly policy (admin JWT
    /// claim). Powers the web/ admin dashboard's auto-refreshing
    /// /ops page; safe to poll every 10s.
    /// </summary>
    [Authorize(Policy = "AdminOnly")]
    [Route("api/v3/admin/ops")]
    public sealed class AdminOpsController : ApiControllerBase
    {
        private readonly IAdminOpsReader _reader;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AdminOpsController> _logger;

        public AdminOpsController(
            IAdminOpsReader reader,
            IServiceScopeFactory scopeFactory,
            ILogger<AdminOpsController> logger)
        {
            _reader = reader;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Last-run snapshot for every worker job key seen in the last
        /// 24 hours. Drives the "live / pulse / nightly tier health"
        /// widget.
        /// </summary>
        [HttpGet("workers")]
        public async Task<IActionResult> GetWorkersAsync(CancellationToken ct)
        {
            var items = await _reader.GetWorkerTierStatusAsync(ct);
            return OkResponse(items);
        }

        /// <summary>
        /// Postgres health snapshot — active query count, longest
        /// runtime, recovery state, DB size. Drives the "DB pulse"
        /// widget that catches lockups before they freeze the mobile
        /// live tile.
        /// </summary>
        [HttpGet("postgres")]
        public async Task<IActionResult> GetPostgresAsync(CancellationToken ct)
        {
            var snapshot = await _reader.GetPostgresHealthAsync(ct);
            return OkResponse(snapshot);
        }

        /// <summary>
        /// Nightly snapshot run history — one row per recorded run for
        /// the last `days` calendar days (default 10, max 30). Used by
        /// the ops dashboard to surface missed nights at a glance.
        /// </summary>
        [HttpGet("nightly-snapshot/history")]
        public async Task<IActionResult> GetNightlySnapshotHistoryAsync(
            [FromQuery] int days = 10,
            CancellationToken ct = default)
        {
            var clamped = days <= 0 ? 10 : (days > 30 ? 30 : days);
            var items = await _reader.GetNightlySnapshotHistoryAsync(clamped, ct);
            return OkResponse(items);
        }

        /// <summary>
        /// SportMonks rate-limit + volume snapshot — pulls the most
        /// recent rate-limit headers stored in sync.api_requests plus
        /// a 60-min volume / failure aggregate. Catches a runaway
        /// sync before it burns the daily quota.
        /// </summary>
        [HttpGet("sportmonks/quota")]
        public async Task<IActionResult> GetSportMonksQuotaAsync(CancellationToken ct)
        {
            var quota = await _reader.GetSportMonksQuotaAsync(ct);
            return OkResponse(quota);
        }

        /// <summary>
        /// Recent SportMonks API failures — 4xx / 5xx responses or
        /// exception-text rows from sync.api_requests, last N hours
        /// (default 24, max 168 = 1 week).
        /// </summary>
        [HttpGet("sportmonks/errors")]
        public async Task<IActionResult> GetSportMonksRecentErrorsAsync(
            [FromQuery] int hours = 24,
            CancellationToken ct = default)
        {
            var clamped = hours <= 0 ? 24 : (hours > 168 ? 168 : hours);
            var items = await _reader.GetSportMonksRecentErrorsAsync(clamped, ct);
            return OkResponse(items);
        }

        /// <summary>
        /// Terminates a postgres backend by pid. Use to clear a runaway
        /// query that's pinning CPU — the equivalent of running
        /// `select pg_terminate_backend(pid)` in psql. Requires the pid
        /// be currently active (we double-check before issuing terminate)
        /// to avoid hitting an idle connection by accident.
        ///
        /// AdminOnly policy gates the endpoint. The web dashboard
        /// surfaces a confirm step before posting, but operators with
        /// curl + bearer token can also call it from the shell.
        /// </summary>
        [HttpPost("postgres/kill-query")]
        public async Task<IActionResult> KillPostgresQueryAsync(
            [FromQuery] int pid,
            CancellationToken ct)
        {
            if (pid <= 0)
                return BadRequest(new { success = false, error = new { message = "pid is required and must be positive." } });

            // Look up the target in pg_stat_activity first so we can:
            //  1. Confirm the pid is still active (not idle, not
            //     finished — terminating a no-op is harmless but the
            //     audit log entry then misleads).
            //  2. Log what we're killing for the audit trail.
            var connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? HttpContext.RequestServices
                    .GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()
                    .GetConnectionString("PreOddsApiPostgresDb");
            if (string.IsNullOrWhiteSpace(connectionString))
                return Problem("Postgres connection string not configured.");

            await using var conn = new Npgsql.NpgsqlConnection(connectionString);
            await conn.OpenAsync(ct);

            string? targetQuery = null;
            double targetDuration = 0;
            await using (var probe = new Npgsql.NpgsqlCommand(
                "select left(query, 200) as query, extract(epoch from now() - query_start)::float as duration_seconds " +
                "from pg_stat_activity where pid = @pid and state = 'active'",
                conn))
            {
                probe.Parameters.AddWithValue("pid", pid);
                await using var r = await probe.ExecuteReaderAsync(ct);
                if (await r.ReadAsync(ct))
                {
                    targetQuery = r.IsDBNull(0) ? null : r.GetString(0);
                    targetDuration = r.IsDBNull(1) ? 0 : r.GetDouble(1);
                }
            }

            if (targetQuery == null)
                return NotFound(new { success = false, error = new { message = $"pid {pid} is not currently active." } });

            await using (var kill = new Npgsql.NpgsqlCommand(
                "select pg_terminate_backend(@pid)",
                conn))
            {
                kill.Parameters.AddWithValue("pid", pid);
                await kill.ExecuteScalarAsync(ct);
            }

            _logger.LogWarning(
                "Admin-triggered pg_terminate_backend on pid {Pid} (running {Duration:F0}s). Query: {Query}",
                pid, targetDuration, targetQuery);
            return Ok(new
            {
                success = true,
                terminated = true,
                pid,
                duration_seconds = targetDuration,
                query = targetQuery,
            });
        }

        /// <summary>
        /// Kicks off the full analytics rebuild chain (season stats,
        /// outcome finalizer with wide lookback, snapshot regenerate)
        /// as a fire-and-forget background task and returns 202 Accepted
        /// immediately. Real execution takes 5-10 min — the dashboard
        /// polls /admin/ops/postgres to watch the long-running query
        /// surface in SlowQueries and disappear when it finishes.
        ///
        /// Why fire-and-forget instead of awaiting:
        ///   * WebAPI now caps every postgres query at 60s. Awaiting a
        ///     10-min job here would either trip that ceiling or hold
        ///     a request handler for ten minutes.
        ///   * The analytics engine opens its own NpgsqlConnection (not
        ///     from the WebAPI's data source pool), so the 60s cap
        ///     doesn't kill the underlying batch.
        ///   * The endpoint is admin-gated so a stray double-tap by an
        ///     impatient admin doesn't snowball into a queue.
        /// </summary>
        [HttpPost("analytics/rebuild")]
        public IActionResult TriggerAnalyticsRebuild()
        {
            // Spin up a detached scope so the engine + its scoped
            // dependencies (NpgsqlDataSource is singleton, but logger
            // factory + config are scoped through DI) outlive the
            // controller request lifecycle. Without the explicit scope
            // the ASP.NET request-scoped services would be disposed the
            // instant we returned 202 and the rebuild would crash on
            // its first DI resolution.
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<IAnalyticsEngine>();
                try
                {
                    _logger.LogInformation("Admin-triggered analytics rebuild starting at {NowUtc}.", DateTimeOffset.UtcNow);
                    await engine.RunSeasonStatsAsync();
                    await engine.RunSeasonTeamStatsAsync();
                    await engine.RunSeasonPlayerStatsAsync();
                    var finalized = await engine.RunOddOutcomeFinalizerAsync(lookbackHours: 24 * 365);
                    var rows = await engine.RunOddAnalysisSnapshotsAsync();
                    _logger.LogInformation(
                        "Admin-triggered analytics rebuild success: outcomes_finalized={Finalized} snapshot_rows={Rows}",
                        finalized, rows);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Admin-triggered analytics rebuild failed. Re-run from the dashboard or wait for the next 03:00 UTC window.");
                }
            });
            return Accepted(new
            {
                success = true,
                accepted_at = DateTimeOffset.UtcNow,
                note = "Rebuild started in the background; watch /admin/ops/postgres for progress.",
            });
        }
    }
}
