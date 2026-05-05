using System.Collections.Generic;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class UserPreferencesDto
    {
        [JsonProperty("odds_format")]
        public string OddsFormat { get; init; } = "decimal";

        [JsonProperty("locale")]
        public string? Locale { get; init; }

        [JsonProperty("timezone")]
        public string? Timezone { get; init; }

        [JsonProperty("favorite_market_ids")]
        public IReadOnlyList<long> FavoriteMarketIds { get; init; } = new List<long>();
    }
}
