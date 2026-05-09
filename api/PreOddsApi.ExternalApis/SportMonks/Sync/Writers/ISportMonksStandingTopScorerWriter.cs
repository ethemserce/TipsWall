using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksStandingTopScorerWriter
    {
        Task UpsertStandingsAsync(
            IEnumerable<Standing> standings,
            CancellationToken cancellationToken = default);

        Task UpsertTopScorersAsync(
            IEnumerable<TopScorer> topScorers,
            CancellationToken cancellationToken = default);
    }
}
