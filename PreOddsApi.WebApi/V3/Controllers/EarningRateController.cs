using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [Route("api/v3/earning-rate")]
    [AllowAnonymous]
    public sealed class EarningRateController : ApiControllerBase
    {
        private readonly IAnalyticsReader _reader;

        public EarningRateController(IAnalyticsReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "bookmaker_id")] long? bookmakerId,
            [FromQuery(Name = "market_id")] long? marketId,
            [FromQuery(Name = "window")] string? windowCode,
            [FromQuery(Name = "match_state")] int? matchState,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var (items, total) = await _reader.GetEarningRateAsync(
                bookmakerId, marketId, windowCode, matchState,
                paging.NormalizedPage, paging.NormalizedPerPage, ct);
            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }
    }
}
