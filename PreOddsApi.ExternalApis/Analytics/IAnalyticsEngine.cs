namespace PreOddsApi.ExternalApis.Analytics
{
    public interface IAnalyticsEngine
    {
        Task<int> RunSeasonStatsAsync(CancellationToken cancellationToken = default);

        Task<int> RunSeasonTeamStatsAsync(CancellationToken cancellationToken = default);

        Task<int> RunOddAnalysisSnapshotsAsync(CancellationToken cancellationToken = default);

        Task<int> RunFixtureSignalsAsync(CancellationToken cancellationToken = default);

        Task<int> RunRateResultsAsync(CancellationToken cancellationToken = default);
    }
}
