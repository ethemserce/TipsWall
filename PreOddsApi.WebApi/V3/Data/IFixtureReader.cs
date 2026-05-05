using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface IFixtureReader
    {
        Task<(IReadOnlyList<FixtureSummaryDto> Items, int Total)> GetFixturesAsync(
            DateTime? date,
            DateTime? fromDate,
            DateTime? toDate,
            long? leagueId,
            long? seasonId,
            long? teamId,
            long? stateId,
            int page,
            int perPage,
            CancellationToken ct = default);

        Task<FixtureDetailDto?> GetFixtureByIdAsync(long id, CancellationToken ct = default);
    }
}
