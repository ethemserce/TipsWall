using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class FeaturedFixturesController : ApiControllerBase
    {
        private readonly IAppSchemaService _appSchema;

        public FeaturedFixturesController(IAppSchemaService appSchema)
        {
            _appSchema = appSchema;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "date")] DateTime? featureDate,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var (items, total) = await _appSchema.GetFeaturedFixturesAsync(
                featureDate,
                paging.NormalizedPage,
                paging.NormalizedPerPage,
                ct);

            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }
    }
}
