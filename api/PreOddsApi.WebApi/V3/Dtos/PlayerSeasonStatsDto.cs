using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    /// <summary>
    /// Aggregated season totals for a single player. Mirrors
    /// analytics.season_player_stats — one row per (league × season ×
    /// team). The mobile UI shows the latest row by default and lets
    /// the user flip through past seasons.
    /// </summary>
    public sealed class PlayerSeasonStatsDto
    {
        public long LeagueId { get; init; }
        public string? LeagueName { get; init; }
        public long SeasonId { get; init; }
        public string? SeasonName { get; init; }
        public long TeamId { get; init; }
        public string? TeamName { get; init; }
        public string? TeamImagePath { get; init; }
        public DateTime AsOfDate { get; init; }
        public string FixtureScope { get; init; } = "all";

        public int? MatchesPlayed { get; init; }
        public int? MatchesStarted { get; init; }
        public int? MatchesSubbedIn { get; init; }
        public int? MatchesSubbedOut { get; init; }
        public int? MinutesPlayed { get; init; }
        public int? Goals { get; init; }
        public int? Assists { get; init; }
        public int? OwnGoals { get; init; }
        public int? PenaltiesScored { get; init; }
        public int? PenaltiesMissed { get; init; }
        public int? YellowCards { get; init; }
        public int? RedCards { get; init; }
    }
}
