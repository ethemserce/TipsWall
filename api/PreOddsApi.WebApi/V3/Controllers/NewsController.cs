using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [AllowAnonymous]
    public sealed class NewsController : ApiControllerBase
    {
        private readonly IStandingsNewsReader _reader;

        public NewsController(IStandingsNewsReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "fixture_id")] long? fixtureId,
            [FromQuery(Name = "league_id")] long? leagueId,
            [FromQuery(Name = "type")] string? type,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var (items, total) = await _reader.GetNewsAsync(
                fixtureId, leagueId, type,
                paging.NormalizedPage, paging.NormalizedPerPage, ct);
            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken ct)
        {
            var detail = await _reader.GetNewsByIdAsync(id, ct);
            if (detail == null)
                return NotFoundResponse($"News {id} not found.");
            return OkResponse(detail);
        }
    }
}
