using PreOddsApi.Entities.SportMonks.Football;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksFixtureMediaWeatherWriter
    {
        Task UpsertTvStationsAsync(
            IEnumerable<TvStation> tvStations,
            CancellationToken cancellationToken = default);

        Task UpsertFixtureMediaWeatherAsync(
            IEnumerable<Fixture> fixtures,
            CancellationToken cancellationToken = default);
    }
}
