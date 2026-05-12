using System.Collections.Generic;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureSidelinedItemDto
    {
        public long? PlayerId { get; init; }
        public string? PlayerName { get; init; }
        public string? PlayerImagePath { get; init; }
        public string? PositionCode { get; init; }
        public string? Category { get; init; }
        public string? Reason { get; init; }
        public System.DateTimeOffset? EndDate { get; init; }
        public int? GamesMissed { get; init; }
    }

    public sealed class FixtureSidelinedDto
    {
        public IReadOnlyList<FixtureSidelinedItemDto> Home { get; init; } =
            new List<FixtureSidelinedItemDto>();
        public IReadOnlyList<FixtureSidelinedItemDto> Away { get; init; } =
            new List<FixtureSidelinedItemDto>();
    }
}
