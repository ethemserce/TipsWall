using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksValueBetsWriter
    {
        Task UpsertValueBetsForFixtureAsync(
            long fixtureId,
            IEnumerable<ValueBet> valueBets,
            CancellationToken cancellationToken = default);
    }
}
