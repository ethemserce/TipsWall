using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class PreferencesController : ApiControllerBase
    {
        private readonly IUserDataService _userData;

        public PreferencesController(IUserDataService userData)
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

            var prefs = await _userData.GetPreferencesAsync(userId.Value, ct);
            return OkResponse(prefs);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAsync(
            [FromBody] UpdateUserPreferencesRequest request,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Token is missing user id."));

            var outcome = await _userData.UpsertPreferencesAsync(userId.Value, request, ct);
            if (!outcome.Succeeded)
                return BadRequestResponse(outcome.ErrorMessage ?? "Validation failed.");

            return OkResponse(outcome.Preferences!);
        }
    }
}
