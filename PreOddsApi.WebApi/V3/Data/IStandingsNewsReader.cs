using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface IStandingsNewsReader
    {
        Task<(IReadOnlyList<StandingDto> Items, int Total)> GetStandingsAsync(
            long? seasonId,
            long? leagueId,
            long? stageId,
            long? groupId,
            long? roundId,
            int page,
            int perPage,
            CancellationToken ct = default);

        Task<(IReadOnlyList<NewsSummaryDto> Items, int Total)> GetNewsAsync(
            long? fixtureId,
            long? leagueId,
            string? type,
            int page,
            int perPage,
            CancellationToken ct = default);

        Task<NewsDetailDto?> GetNewsByIdAsync(long id, CancellationToken ct = default);
    }
}
