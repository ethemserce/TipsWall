using System.Threading.Tasks;

namespace PreOddsApi.WebApi.V3.Hubs
{
    public interface ILiveBroadcaster
    {
        /// <summary>
        /// Broadcasts a generic 'fixture has new data' event to all clients
        /// subscribed to the fixture's group. The mobile client uses this as a
        /// hint to invalidate its TanStack Query cache for that fixture.
        /// </summary>
        Task FixtureUpdatedAsync(long fixtureId, string source, object? payload = null);
    }
}
