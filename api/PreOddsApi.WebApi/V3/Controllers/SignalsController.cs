using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Controllers
{
    /// <summary>
    /// Unified analytics endpoint that replaces the legacy hot-rate /
    /// winning-rate / earning-rate trio. The same fixture_signals dataset
    /// is sorted/filtered at query time rather than physically partitioned.
    /// </summary>
    [Route("api/v3/signals")]
    [AllowAnonymous]
    [EnableRateLimiting("read-heavy")]
    public sealed class SignalsController : ApiControllerBase
    {
        private readonly IAnalyticsReader _reader;

        public SignalsController(IAnalyticsReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "bookmaker_id")] long? bookmakerId,
            [FromQuery(Name = "market_id")] long? marketId,
            [FromQuery(Name = "league_id")] long? leagueId,
            [FromQuery(Name = "window")] string? windowCode,
            [FromQuery(Name = "match_state")] int? matchState,
            [FromQuery(Name = "min_rate")] decimal? minRate,
            [FromQuery(Name = "max_rate")] decimal? maxRate,
            [FromQuery(Name = "min_winning_percent")] decimal? minWinningPercent,
            [FromQuery(Name = "min_earning_percent")] decimal? minEarningPercent,
            [FromQuery(Name = "min_sample_count")] int? minSampleCount,
            [FromQuery(Name = "fixture_date")] DateTime? fixtureDate,
            [FromQuery(Name = "value_only")] bool valueOnly,
            [FromQuery(Name = "top_per_fixture")] int? topPerFixture,
            [FromQuery(Name = "sort")] string? sort,
            [FromQuery(Name = "sort_dir")] string? sortDir,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var sortKey = (sort ?? "confidence").Trim().ToLowerInvariant() switch
            {
                "winning" => SignalSort.Winning,
                "earning" => SignalSort.Earning,
                "odd" => SignalSort.Odd,
                "edge" => SignalSort.Edge,
                _ => SignalSort.Confidence,
            };

            var query = new SignalQuery
            {
                BookmakerId = bookmakerId,
                MarketId = marketId,
                LeagueId = leagueId,
                WindowCode = windowCode,
                MatchState = matchState,
                MinRate = minRate,
                MaxRate = maxRate,
                MinWinningPercent = minWinningPercent,
                MinEarningPercent = minEarningPercent,
                MinSampleCount = minSampleCount,
                FixtureDate = fixtureDate,
                ValueOnly = valueOnly,
                TopPerFixture = topPerFixture,
                Sort = sortKey,
                SortAscending = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase),
                Page = paging.NormalizedPage,
                PerPage = paging.NormalizedPerPage,
            };

            var result = await _reader.GetSignalsAsync(query, ct);
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
