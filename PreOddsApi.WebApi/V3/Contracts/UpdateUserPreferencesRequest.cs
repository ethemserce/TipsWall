using System.Collections.Generic;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class UpdateUserPreferencesRequest
    {
        [JsonProperty("odds_format")]
        public string? OddsFormat { get; set; }

        [JsonProperty("locale")]
        public string? Locale { get; set; }

        [JsonProperty("timezone")]
        public string? Timezone { get; set; }

        [JsonProperty("favorite_market_ids")]
        public IReadOnlyList<long>? FavoriteMarketIds { get; set; }
    }
}
