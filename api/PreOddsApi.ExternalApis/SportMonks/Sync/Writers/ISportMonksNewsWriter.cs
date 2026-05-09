using PreOddsApi.Entities.SportMonks.Football;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksNewsWriter
    {
        Task UpsertNewsAsync(
            IEnumerable<News> newsItems,
            CancellationToken cancellationToken = default);

        Task UpsertFixtureNewsAsync(
            IEnumerable<Fixture> fixtures,
            CancellationToken cancellationToken = default);
    }
}
