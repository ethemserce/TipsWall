using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [Route("api/v3/hot-rate")]
    [AllowAnonymous]
    public sealed class HotRateController : ApiControllerBase
    {
        private readonly IAnalyticsReader _reader;

        public HotRateController(IAnalyticsReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "bookmaker_id")] long? bookmakerId,
            [FromQuery(Name = "market_id")] long? marketId,
            [FromQuery(Name = "window")] string? windowCode,
            [FromQuery(Name = "match_state")] int? matchState,
            [FromQuery(Name = "min_rate")] decimal? minRate,
            [FromQuery(Name = "min_winning_percent")] decimal? minWinningPercent,
            [FromQuery(Name = "min_earning_percent")] decimal? minEarningPercent,
            [FromQuery(Name = "min_sample_count")] int? minSampleCount,
            [FromQuery(Name = "fixture_date")] DateTime? fixtureDate,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var query = new RateQuery
            {
                BookmakerId = bookmakerId,
                MarketId = marketId,
                WindowCode = windowCode,
                MatchState = matchState,
                MinRate = minRate,
                MinWinningPercent = minWinningPercent,
                MinEarningPercent = minEarningPercent,
                MinSampleCount = minSampleCount,
                FixtureDate = fixtureDate,
                Page = paging.NormalizedPage,
                PerPage = paging.NormalizedPerPage,
            };

            var result = await _reader.GetHotRateAsync(query, ct);
            var payload = new RateListResponseDto
            {
                Items = result.Items,
                Summary = result.Summary,
                AsOfDate = result.AsOfDate,
            };
            return OkPagedObjectResponse(
                payload,
                paging.NormalizedPage,
                paging.NormalizedPerPage,
                result.Total);
        }
    }
}
