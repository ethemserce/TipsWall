using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class FavoritesController : ApiControllerBase
    {
        private readonly IUserDataService _userData;

        public FavoritesController(IUserDataService userData)
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

            var items = await _userData.GetFavoritesAsync(userId.Value, ct);
            return OkResponse(items);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync(
            [FromBody] CreateFavoriteRequest request,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Token is missing user id."));

            var outcome = await _userData.CreateFavoriteAsync(userId.Value, request, ct);
            if (!outcome.Succeeded)
            {
                if (outcome.ErrorCode == FavoriteOutcome.ErrorCodes.Conflict)
                    return Conflict(ApiResponse<object>.Fail(
                        outcome.ErrorCode!, outcome.ErrorMessage ?? "Conflict."));
                return BadRequestResponse(outcome.ErrorMessage ?? "Validation failed.");
            }

            return Ok(ApiResponse<object>.Ok(outcome.Favorite!));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Token is missing user id."));

            var deleted = await _userData.DeleteFavoriteAsync(userId.Value, id, ct);
            if (!deleted)
                return NotFoundResponse($"Favorite {id} not found.");

            return OkResponse(new { deleted = true });
        }
    }
}
