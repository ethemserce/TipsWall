using System;

namespace PreOddsApi.ExternalApis.SportMonks.Sync
{
    public interface ISyncJobScheduler
    {
        bool ShouldRun(string scheduleKey, int intervalSeconds);
        void RecordRun(string scheduleKey);

        /// <summary>
        /// Last successful run timestamp for <paramref name="scheduleKey"/>,
        /// or null when the key was never recorded. Lets callers do
        /// calendar-day-based scheduling (vs. interval-based) without
        /// poking at the underlying storage.
        /// </summary>
        DateTimeOffset? GetLastRunUtc(string scheduleKey);
    }
}
