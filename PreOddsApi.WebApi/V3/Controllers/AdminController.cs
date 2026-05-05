using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class AdminController : ApiControllerBase
    {
        private const int DefaultRecentRequestsLimit = 50;
        private const int MaxRecentRequestsLimit = 500;

        private readonly ISyncDiagnostics _diagnostics;

        public AdminController(ISyncDiagnostics diagnostics)
        {
            _diagnostics = diagnostics;
        }

        [HttpGet("sync-status")]
        public async Task<IActionResult> GetSyncStatusAsync(CancellationToken ct)
        {
            var items = await _diagnostics.GetSyncStatusAsync(ct);
            return OkResponse(items);
        }

        [HttpGet("recent-requests")]
        public async Task<IActionResult> GetRecentRequestsAsync(
            [FromQuery(Name = "limit")] int? limit,
            CancellationToken ct)
        {
            var clamped = System.Math.Clamp(
                limit ?? DefaultRecentRequestsLimit,
                1,
                MaxRecentRequestsLimit);

            var items = await _diagnostics.GetRecentRequestsAsync(clamped, ct);
            return OkResponse(items);
        }
    }
}
