
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureScoreDto
    {
        public long Id { get; init; }

        public long? TypeId { get; init; }

        public long? ParticipantId { get; init; }

        public string? ParticipantLocation { get; init; }

        public string? Description { get; init; }

        public int? Goals { get; init; }
    }
}
