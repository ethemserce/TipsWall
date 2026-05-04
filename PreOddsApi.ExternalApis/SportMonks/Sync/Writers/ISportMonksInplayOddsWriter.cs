using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksInplayOddsWriter
    {
        Task UpsertInplayOddsForFixtureAsync(
            long fixtureId,
            IEnumerable<InplayOdd> odds,
            CancellationToken cancellationToken = default);
    }
}
