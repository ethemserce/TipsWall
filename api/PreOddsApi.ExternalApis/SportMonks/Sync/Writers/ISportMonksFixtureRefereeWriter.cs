using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksFixtureRefereeWriter
    {
        Task UpsertFixtureRefereesAsync(
            IEnumerable<Fixture> fixtures,
            CancellationToken cancellationToken = default);
    }
}
