using PreOddsApi.Entities.SportMonks.Odds.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksOddsReferenceWriter
    {
        Task UpsertMarketsAsync(
            IEnumerable<Market> markets,
            CancellationToken cancellationToken = default);

        Task UpsertBookmakersAsync(
            IEnumerable<Bookmaker> bookmakers,
            CancellationToken cancellationToken = default);
    }
}
