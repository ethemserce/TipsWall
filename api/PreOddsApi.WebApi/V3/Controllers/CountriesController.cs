using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [AllowAnonymous]
    public sealed class CountriesController : ApiControllerBase
    {
        private readonly IReferenceDataReader _reader;

        public CountriesController(IReferenceDataReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "continent_id")] long? continentId,
            [FromQuery(Name = "search")] string? search,
            [FromQuery(Name = "iso2")] string? iso2,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var (items, total) = await _reader.GetCountriesAsync(
                continentId, search, iso2, paging.NormalizedPage, paging.NormalizedPerPage, ct);
            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken ct)
        {
            var country = await _reader.GetCountryByIdAsync(id, ct);
            if (country == null)
                return NotFoundResponse($"Country {id} not found.");
            return OkResponse(country);
        }
    }
}
