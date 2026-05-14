using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PreOddsApi.WebApi.V3.Data;

namespace PreOddsApi.WebApi.V3.Controllers
{
    [AllowAnonymous]
    [EnableRateLimiting("read-heavy")]
    public sealed class PlayersController : ApiControllerBase
    {
        private readonly IPlayerReader _reader;

        public PlayersController(IPlayerReader reader)
        {
            _reader = reader;
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken ct)
        {
            var player = await _reader.GetPlayerByIdAsync(id, ct);
            if (player == null)
                return NotFoundResponse($"Player {id} not found.");
            return OkResponse(player);
        }

        /// <summary>
        /// Sezon istatistikleri. analytics.season_player_stats okunur,
        /// (lig × sezon × takım) başına en güncel as_of_date alınır.
        /// season_id verilirse o sezona kısıtlar.
        /// </summary>
        [HttpGet("{id:long}/season-stats")]
        public async Task<IActionResult> GetSeasonStatsAsync(
            long id,
            [FromQuery(Name = "season_id")] long? seasonId,
            CancellationToken ct)
        {
            var items = await _reader.GetPlayerSeasonStatsAsync(id, seasonId, ct);
            return OkResponse(items);
        }
    }
}
