using System.Collections.Generic;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureLineupsDto
    {
        public FixtureTeamLineupDto? Home { get; init; }

        public FixtureTeamLineupDto? Away { get; init; }
    }

    public sealed class FixtureTeamLineupDto
    {
        public long? TeamId { get; init; }

        public string? Formation { get; init; }

        public IReadOnlyList<FixtureLineupPlayerDto> Starters { get; init; }
            = new List<FixtureLineupPlayerDto>();

        public IReadOnlyList<FixtureLineupPlayerDto> Bench { get; init; }
            = new List<FixtureLineupPlayerDto>();
    }

    public sealed class FixtureLineupPlayerDto
    {
        public long? PlayerId { get; init; }

        public string? PlayerName { get; init; }

        public int? JerseyNumber { get; init; }

        public string? FormationField { get; init; }

        public int? FormationPosition { get; init; }

        public string? PositionCode { get; init; }
    }
}
