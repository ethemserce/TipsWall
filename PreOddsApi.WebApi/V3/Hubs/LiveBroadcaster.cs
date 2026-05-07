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

        public Task FixtureUpdatedAsync(long fixtureId, string source, object? payload = null)
        {
            var envelope = new
            {
                fixture_id = fixtureId,
                source,
                payload,
                broadcast_at = DateTimeOffset.UtcNow,
            };
            return _hub.Clients
                .Group(LiveHub.GroupName(fixtureId))
                .SendAsync("FixtureUpdated", envelope);
        }
    }
}
