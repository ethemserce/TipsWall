using System.Collections.Concurrent;

namespace PreOddsApi.ExternalApis.SportMonks.Sync
{
    public sealed class SyncJobScheduler : ISyncJobScheduler
    {
        private readonly ConcurrentDictionary<string, DateTimeOffset> _lastRun = new();

        public bool ShouldRun(string scheduleKey, int intervalSeconds)
        {
            if (!_lastRun.TryGetValue(scheduleKey, out var lastRun))
                return true;

            return DateTimeOffset.UtcNow - lastRun >= TimeSpan.FromSeconds(intervalSeconds);
        }

        public void RecordRun(string scheduleKey)
        {
            _lastRun[scheduleKey] = DateTimeOffset.UtcNow;
        }
    }
}
