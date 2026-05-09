using PreOddsApi.Entities.SportMonks.Core.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksCatalogReferenceWriter
    {
        Task UpsertContinentsAsync(
            IEnumerable<Continent> continents,
            CancellationToken cancellationToken = default);

        Task UpsertCountriesWithRegionsAndCitiesAsync(
            IEnumerable<Country> countries,
            CancellationToken cancellationToken = default);

        Task UpsertTypesAsync(
            IEnumerable<Types> types,
            CancellationToken cancellationToken = default);

        Task UpsertSportsAsync(
            IEnumerable<Sport> sports,
            CancellationToken cancellationToken = default);
    }
}
