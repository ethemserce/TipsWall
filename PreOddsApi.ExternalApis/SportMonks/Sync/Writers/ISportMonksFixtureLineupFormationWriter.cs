using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksFixtureLineupFormationWriter
    {
        Task UpsertLineupsAndFormationsAsync(
            IEnumerable<Fixture> fixtures,
            CancellationToken cancellationToken = default);
    }
}
