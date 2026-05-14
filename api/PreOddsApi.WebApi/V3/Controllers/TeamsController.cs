using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [AllowAnonymous]
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

        /// <summary>
        /// Sezon istatistikleri. analytics.season_team_stats okunur,
        /// (lig × sezon) başına en güncel as_of_date alınır. season_id
        /// verilirse o sezona kısıtlar; aksi halde takımın oynadığı
        /// her ligi listeler (UI ilk satırı veya kullanıcının seçtiği
        /// satırı gösterir).
        /// </summary>
        [HttpGet("{id:long}/season-stats")]
        public async Task<IActionResult> GetSeasonStatsAsync(
            long id,
            [FromQuery(Name = "season_id")] long? seasonId,
            CancellationToken ct)
        {
            var items = await _reader.GetTeamSeasonStatsAsync(id, seasonId, ct);
            return OkResponse(items);
        }

        /// <summary>
        /// Kadro listesi. seasonId verilmezse takımın en yeni
        /// team_squads.season_id'sinden okunur. Pozisyon bazında
        /// sıralı döner (GK → DEF → MID → FWD).
        /// </summary>
        [HttpGet("{id:long}/squad")]
        public async Task<IActionResult> GetSquadAsync(
            long id,
            [FromQuery(Name = "season_id")] long? seasonId,
            CancellationToken ct)
        {
            var items = await _reader.GetTeamSquadAsync(id, seasonId, ct);
            return OkResponse(items);
        }
    }
}
