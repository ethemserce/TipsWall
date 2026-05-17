using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Admin;

namespace PreOddsApi.WebApi.V3.Controllers
{
    /// <summary>
    /// Operational dashboard endpoints — worker tier status + Postgres
    /// health. Read-only, gated on the AdminOnly policy (admin JWT
    /// claim). Powers the web/ admin dashboard's auto-refreshing
    /// /ops page; safe to poll every 10s.
    /// </summary>
    [Authorize(Policy = "AdminOnly")]
    [Route("api/v3/admin/ops")]
    public sealed class AdminOpsController : ApiControllerBase
    {
        private readonly IAdminOpsReader _reader;

        public AdminOpsController(IAdminOpsReader reader)
        {
            _reader = reader;
        }

        /// <summary>
        /// Last-run snapshot for every worker job key seen in the last
        /// 24 hours. Drives the "live / pulse / nightly tier health"
        /// widget.
        /// </summary>
        [HttpGet("workers")]
        public async Task<IActionResult> GetWorkersAsync(CancellationToken ct)
        {
            var items = await _reader.GetWorkerTierStatusAsync(ct);
            return OkResponse(items);
        }

        /// <summary>
        /// Postgres health snapshot — active query count, longest
        /// runtime, recovery state, DB size. Drives the "DB pulse"
        /// widget that catches lockups before they freeze the mobile
        /// live tile.
        /// </summary>
        [HttpGet("postgres")]
        public async Task<IActionResult> GetPostgresAsync(CancellationToken ct)
        {
            var snapshot = await _reader.GetPostgresHealthAsync(ct);
            return OkResponse(snapshot);
        }
    }
}
