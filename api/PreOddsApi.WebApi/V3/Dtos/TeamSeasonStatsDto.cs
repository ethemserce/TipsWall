using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    /// <summary>
    /// Aggregated season-level performance numbers for a single team in
    /// a single league × season scope. Backed by analytics.season_team_stats
    /// which is refreshed by the worker's nightly analytics tier.
    /// </summary>
    public sealed class TeamSeasonStatsDto
    {
        public long LeagueId { get; init; }
        public long SeasonId { get; init; }
        public long TeamId { get; init; }
        public DateTime AsOfDate { get; init; }
        public string FixtureScope { get; init; } = "all";

        public int? MatchesPlayed { get; init; }
        public int? MatchesWon { get; init; }
        public int? MatchesDrawn { get; init; }
        public int? MatchesLost { get; init; }
        public int? GoalsFor { get; init; }
        public int? GoalsAgainst { get; init; }
        public int? GoalDifference { get; init; }
        public int? CleanSheets { get; init; }
        public int? FailedToScore { get; init; }
        public int? BothTeamsScored { get; init; }
        public int? YellowCards { get; init; }
        public int? RedCards { get; init; }
        public decimal? AverageGoalsFor { get; init; }
        public decimal? AverageGoalsAgainst { get; init; }
        public int? Points { get; init; }
        // "WWDLW" — last 5 match outcomes oldest → newest, fed by SportMonks.
        public string? Form { get; init; }
    }
}
