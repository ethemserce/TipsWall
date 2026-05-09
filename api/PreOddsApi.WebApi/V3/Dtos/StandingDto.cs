
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class StandingDto
    {
        public long Id { get; init; }

        public long? ParticipantId { get; init; }

        public long? LeagueId { get; init; }

        public long? SeasonId { get; init; }

        public long? StageId { get; init; }

        public long? GroupId { get; init; }

        public long? RoundId { get; init; }

        public int? Position { get; init; }

        public string? Result { get; init; }

        public int? Points { get; init; }
    }
}
