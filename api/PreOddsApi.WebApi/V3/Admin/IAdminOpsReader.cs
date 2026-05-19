using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.V3.Admin
{
    /// <summary>
    /// Read-side queries that power the admin ops dashboard. Kept narrow
    /// + DTO-shaped so the dashboard can stay one tight roundtrip per
    /// auto-refresh cycle (default 10s on the web side).
    /// </summary>
    public interface IAdminOpsReader
    {
        Task<IReadOnlyList<WorkerTierStatusDto>> GetWorkerTierStatusAsync(CancellationToken ct);
        Task<PostgresHealthDto> GetPostgresHealthAsync(CancellationToken ct);
        Task<IReadOnlyList<NightlySnapshotRunDto>> GetNightlySnapshotHistoryAsync(int days, CancellationToken ct);
    }

    public sealed class NightlySnapshotRunDto
    {
        /// <summary>UTC day the run started (date portion of started_at). </summary>
        public DateOnly Date { get; init; }
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset? CompletedAt { get; init; }
        /// <summary>Seconds between started_at and completed_at; null while running. </summary>
        public double? DurationSeconds { get; init; }
        /// <summary>'success' | 'failure'. The current scheduler always writes 'success' even on retries-exhausted-failure path; we still expose the column so a future writer that distinguishes can light up the grid without an API change. </summary>
        public string Status { get; init; } = string.Empty;
        /// <summary>Optional row count or units processed — null when the scheduler didn't pass one. </summary>
        public int? ItemsCount { get; init; }
        public string? ErrorMessage { get; init; }
    }

    public sealed class WorkerTierStatusDto
    {
        /// <summary>Schedule key — e.g. "sportmonks.football.livescores.latest". </summary>
        public string JobKey { get; init; } = string.Empty;
        /// <summary>Last successful run time, UTC. </summary>
        public DateTimeOffset? LastRunAt { get; init; }
        /// <summary>Last failure time, UTC; null when the most recent run succeeded. </summary>
        public DateTimeOffset? LastFailureAt { get; init; }
        /// <summary>Last error message (truncated to 200 chars) if the most recent run failed. </summary>
        public string? LastError { get; init; }
        /// <summary>Total run count over the lifetime of the worker. </summary>
        public long RunCount { get; init; }
    }

    public sealed class PostgresHealthDto
    {
        /// <summary>Active backends excluding the current diagnostic connection. </summary>
        public int ActiveQueries { get; init; }
        /// <summary>Total backends including idle. </summary>
        public int TotalConnections { get; init; }
        /// <summary>Longest active query duration, seconds. Null when nothing is active. </summary>
        public double? LongestQuerySeconds { get; init; }
        /// <summary>First 120 chars of the longest active query. </summary>
        public string? LongestQueryText { get; init; }
        /// <summary>pg_is_in_recovery() — true if the DB is replaying WAL. </summary>
        public bool InRecovery { get; init; }
        /// <summary>Database size in bytes. </summary>
        public long DatabaseBytes { get; init; }
        /// <summary>Free disk bytes on the postgres data mount (host root in our Docker setup). </summary>
        public long DiskFreeBytes { get; init; }
        /// <summary>Total disk bytes on the postgres data mount. </summary>
        public long DiskTotalBytes { get; init; }
        /// <summary>Percent of disk currently used (0-100). </summary>
        public double DiskUsedPercent { get; init; }
        /// <summary>Active queries running longer than the slow-query threshold (default 60s). </summary>
        public IReadOnlyList<SlowQueryDto> SlowQueries { get; init; } = Array.Empty<SlowQueryDto>();
    }

    public sealed class SlowQueryDto
    {
        /// <summary>Postgres backend pid for kill operations. </summary>
        public int Pid { get; init; }
        /// <summary>Seconds the query has been running. </summary>
        public double DurationSeconds { get; init; }
        /// <summary>First 200 chars of the query text. </summary>
        public string Query { get; init; } = string.Empty;
        /// <summary>e.g. 'active' / 'idle in transaction' — only 'active' is in the result set by default. </summary>
        public string State { get; init; } = string.Empty;
    }
}
