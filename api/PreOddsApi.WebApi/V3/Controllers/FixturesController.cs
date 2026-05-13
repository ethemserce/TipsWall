using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [AllowAnonymous]
    [EnableRateLimiting("read-heavy")]
    public sealed class FixturesController : ApiControllerBase
    {
        private readonly IFixtureReader _reader;

        public FixturesController(IFixtureReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "date")] DateTime? date,
            [FromQuery(Name = "from_date")] DateTime? fromDate,
            [FromQuery(Name = "to_date")] DateTime? toDate,
            [FromQuery(Name = "league_id")] long? leagueId,
            [FromQuery(Name = "season_id")] long? seasonId,
            [FromQuery(Name = "team_id")] long? teamId,
            [FromQuery(Name = "state_id")] long? stateId,
            [FromQuery(Name = "status")] string? status,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
                return BadRequestResponse("from_date must be before or equal to to_date.");

            var (items, total) = await _reader.GetFixturesAsync(
                date, fromDate, toDate, leagueId, seasonId, teamId, stateId, status,
                paging.NormalizedPage, paging.NormalizedPerPage, ct);

            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken ct)
        {
            var detail = await _reader.GetFixtureByIdAsync(id, ct);
            if (detail == null)
                return NotFoundResponse($"Fixture {id} not found.");
            return OkResponse(detail);
        }

        [HttpGet("{id:long}/odds-rates")]
        public async Task<IActionResult> GetOddsRatesAsync(
            long id,
            [FromQuery(Name = "bookmaker_id")] long? bookmakerId,
            [FromQuery(Name = "market_ids")] string? marketIds,
            [FromQuery(Name = "window")] string? windowCode,
            CancellationToken ct)
        {
            if (!bookmakerId.HasValue || bookmakerId.Value <= 0)
                return BadRequestResponse("bookmaker_id is required.");

            // When market_ids isn't provided, the reader falls back to every
            // market with has_winning_calculations = true (matches the
            // analytics pipeline's filter).
            var ids = ParseMarketIds(marketIds);

            var window = string.IsNullOrWhiteSpace(windowCode) ? "all" : windowCode.Trim();

            var result = await _reader.GetFixtureOddsRatesAsync(
                id, bookmakerId.Value, ids, window, ct);
            return OkResponse(result);
        }

        [HttpGet("{id:long}/events")]
        public async Task<IActionResult> GetEventsAsync(long id, CancellationToken ct)
        {
            var items = await _reader.GetFixtureEventsAsync(id, ct);
            return OkResponse(items);
        }

        [HttpGet("{id:long}/statistics")]
        public async Task<IActionResult> GetStatisticsAsync(long id, CancellationToken ct)
        {
            var items = await _reader.GetFixtureStatisticsAsync(id, ct);
            return OkResponse(items);
        }

        [HttpGet("{id:long}/lineups")]
        public async Task<IActionResult> GetLineupsAsync(long id, CancellationToken ct)
        {
            var lineups = await _reader.GetFixtureLineupsAsync(id, ct);
            return OkResponse(lineups);
        }

        [HttpGet("{id:long}/h2h")]
        public async Task<IActionResult> GetH2HAsync(
            long id,
            [FromQuery(Name = "limit")] int? limit,
            CancellationToken ct)
        {
            var capped = limit is int l && l > 0 ? Math.Min(l, 50) : 10;
            var items = await _reader.GetFixtureH2HAsync(id, capped, ct);
            return OkResponse(items);
        }

        [HttpGet("{id:long}/trends")]
        public async Task<IActionResult> GetTrendsAsync(long id, CancellationToken ct)
        {
            var items = await _reader.GetFixtureTrendsAsync(id, ct);
            return OkResponse(items);
        }

        [HttpGet("{id:long}/match-facts")]
        public async Task<IActionResult> GetMatchFactsAsync(
            long id,
            [FromQuery(Name = "limit")] int? limit,
            CancellationToken ct)
        {
            var capped = limit is int l && l > 0 ? Math.Min(l, 200) : 50;
            var items = await _reader.GetFixtureMatchFactsAsync(id, capped, ct);
            return OkResponse(items);
        }

        [HttpGet("{id:long}/weather")]
        public async Task<IActionResult> GetWeatherAsync(long id, CancellationToken ct)
        {
            var item = await _reader.GetFixtureWeatherAsync(id, ct);
            return OkResponse(item);
        }

        [HttpGet("{id:long}/tv-stations")]
        public async Task<IActionResult> GetTvStationsAsync(
            long id,
            [FromQuery(Name = "country_iso")] string? countryIso,
            CancellationToken ct)
        {
            var items = await _reader.GetFixtureTvStationsAsync(id, countryIso, ct);
            return OkResponse(items);
        }

        [HttpGet("{id:long}/value-bets")]
        public async Task<IActionResult> GetValueBetsAsync(long id, CancellationToken ct)
        {
            var items = await _reader.GetFixtureValueBetsAsync(id, ct);
            return OkResponse(items);
        }

        [HttpGet("{id:long}/expected-goals")]
        public async Task<IActionResult> GetExpectedGoalsAsync(long id, CancellationToken ct)
        {
            var item = await _reader.GetFixtureExpectedGoalsAsync(id, ct);
            return OkResponse(item);
        }

        [HttpGet("{id:long}/sidelined")]
        public async Task<IActionResult> GetSidelinedAsync(long id, CancellationToken ct)
        {
            var item = await _reader.GetFixtureSidelinedAsync(id, ct);
            return OkResponse(item);
        }

        private static IReadOnlyList<long> ParseMarketIds(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<long>();
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => long.TryParse(part, out var v) ? v : 0L)
                .Where(v => v > 0)
                .Distinct()
                .ToList();
        }
    }
}
