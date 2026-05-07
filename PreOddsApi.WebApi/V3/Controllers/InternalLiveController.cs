using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Hubs;

namespace PreOddsApi.WebApi.V3.Controllers
{
    /// <summary>
    /// Bridge endpoint the worker (or any internal job) calls to push a
    /// 'fixture has new data' event onto the SignalR hub. Anonymous in
    /// development so the worker can reach it without juggling JWTs;
    /// production should add a shared-secret check or move to a private
    /// network.
    /// </summary>
    [Route("api/v3/internal/live")]
    [AllowAnonymous]
    public sealed class InternalLiveController : ApiControllerBase
    {
        private readonly ILiveBroadcaster _broadcaster;

        public InternalLiveController(ILiveBroadcaster broadcaster)
        {
            _broadcaster = broadcaster;
        }

        [HttpPost("fixture/{id:long}/updated")]
        public async Task<IActionResult> FixtureUpdatedAsync(
            long id,
            [FromQuery(Name = "source")] string? source,
            [FromBody] object? payload)
        {
            await _broadcaster.FixtureUpdatedAsync(id, source ?? "manual", payload);
            return OkResponse(new { fixture_id = id, broadcast = "queued" });
        }
    }
}
