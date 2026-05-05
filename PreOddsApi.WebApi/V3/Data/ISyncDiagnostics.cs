using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface ISyncDiagnostics
    {
        Task<IReadOnlyList<SyncJobCursorDto>> GetSyncStatusAsync(CancellationToken ct = default);

        Task<IReadOnlyList<ApiRequestSummaryDto>> GetRecentRequestsAsync(
            int limit,
            CancellationToken ct = default);
    }
}
