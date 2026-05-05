using System.Collections.Generic;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureDetailDto
    {
        [JsonProperty("fixture")]
        public FixtureSummaryDto Fixture { get; init; } = new();

        [JsonProperty("participants")]
        public IReadOnlyList<FixtureParticipantDto> Participants { get; init; }
            = new List<FixtureParticipantDto>();

        [JsonProperty("scores")]
        public IReadOnlyList<FixtureScoreDto> Scores { get; init; }
            = new List<FixtureScoreDto>();
    }
}
