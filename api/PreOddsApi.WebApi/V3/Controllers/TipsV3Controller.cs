using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [Route("api/v3/tips")]
    public sealed class TipsV3Controller : ApiControllerBase
    {
        private readonly IAppSchemaService _appSchema;

        public TipsV3Controller(IAppSchemaService appSchema)
        {
            _appSchema = appSchema;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "result_status")] string? resultStatus,
            [FromQuery(Name = "fixture_id")] long? fixtureId,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var (items, total) = await _appSchema.GetPublicTipsAsync(
                resultStatus,
                fixtureId,
                paging.NormalizedPage,
                paging.NormalizedPerPage,
                ct);

            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync(
            [FromBody] CreateTipRequest request,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Token is missing user id."));

            var outcome = await _appSchema.CreateTipAsync(userId.Value, request, ct);
            if (!outcome.Succeeded)
                return BadRequestResponse(outcome.ErrorMessage ?? "Validation failed.");

            return Ok(ApiResponse<object>.Ok(outcome.Tip!));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Token is missing user id."));

            var deleted = await _appSchema.DeleteTipAsync(userId.Value, id, ct);
            if (!deleted)
                return NotFoundResponse($"Tip {id} not found.");

            return OkResponse(new { deleted = true });
        }
    }
}
