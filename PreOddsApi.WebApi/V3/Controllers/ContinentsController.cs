using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class ContinentsController : ApiControllerBase
    {
        private readonly IReferenceDataReader _reader;

        public ContinentsController(IReferenceDataReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(CancellationToken ct)
        {
            var items = await _reader.GetContinentsAsync(ct);
            return OkResponse(items);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken ct)
        {
            var continent = await _reader.GetContinentByIdAsync(id, ct);
            if (continent == null)
                return NotFoundResponse($"Continent {id} not found.");
            return OkResponse(continent);
        }
    }
}
