using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface IOddsReader
    {
        Task<(IReadOnlyList<PrematchOddDto> Items, int Total)> GetPrematchOddsAsync(
            long fixtureId,
            long? bookmakerId,
            long? marketId,
            int page,
            int perPage,
            CancellationToken ct = default);

        Task<(IReadOnlyList<OddHistoryDto> Items, int Total)> GetPrematchHistoryAsync(
            long fixtureId,
            long? bookmakerId,
            long? marketId,
            string? outcomeKey,
            int page,
            int perPage,
            CancellationToken ct = default);

        Task<(IReadOnlyList<InplayOddDto> Items, int Total)> GetInplayOddsAsync(
            long fixtureId,
            long? bookmakerId,
            long? marketId,
            int page,
            int perPage,
            CancellationToken ct = default);

        Task<(IReadOnlyList<OddHistoryDto> Items, int Total)> GetInplayHistoryAsync(
            long fixtureId,
            long? bookmakerId,
            long? marketId,
            string? outcomeKey,
            int page,
            int perPage,
            CancellationToken ct = default);
    }
}
