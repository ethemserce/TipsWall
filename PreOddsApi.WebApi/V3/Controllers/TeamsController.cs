using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class TeamsController : ApiControllerBase
    {
        private readonly IReferenceDataReader _reader;

        public TeamsController(IReferenceDataReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "country_id")] long? countryId,
            [FromQuery(Name = "search")] string? search,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var (items, total) = await _reader.GetTeamsAsync(
                countryId, search, paging.NormalizedPage, paging.NormalizedPerPage, ct);
            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken ct)
        {
            var team = await _reader.GetTeamByIdAsync(id, ct);
            if (team == null)
                return NotFoundResponse($"Team {id} not found.");
            return OkResponse(team);
        }
    }
}
