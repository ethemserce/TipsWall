using System.Collections.Generic;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class UserPreferencesDto
    {
        public string OddsFormat { get; init; } = "decimal";

        public string? Locale { get; init; }

        public string? Timezone { get; init; }

        public IReadOnlyList<long> FavoriteMarketIds { get; init; } = new List<long>();
    }
}
