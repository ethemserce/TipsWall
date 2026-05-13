using System;
using System.Collections.Generic;

namespace PreOddsApi.WebApi.V3.Dtos
{
    /// <summary>
    /// Snapshot of pipeline-health signals consumed by uptime monitors
    /// (UptimeRobot / Healthchecks.io / etc) and by the team during
    /// incident triage. Each field is a single number or timestamp so
    /// a dashboard cell can pick the metric directly without parsing.
    /// </summary>
    public sealed class SystemHealthDto
    {
        // "healthy" | "degraded" | "unhealthy". An aggregate flag so the
        // monitor only needs to alert on a single field instead of
        // parsing every check.
        public string Status { get; init; } = "unknown";
        public DateTimeOffset Timestamp { get; init; }

        public SystemHealthFixturesDto Fixtures { get; init; } = new();
        public SystemHealthSnapshotDto Snapshot { get; init; } = new();

        // One row per scheduler key — last successful run + seconds
        // since. Lets us see "transfers hasn't run in 3 days" without
        // diving into container logs.
        public IReadOnlyList<SystemHealthJobDto> Jobs { get; init; }
            = Array.Empty<SystemHealthJobDto>();

        // Anything the checks flagged. Empty list when healthy.
        public IReadOnlyList<string> Issues { get; init; }
            = Array.Empty<string>();
    }

    public sealed class SystemHealthFixturesDto
    {
        public int TodayCount { get; init; }
        public int TodayWithOdds { get; init; }
        public int Next7DaysCount { get; init; }
        public int Next7DaysWithOdds { get; init; }
    }

    public sealed class SystemHealthSnapshotDto
    {
        // odd_analysis_snapshots. as_of_date should equal today for the
        // app to surface fresh signals.
        public DateTime? AsOfDate { get; init; }
        public long TotalRows { get; init; }
    }

    public sealed class SystemHealthJobDto
    {
        public string JobKey { get; init; } = string.Empty;
        public DateTimeOffset? LastSuccessAt { get; init; }
        public int? AgeSeconds { get; init; }
    }
}
