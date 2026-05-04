using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksTransferSidelinedWriter
    {
        Task UpsertTransfersAsync(
            IEnumerable<Transfer> transfers,
            CancellationToken cancellationToken = default);

        Task UpsertFixtureSidelinedAsync(
            IEnumerable<Fixture> fixtures,
            CancellationToken cancellationToken = default);
    }
}
