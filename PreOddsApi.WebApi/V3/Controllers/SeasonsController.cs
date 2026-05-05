using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class SeasonsController : ApiControllerBase
    {
        private readonly IReferenceDataReader _reader;

        public SeasonsController(IReferenceDataReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "league_id")] long? leagueId,
            [FromQuery(Name = "is_current")] bool? isCurrent,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var (items, total) = await _reader.GetSeasonsAsync(
                leagueId, isCurrent, paging.NormalizedPage, paging.NormalizedPerPage, ct);
            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken ct)
        {
            var season = await _reader.GetSeasonByIdAsync(id, ct);
            if (season == null)
                return NotFoundResponse($"Season {id} not found.");
            return OkResponse(season);
        }
    }
}
