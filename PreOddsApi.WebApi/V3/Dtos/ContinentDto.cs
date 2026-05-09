
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class ContinentDto
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Code { get; init; }
    }
}
