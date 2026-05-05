using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class MarketsController : ApiControllerBase
    {
        private readonly IReferenceDataReader _reader;

        public MarketsController(IReferenceDataReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "active")] bool? active,
            [FromQuery(Name = "search")] string? search,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var (items, total) = await _reader.GetMarketsAsync(
                active, search, paging.NormalizedPage, paging.NormalizedPerPage, ct);
            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }
    }
}
