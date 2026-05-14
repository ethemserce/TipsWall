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
    }
}
