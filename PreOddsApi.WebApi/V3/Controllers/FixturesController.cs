using System;
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
    }
}
