namespace PreOddsApi.ExternalApis.Analytics
{
    public interface IAnalyticsEngine
    {
        Task<int> RunSeasonStatsAsync(CancellationToken cancellationToken = default);

        Task<int> RunSeasonTeamStatsAsync(CancellationToken cancellationToken = default);
    }
}
