using System.Collections.Generic;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureTrendPointDto
    {
        public int? Minute { get; init; }
        public string? Side { get; init; }
        public decimal? Value { get; init; }
    }

    public sealed class FixtureTrendDto
    {
        public long TypeId { get; init; }
        public string? TypeCode { get; init; }
        public string? TypeName { get; init; }
        public IReadOnlyList<FixtureTrendPointDto> Points { get; init; } =
            new List<FixtureTrendPointDto>();
    }
}
