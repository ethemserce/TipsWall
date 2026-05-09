
namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class CreateFavoriteRequest
    {
        public string FavoriteType { get; set; } = string.Empty;

        public long? TeamId { get; set; }

        public long? LeagueId { get; set; }

        public long? FixtureId { get; set; }

        public string? Notes { get; set; }

        public int? SortOrder { get; set; }
    }
}
