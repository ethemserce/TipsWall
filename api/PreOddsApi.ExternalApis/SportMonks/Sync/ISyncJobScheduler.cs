namespace PreOddsApi.ExternalApis.SportMonks.Sync
{
    public interface ISyncJobScheduler
    {
        bool ShouldRun(string scheduleKey, int intervalSeconds);
        void RecordRun(string scheduleKey);
    }
}
