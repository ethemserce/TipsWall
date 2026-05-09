using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksPrematchOddsWriter
    {
        Task UpsertPrematchOddsAsync(
            IEnumerable<Fixture> fixtures,
            CancellationToken cancellationToken = default);

        Task UpsertPrematchOddsForFixtureAsync(
            long fixtureId,
            IEnumerable<PreMatchOdd> odds,
            CancellationToken cancellationToken = default);
    }
}
