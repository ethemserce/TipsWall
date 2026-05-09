
namespace PreOddsApi.WebApi.V3.Dtos
{
    /// <summary>
    /// Pivoted standings row enriched with team info and overall stats —
    /// the shape the mobile fixture-detail standings tab consumes directly.
    /// </summary>
    public sealed class LeagueTableRowDto
    {
        public long? TeamId { get; init; }

        public string? TeamName { get; init; }

        public string? TeamImagePath { get; init; }

        public int? Position { get; init; }

        public int Played { get; init; }

        public int Wins { get; init; }

        public int Draws { get; init; }

        public int Losses { get; init; }

        public int GoalsFor { get; init; }

        public int GoalsAgainst { get; init; }

        public int GoalDifference { get; init; }

        public int Points { get; init; }
    }
}
