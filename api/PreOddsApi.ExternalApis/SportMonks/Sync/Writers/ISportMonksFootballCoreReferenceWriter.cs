using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksFootballCoreReferenceWriter
    {
        Task UpsertStatesAsync(
            IEnumerable<State> states,
            CancellationToken cancellationToken = default);

        Task UpsertVenuesAsync(
            IEnumerable<Venue> venues,
            CancellationToken cancellationToken = default);

        Task UpsertTeamsWithVenuesAsync(
            IEnumerable<Team> teams,
            CancellationToken cancellationToken = default);
    }
}
