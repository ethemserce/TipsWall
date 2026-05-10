using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksMatchFactsWriter
    {
        Task UpsertMatchFactsForFixtureAsync(
            long fixtureId,
            IEnumerable<MatchFact> matchFacts,
            CancellationToken cancellationToken = default);
    }
}
