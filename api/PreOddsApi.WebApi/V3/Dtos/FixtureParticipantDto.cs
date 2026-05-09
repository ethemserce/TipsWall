
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureParticipantDto
    {
        public long TeamId { get; init; }

        public string Location { get; init; } = string.Empty;

        public bool? Winner { get; init; }

        public int? Position { get; init; }
    }
}
