using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    /// <summary>
    /// Endpoints that gate the daily picks a guest device can stamp into
    /// its (still-local) prediction list. The companion DB table is
    /// app.guest_pick_quotas — see 024-add-user-tier.sql.
    ///
    /// Both endpoints are [AllowAnonymous]; the contract is that the
    /// mobile client mints a stable device_id at first launch and sends
    /// it here. Logged-in users never call this controller — they get
    /// unlimited picks through their tier instead.
    /// </summary>
    [AllowAnonymous]
    [Route("api/v3/guest-quota")]
    public sealed class GuestQuotaController : ApiControllerBase
    {
        private readonly IGuestQuotaService _quota;

        public GuestQuotaController(IGuestQuotaService quota)
        {
            _quota = quota;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatusAsync(
            [FromQuery(Name = "device_id")] string? deviceId,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return BadRequestResponse("device_id is required.");

            var status = await _quota.GetStatusAsync(deviceId.Trim(), ct);
            return OkResponse(new
            {
                limit = status.Limit,
                picks_today = status.PicksToday,
                remaining = status.Remaining
            });
        }

        [HttpPost("claim")]
        public async Task<IActionResult> ClaimAsync(
            [FromBody] GuestQuotaClaimRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.DeviceId))
                return BadRequestResponse("device_id is required.");

            var result = await _quota.TryClaimAsync(request.DeviceId.Trim(), ct);
            if (!result.Granted)
            {
                // 429 reads as "rate limited" — the mobile client maps this
                // to the "upgrade for unlimited" CTA modal rather than a
                // generic error toast.
                return StatusCode(429, ApiResponse<object>.Ok(new
                {
                    granted = false,
                    limit = result.Limit,
                    picks_today = result.PicksToday,
                    remaining = result.Remaining
                }));
            }

            return OkResponse(new
            {
                granted = true,
                limit = result.Limit,
                picks_today = result.PicksToday,
                remaining = result.Remaining
            });
        }
    }
}
