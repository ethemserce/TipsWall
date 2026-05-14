using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.ExternalApis.Analytics;
using PreOddsApi.WebApi.Helpers;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class AdminController : ApiControllerBase
    {
        private const int DefaultRecentRequestsLimit = 50;
        private const int MaxRecentRequestsLimit = 500;

        private readonly ISyncDiagnostics _diagnostics;
        private readonly IAnalyticsEngine _analytics;

        public AdminController(
            ISyncDiagnostics diagnostics,
            IAnalyticsEngine analytics)
        {
            _diagnostics = diagnostics;
            _analytics = analytics;
        }

        [HttpGet("sync-status")]
        public async Task<IActionResult> GetSyncStatusAsync(CancellationToken ct)
        {
            var items = await _diagnostics.GetSyncStatusAsync(ct);
            return OkResponse(items);
        }

        [HttpGet("recent-requests")]
        public async Task<IActionResult> GetRecentRequestsAsync(
            [FromQuery(Name = "limit")] int? limit,
            CancellationToken ct)
        {
            var clamped = System.Math.Clamp(
                limit ?? DefaultRecentRequestsLimit,
                1,
                MaxRecentRequestsLimit);

            var items = await _diagnostics.GetRecentRequestsAsync(clamped, ct);
            return OkResponse(items);
        }

        /// <summary>
        /// Daily snapshot rebuild — meant to be poked by a cron job (VPS
        /// host or an external monitor) every night around 02:00 UTC.
        /// Replaces today's analytics.odd_analysis_snapshots rows with a
        /// fresh aggregate against odds.prematch_odds_current where the
        /// fixture has finished (state_id in (5, 7, 8)). Wall-clock
        /// trigger plugs the gap that the worker's 24h-interval scheduler
        /// leaves: with interval-based scheduling the snapshot drifts
        /// later every day and disappears for hours after midnight.
        /// </summary>
        [HttpPost("analytics/snapshot/rebuild")]
        public async Task<IActionResult> RebuildSnapshotAsync(
            [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey,
            CancellationToken ct)
        {
            if (!string.Equals(apiKey, ApiKeyHandler.GetApiKey(), StringComparison.Ordinal))
                return Unauthorized(new { success = false, error = "bad-api-key" });

            // Order matches the worker's nightly chain: season stats →
            // team stats → player stats → odd snapshots. Each writes
            // idempotently for current_date so re-running mid-day just
            // refreshes.
            await _analytics.RunSeasonStatsAsync(ct);
            await _analytics.RunSeasonTeamStatsAsync(ct);
            await _analytics.RunSeasonPlayerStatsAsync(ct);
            // Wide lookback so backfill picks up anything that ran the
            // outcome-finalizer scheduler tier missed (e.g. worker outage).
            var finalized = await _analytics.RunOddOutcomeFinalizerAsync(
                lookbackHours: 24 * 30, cancellationToken: ct);
            var rows = await _analytics.RunOddAnalysisSnapshotsAsync(ct);

            return OkResponse(new
            {
                rebuilt_at = DateTimeOffset.UtcNow,
                snapshot_rows = rows,
                outcomes_finalized = finalized
            });
        }
    }
}
