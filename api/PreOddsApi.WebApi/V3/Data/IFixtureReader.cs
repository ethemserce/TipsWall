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

        Task<IReadOnlyList<FixtureOddsRatesDto>> GetFixtureOddsRatesAsync(
            long fixtureId,
            long bookmakerId,
            IReadOnlyList<long> marketIds,
            string windowCode,
            CancellationToken ct = default);

        Task<IReadOnlyList<FixtureEventDto>> GetFixtureEventsAsync(
            long fixtureId,
            CancellationToken ct = default);

        Task<IReadOnlyList<FixtureStatisticDto>> GetFixtureStatisticsAsync(
            long fixtureId,
            CancellationToken ct = default);

        Task<FixtureLineupsDto> GetFixtureLineupsAsync(
            long fixtureId,
            CancellationToken ct = default);

        Task<IReadOnlyList<FixtureSummaryDto>> GetFixtureH2HAsync(
            long fixtureId,
            int limit,
            CancellationToken ct = default);

        Task<IReadOnlyList<FixtureTrendDto>> GetFixtureTrendsAsync(
            long fixtureId,
            CancellationToken ct = default);

        Task<IReadOnlyList<FixtureMatchFactDto>> GetFixtureMatchFactsAsync(
            long fixtureId,
            int limit,
            CancellationToken ct = default);

        Task<FixtureWeatherDto?> GetFixtureWeatherAsync(
            long fixtureId,
            CancellationToken ct = default);

        Task<IReadOnlyList<FixtureTvStationDto>> GetFixtureTvStationsAsync(
            long fixtureId,
            CancellationToken ct = default);

        Task<IReadOnlyList<FixtureValueBetDto>> GetFixtureValueBetsAsync(
            long fixtureId,
            CancellationToken ct = default);

        Task<FixtureExpectedGoalsDto?> GetFixtureExpectedGoalsAsync(
            long fixtureId,
            CancellationToken ct = default);

        Task<FixtureSidelinedDto> GetFixtureSidelinedAsync(
            long fixtureId,
            CancellationToken ct = default);
    }
}
