using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace PreOddsApi.WebApi.V3.Hubs
{
    public sealed class LiveBroadcaster : ILiveBroadcaster
    {
        private readonly IHubContext<LiveHub> _hub;

        public LiveBroadcaster(IHubContext<LiveHub> hub)
        {
            _hub = hub;
        }

        public async Task FixtureUpdatedAsync(long fixtureId, string source, object? payload = null)
        {
            var envelope = new
            {
                fixture_id = fixtureId,
                source,
                payload,
                broadcast_at = DateTimeOffset.UtcNow,
            };
            // Per-fixture subscribers (detail screen) and the global live-
            // ticker (home screen) both get the same envelope.
            await _hub.Clients
                .Groups(LiveHub.FixtureGroup(fixtureId), LiveHub.LiveTickerGroup)
                .SendAsync("FixtureUpdated", envelope);
        }
    }
}
