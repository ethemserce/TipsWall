using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface IUserMarketPreferencesReader
    {
        Task<IReadOnlyList<long>> GetAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Replace the user's preferred-market list with the given ids
        /// (deduplicated, intersected with existing markets so foreign
        /// keys never fail). Returns the persisted set.
        /// </summary>
        Task<IReadOnlyList<long>> ReplaceAsync(
            Guid userId, IReadOnlyList<long> marketIds, CancellationToken ct = default);
    }
}
