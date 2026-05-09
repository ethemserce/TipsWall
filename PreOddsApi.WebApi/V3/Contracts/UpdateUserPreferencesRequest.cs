using System.Collections.Generic;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class UpdateUserPreferencesRequest
    {
        public string? OddsFormat { get; set; }

        public string? Locale { get; set; }

        public string? Timezone { get; set; }

        public IReadOnlyList<long>? FavoriteMarketIds { get; set; }
    }
}
