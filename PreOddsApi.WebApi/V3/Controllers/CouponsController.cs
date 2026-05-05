using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class CouponsController : ApiControllerBase
    {
        private readonly IAppSchemaService _appSchema;

        public CouponsController(IAppSchemaService appSchema)
        {
            _appSchema = appSchema;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "status")] string? status,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var (items, total) = await _appSchema.GetPublicCouponsAsync(
                status,
                paging.NormalizedPage,
                paging.NormalizedPerPage,
                ct);

            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }

        [HttpGet("{publicCode}")]
        public async Task<IActionResult> GetByCodeAsync(string publicCode, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(publicCode))
                return BadRequestResponse("publicCode is required.");

            var detail = await _appSchema.GetCouponByPublicCodeAsync(publicCode.Trim(), ct);
            if (detail == null)
                return NotFoundResponse($"Coupon '{publicCode}' not found.");

            return OkResponse(detail);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync(
            [FromBody] CreateCouponRequest request,
            CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Token is missing user id."));

            var outcome = await _appSchema.CreateCouponAsync(userId.Value, request, ct);
            if (!outcome.Succeeded)
            {
                if (outcome.ErrorCode == "CONFLICT")
                    return Conflict(ApiResponse<object>.Fail(
                        outcome.ErrorCode!, outcome.ErrorMessage ?? "Conflict."));
                return BadRequestResponse(outcome.ErrorMessage ?? "Validation failed.");
            }

            return Ok(ApiResponse<object>.Ok(outcome.Coupon!));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Token is missing user id."));

            var deleted = await _appSchema.DeleteCouponAsync(userId.Value, id, ct);
            if (!deleted)
                return NotFoundResponse($"Coupon {id} not found.");

            return OkResponse(new { deleted = true });
        }
    }
}
