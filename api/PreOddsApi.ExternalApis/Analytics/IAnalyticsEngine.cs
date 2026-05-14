namespace PreOddsApi.ExternalApis.Analytics
{
    public interface IAnalyticsEngine
    {
        Task<int> RunSeasonStatsAsync(CancellationToken cancellationToken = default);

        Task<int> RunSeasonTeamStatsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// analytics.season_player_stats refresh for current_date.
        /// Aggregates football.fixture_lineups × football.fixture_events
        /// × football.fixtures into one row per
        /// (league, season, team, player). Idempotent — re-running
        /// overwrites today's slice.
        /// </summary>
        Task<int> RunSeasonPlayerStatsAsync(CancellationToken cancellationToken = default);

        Task<int> RunOddAnalysisSnapshotsAsync(CancellationToken cancellationToken = default);

        Task<int> RunFixtureSignalsAsync(CancellationToken cancellationToken = default);
    }
}
