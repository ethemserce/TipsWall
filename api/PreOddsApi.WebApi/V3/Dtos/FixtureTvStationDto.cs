namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureTvStationDto
    {
        public long Id { get; init; }
        public string? Name { get; init; }
        public string? Url { get; init; }
        public string? ImagePath { get; init; }
    }
}
