namespace PreOddsApi.ExternalApis.SportMonks.Sync
{
    public interface ISportMonksSyncRunner
    {
        Task<IReadOnlyList<TItem>> GetAllAsync<TItem>(
            SportMonksSyncJobDefinition jobDefinition,
            SportMonksApiRequest request,
            string? cursorKey = null,
            CancellationToken cancellationToken = default);
    }
}
