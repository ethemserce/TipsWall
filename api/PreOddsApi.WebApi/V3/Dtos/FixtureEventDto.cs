
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

        /// <summary>
        /// True when the event was inferred to have been overturned by VAR
        /// (same-player VAR event within ~5 minutes after this goal AND the
        /// team's GOAL count exceeds the final score). Mobile renders these
        /// with a strikethrough + "İPTAL" badge so the timeline stays
        /// honest — the user saw the goal live, removing it would be
        /// confusing.
        /// </summary>
        public bool Cancelled { get; init; }
    }
}
