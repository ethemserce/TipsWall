using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksPredictionsWriter
    {
        Task UpsertPredictionsForFixtureAsync(
            long fixtureId,
            IEnumerable<PreMatchPrediction> predictions,
            CancellationToken cancellationToken = default);
    }
}
