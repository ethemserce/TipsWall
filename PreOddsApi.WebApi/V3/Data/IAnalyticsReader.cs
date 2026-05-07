using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface IAnalyticsReader
    {
        Task<(IReadOnlyList<RateResultDto> Items, int Total)> GetHotRateAsync(
            long? bookmakerId, long? marketId, string? windowCode, int? matchState,
            int page, int perPage, CancellationToken ct = default);

        Task<(IReadOnlyList<RateResultDto> Items, int Total)> GetWinningRateAsync(
            long? bookmakerId, long? marketId, string? windowCode, int? matchState,
            int page, int perPage, CancellationToken ct = default);

        Task<(IReadOnlyList<RateResultDto> Items, int Total)> GetEarningRateAsync(
            long? bookmakerId, long? marketId, string? windowCode, int? matchState,
            int page, int perPage, CancellationToken ct = default);
    }
}
