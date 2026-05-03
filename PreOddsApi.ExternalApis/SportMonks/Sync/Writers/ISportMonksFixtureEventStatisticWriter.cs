using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksFixtureEventStatisticWriter
    {
        Task UpsertEventsAndStatisticsAsync(
            IEnumerable<Fixture> fixtures,
            CancellationToken cancellationToken = default);
    }
}
