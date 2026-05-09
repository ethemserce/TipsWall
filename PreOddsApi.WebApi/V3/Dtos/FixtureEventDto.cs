
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureEventDto
    {
        public long Id { get; init; }

        public int? Minute { get; init; }

        public int? ExtraMinute { get; init; }

        public long? TypeId { get; init; }

        public string? TypeCode { get; init; }

        public string? TypeName { get; init; }

        public long? ParticipantId { get; init; }

        public string? ParticipantLocation { get; init; }

        public long? PlayerId { get; init; }

        public string? PlayerName { get; init; }

        public string? RelatedPlayerName { get; init; }

        public string? Result { get; init; }

        public string? Info { get; init; }

        public bool? Injured { get; init; }
    }
}
