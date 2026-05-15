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

            var tier = GetTier();
            var ids = await _reader.GetAsync(userId, ct);
            return OkResponse(new MarketPreferencesResponse
            {
                MarketIds = ids,
                Cap = CuratedMarkets.CapFor(tier),
                Tier = tier,
                Defaults = CuratedMarkets.DefaultsFor(tier),
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

            var tier = GetTier();
            var cap = CuratedMarkets.CapFor(tier);
            if (request.MarketIds.Count > cap)
                return BadRequestResponse(
                    $"Too many markets ({request.MarketIds.Count}); cap for tier '{tier}' is {cap}.");

            var persisted = await _reader.ReplaceAsync(userId, request.MarketIds, ct);
            return OkResponse(new MarketPreferencesResponse
            {
                MarketIds = persisted,
                Cap = cap,
                Tier = tier,
                Defaults = CuratedMarkets.DefaultsFor(tier),
            });
        }

        private bool TryGetUserId(out Guid userId)
        {
            var raw = User.FindFirst("uid")?.Value;
            return Guid.TryParse(raw, out userId);
        }

        private new string GetTier()
        {
            return User.FindFirst("tier")?.Value ?? "free";
        }
    }

    /// <summary>
    /// Anonymous siblings: returns the curated defaults + cap for any
    /// tier (including 'guest') so the mobile app can auto-fill the
    /// picker at first launch without an account.
    /// </summary>
    [AllowAnonymous]
    [Route("api/v3/markets/curated")]
    public sealed class CuratedMarketsController : ApiControllerBase
    {
        [HttpGet]
        public IActionResult GetAsync([FromQuery(Name = "tier")] string? tier)
        {
            var normalized = (tier ?? string.Empty).ToLowerInvariant();
            if (normalized != "guest" && normalized != "free" && normalized != "premium")
                normalized = "guest";
            return OkResponse(new MarketPreferencesResponse
            {
                MarketIds = CuratedMarkets.DefaultsFor(normalized),
                Cap = CuratedMarkets.CapFor(normalized),
                Tier = normalized,
                Defaults = CuratedMarkets.DefaultsFor(normalized),
            });
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
        public string Tier { get; set; } = "guest";
        public IReadOnlyList<long> Defaults { get; set; } = Array.Empty<long>();
    }
}
