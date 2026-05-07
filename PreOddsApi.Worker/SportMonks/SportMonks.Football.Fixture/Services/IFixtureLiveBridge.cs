using System.Threading;
using System.Threading.Tasks;

namespace SportMonks.Football.FixtureWorker.Services
{
    /// <summary>
    /// Posts a 'fixture has new data' event back to PreOddsApi.WebApi so it
    /// can fan out via SignalR to mobile subscribers. Best-effort: failures
    /// are swallowed so the worker loop keeps moving.
    /// </summary>
    public interface IFixtureLiveBridge
    {
        Task NotifyFixtureUpdatedAsync(long fixtureId, string source, CancellationToken cancellationToken = default);
    }
}
