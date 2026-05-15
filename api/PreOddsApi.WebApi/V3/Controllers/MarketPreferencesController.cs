using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [Authorize]
    [Route("api/v3/me/market-preferences")]
    public sealed class MarketPreferencesController : ApiControllerBase
    {
        // Free users may pin 5 markets, premium gets 30. Updating the cap
        // here is a single-line change — there's no DB CHECK so we can move
        // the limit without a migration.
        private const int FreeCap = 5;
        private const int PremiumCap = 30;

        private readonly IUserMarketPreferencesReader _reader;

        public MarketPreferencesController(IUserMarketPreferencesReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Invalid token."));

            var ids = await _reader.GetAsync(userId, ct);
            return OkResponse(new MarketPreferencesResponse
            {
                MarketIds = ids,
                Cap = CapFor(GetTier())
            });
        }

        [HttpPut]
        public async Task<IActionResult> ReplaceAsync(
            [FromBody] MarketPreferencesUpdate? request,
            CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(ApiResponse<object>.Fail(
                    ApiError.Codes.Unauthorized, "Invalid token."));
            if (request?.MarketIds == null)
                return BadRequestResponse("market_ids is required.");

            var cap = CapFor(GetTier());
            if (request.MarketIds.Count > cap)
                return BadRequestResponse(
                    $"Too many markets ({request.MarketIds.Count}); cap for this tier is {cap}.");

            var persisted = await _reader.ReplaceAsync(userId, request.MarketIds, ct);
            return OkResponse(new MarketPreferencesResponse
            {
                MarketIds = persisted,
                Cap = cap
            });
        }

        private bool TryGetUserId(out Guid userId)
        {
            var raw = User.FindFirst("uid")?.Value;
            return Guid.TryParse(raw, out userId);
        }

        private string GetTier()
        {
            return User.FindFirst("tier")?.Value ?? "free";
        }

        private static int CapFor(string tier)
        {
            return string.Equals(tier, "premium", StringComparison.OrdinalIgnoreCase)
                ? PremiumCap
                : FreeCap;
        }
    }

    public sealed class MarketPreferencesUpdate
    {
        public IReadOnlyList<long> MarketIds { get; set; } = Array.Empty<long>();
    }

    public sealed class MarketPreferencesResponse
    {
        public IReadOnlyList<long> MarketIds { get; set; } = Array.Empty<long>();
        public int Cap { get; set; }
    }
}
