using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public interface ISportMonksCompetitionReferenceWriter
    {
        Task UpsertLeaguesWithHierarchyAsync(
            IEnumerable<League> leagues,
            CancellationToken cancellationToken = default);
    }
}
