using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class DevicesController : ApiControllerBase
    {
        private readonly IUserDataService _userData;

        public DevicesController(IUserDataService userData)
        {
            _userData = userData;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Token is missing user id."));

            var items = await _userData.GetDevicesAsync(userId.Value, ct);
            return OkResponse(items);
        }

        [HttpPost]
        public async Task<IActionResult> RegisterAsync(
            [FromBody] RegisterDeviceRequest request,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Token is missing user id."));

            var outcome = await _userData.RegisterDeviceAsync(userId.Value, request, ct);
            if (!outcome.Succeeded)
            {
                if (outcome.ErrorCode == "CONFLICT")
                    return Conflict(ApiResponse<object>.Fail(
                        outcome.ErrorCode!, outcome.ErrorMessage ?? "Conflict."));
                return BadRequestResponse(outcome.ErrorMessage ?? "Validation failed.");
            }

            return Ok(ApiResponse<object>.Ok(outcome.Device!));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> RevokeAsync(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Token is missing user id."));

            var revoked = await _userData.RevokeDeviceAsync(userId.Value, id, ct);
            if (!revoked)
                return NotFoundResponse($"Device {id} not found.");

            return OkResponse(new { revoked = true });
        }
    }
}
