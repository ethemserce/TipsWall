using System.Collections.Generic;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureDetailDto
    {
        public FixtureSummaryDto Fixture { get; init; } = new();

        public IReadOnlyList<FixtureParticipantDto> Participants { get; init; }
            = new List<FixtureParticipantDto>();

        public IReadOnlyList<FixtureScoreDto> Scores { get; init; }
            = new List<FixtureScoreDto>();
    }
}
