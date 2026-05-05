using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [Route("api/v3/tips")]
    public sealed class TipsV3Controller : ApiControllerBase
    {
        private readonly IAppSchemaService _appSchema;

        public TipsV3Controller(IAppSchemaService appSchema)
        {
            _appSchema = appSchema;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "result_status")] string? resultStatus,
            [FromQuery(Name = "fixture_id")] long? fixtureId,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var (items, total) = await _appSchema.GetPublicTipsAsync(
                resultStatus,
                fixtureId,
                paging.NormalizedPage,
                paging.NormalizedPerPage,
                ct);

            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }
    }
}
