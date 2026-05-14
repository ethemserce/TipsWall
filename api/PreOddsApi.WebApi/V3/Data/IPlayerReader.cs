using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface IPlayerReader
    {
        Task<PlayerDto?> GetPlayerByIdAsync(long id, CancellationToken ct = default);

        /// <summary>
        /// Player'ın oynadığı tüm (lig × sezon × takım) için en güncel
        /// season_player_stats satırını döner. seasonId verilirse o
        /// sezona kısıtlar. Liste; UI ilk satırı (en yeni) varsayılan
        /// gösterir, kullanıcı sekme ile geçmiş sezonlara dönebilir.
        /// </summary>
        Task<IReadOnlyList<PlayerSeasonStatsDto>> GetPlayerSeasonStatsAsync(
            long playerId, long? seasonId, CancellationToken ct = default);
    }
}
