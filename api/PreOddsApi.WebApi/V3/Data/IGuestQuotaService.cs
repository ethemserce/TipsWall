using System.Threading;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.V3.Data
{
    public interface IGuestQuotaService
    {
        /// <summary>
        /// Returns today's pick-count + limit for the supplied device. Cheap
        /// read — no row is created until <see cref="TryClaimAsync"/> is
        /// called. Used by the mobile client to render "X / N tahmin
        /// kalan" badges without committing a slot.
        /// </summary>
        Task<GuestQuotaStatus> GetStatusAsync(
            string deviceId,
            CancellationToken ct = default);

        /// <summary>
        /// Atomically increments today's counter when the device still has
        /// quota. Returns the post-increment state on success, or null on
        /// the quota row when the limit is already reached (caller maps
        /// that to a 429 / modal prompt).
        /// </summary>
        Task<GuestQuotaClaim> TryClaimAsync(
            string deviceId,
            CancellationToken ct = default);
    }

    public sealed class GuestQuotaStatus
    {
        public int Limit { get; init; }
        public int PicksToday { get; init; }
        public int Remaining => Limit - PicksToday;
    }

    public sealed class GuestQuotaClaim
    {
        public bool Granted { get; init; }
        public int Limit { get; init; }
        public int PicksToday { get; init; }
        public int Remaining => Limit - PicksToday;
    }
}
