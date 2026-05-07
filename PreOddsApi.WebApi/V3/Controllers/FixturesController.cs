using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [AllowAnonymous]
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
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
                return BadRequestResponse("from_date must be before or equal to to_date.");

            var (items, total) = await _reader.GetFixturesAsync(
                date, fromDate, toDate, leagueId, seasonId, teamId, stateId,
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

            var ids = ParseMarketIds(marketIds);
            if (ids.Count == 0)
                return BadRequestResponse("market_ids is required (comma-separated, e.g. 1,52,80,31).");

            var window = string.IsNullOrWhiteSpace(windowCode) ? "all" : windowCode.Trim();

            var result = await _reader.GetFixtureOddsRatesAsync(
                id, bookmakerId.Value, ids, window, ct);
            return OkResponse(result);
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
