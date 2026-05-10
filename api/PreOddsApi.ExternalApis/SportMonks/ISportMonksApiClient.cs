using PreOddsApi.Entities.SportMonks;

namespace PreOddsApi.ExternalApis.SportMonks
{
    public interface ISportMonksApiClient
    {
        Task<SportMonksBase<TData>> GetAsync<TData>(
            SportMonksApiRequest request,
            CancellationToken cancellationToken = default);

        Task<TData> GetDataAsync<TData>(
            SportMonksApiRequest request,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TItem>> GetAllAsync<TItem>(
            SportMonksApiRequest request,
            CancellationToken cancellationToken = default);

        Task<string> GetRawAsync(
            SportMonksApiRequest request,
            CancellationToken cancellationToken = default);
    }
}
