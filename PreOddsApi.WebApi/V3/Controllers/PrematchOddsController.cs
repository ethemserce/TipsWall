using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class PrematchOddsController : ApiControllerBase
    {
        private readonly IOddsReader _reader;

        public PrematchOddsController(IOddsReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentAsync(
            [FromQuery(Name = "fixture_id")] long? fixtureId,
            [FromQuery(Name = "bookmaker_id")] long? bookmakerId,
            [FromQuery(Name = "market_id")] long? marketId,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            if (!fixtureId.HasValue || fixtureId.Value <= 0)
                return BadRequestResponse("fixture_id is required.");

            var (items, total) = await _reader.GetPrematchOddsAsync(
                fixtureId.Value,
                bookmakerId,
                marketId,
                paging.NormalizedPage,
                paging.NormalizedPerPage,
                ct);

            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistoryAsync(
            [FromQuery(Name = "fixture_id")] long? fixtureId,
            [FromQuery(Name = "bookmaker_id")] long? bookmakerId,
            [FromQuery(Name = "market_id")] long? marketId,
            [FromQuery(Name = "outcome_key")] string? outcomeKey,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            if (!fixtureId.HasValue || fixtureId.Value <= 0)
                return BadRequestResponse("fixture_id is required.");

            var (items, total) = await _reader.GetPrematchHistoryAsync(
                fixtureId.Value,
                bookmakerId,
                marketId,
                outcomeKey,
                paging.NormalizedPage,
                paging.NormalizedPerPage,
                ct);

            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }
    }
}
