using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    public sealed class StandingsController : ApiControllerBase
    {
        private readonly IStandingsNewsReader _reader;

        public StandingsController(IStandingsNewsReader reader)
        {
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery(Name = "season_id")] long? seasonId,
            [FromQuery(Name = "league_id")] long? leagueId,
            [FromQuery(Name = "stage_id")] long? stageId,
            [FromQuery(Name = "group_id")] long? groupId,
            [FromQuery(Name = "round_id")] long? roundId,
            [FromQuery] PagedRequest paging,
            CancellationToken ct)
        {
            var (items, total) = await _reader.GetStandingsAsync(
                seasonId, leagueId, stageId, groupId, roundId,
                paging.NormalizedPage, paging.NormalizedPerPage, ct);
            return OkPagedResponse(items, paging.NormalizedPage, paging.NormalizedPerPage, total);
        }
    }
}
