using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class SeasonDto
    {
        public long Id { get; init; }

        public long LeagueId { get; init; }

        public string Name { get; init; } = string.Empty;

        public bool Finished { get; init; }

        public bool Pending { get; init; }

        public bool IsCurrent { get; init; }

        public DateTime? StartingAt { get; init; }

        public DateTime? EndingAt { get; init; }
    }
}
