using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class LeaguesController : ApiControllerBase
    {
        private readonly IReferenceDataReader _reader;

        public LeaguesController(IReferenceDataReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "country_id")] long? countryId,
            [FromQuery(Name = "active")] bool? active,
            [FromQuery(Name = "search")] string? search,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var (items, total) = await _reader.GetLeaguesAsync(
                countryId, active, search, paging.NormalizedPage, paging.NormalizedPerPage, ct);
            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken ct)
        {
            var league = await _reader.GetLeagueByIdAsync(id, ct);
            if (league == null)
                return NotFoundResponse($"League {id} not found.");
            return OkResponse(league);
        }
    }
}
