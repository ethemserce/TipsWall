namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureMatchFactDto
    {
        public long Id { get; init; }
        public long? TypeId { get; init; }
        public string? TypeName { get; init; }
        public string? Category { get; init; }
        public string? Scope { get; init; }
        public string? Participant { get; init; }
        public string? NaturalLanguage { get; init; }
    }
}
