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
    }
}
