using System;

namespace PreOddsApi.ExternalApis.SportMonks.Sync
{
    public interface ISyncJobScheduler
    {
        bool ShouldRun(string scheduleKey, int intervalSeconds);

        /// <summary>
        /// Records a job execution. Backwards-compatible default writes
        /// a success row with now() as both started + completed (legacy
        /// shape that didn't carry runtime / row counts). Callers that
        /// can capture richer metadata should use the overload below so
        /// the admin dashboard's history grid shows real durations and
        /// failure rows.
        /// </summary>
        void RecordRun(string scheduleKey);

        /// <summary>
        /// Records a job execution with full audit detail. completedAt
        /// defaults to now(). status='success' | 'failure'. itemsCount
        /// and errorMessage are optional — pass null when not meaningful.
        /// </summary>
        void RecordRun(
            string scheduleKey,
            DateTimeOffset startedAt,
            string status = "success",
            int? itemsCount = null,
            string? errorMessage = null);

        /// <summary>
        /// Last successful run timestamp for <paramref name="scheduleKey"/>,
        /// or null when the key was never recorded. Lets callers do
        /// calendar-day-based scheduling (vs. interval-based) without
        /// poking at the underlying storage.
        /// </summary>
        DateTimeOffset? GetLastRunUtc(string scheduleKey);
    }
}
