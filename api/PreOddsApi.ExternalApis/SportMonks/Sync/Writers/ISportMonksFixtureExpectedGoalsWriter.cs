using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksFixtureExpectedGoalsWriter
    {
        Task UpsertExpectedGoalsAsync(
            IEnumerable<FixtureExpectedGoals> rows,
            CancellationToken cancellationToken = default);
    }
}
