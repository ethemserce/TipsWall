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

        /// <summary>
        /// Stamps odds.prematch_odds_current.winning for every outcome that
        /// belongs to a fixture in a final state (state_id ∈ {5, 7, 8}) whose
        /// rows still have winning = null — i.e. SportMonks didn't compute it
        /// because has_winning_calculations was false. Uses the
        /// odds.evaluate_outcome plpgsql function to grade by score.
        ///
        /// Idempotent: re-running over already-stamped fixtures is a no-op
        /// (the WHERE clause filters to NULL winning). Bounded to fixtures
        /// whose state was recorded in the last `lookbackHours` so we don't
        /// re-scan the full historical archive on every worker tick.
        /// </summary>
        Task<int> RunOddOutcomeFinalizerAsync(int lookbackHours = 36, CancellationToken cancellationToken = default);

        /// <summary>
        /// Nightly housekeeping: prune `sync.api_requests` rows older than
        /// <paramref name="apiRequestRetentionDays"/>, then VACUUM ANALYZE
        /// the heavy churn tables so dead row space is reclaimed and the
        /// planner's stats stay current. Returns the count of api_requests
        /// rows deleted; VACUUM is best-effort and logs failures rather
        /// than throwing.
        /// </summary>
        Task<int> RunMaintenanceCleanupAsync(
            int apiRequestRetentionDays = 60,
            CancellationToken cancellationToken = default);
    }
}
