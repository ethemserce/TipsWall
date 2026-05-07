using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PreOddsApi.WebApi.V3.Hubs
{
    /// <summary>
    /// SignalR hub for live fixture pushes. Clients join a fixture-specific
    /// group via JoinFixture(fixtureId) and start receiving "FixtureUpdated"
    /// events scoped to that fixture.
    /// </summary>
    [AllowAnonymous]
    public sealed class LiveHub : Hub
    {
        public Task JoinFixture(long fixtureId)
            => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(fixtureId));

        public Task LeaveFixture(long fixtureId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(fixtureId));

        public static string GroupName(long fixtureId) => $"fixture:{fixtureId}";
    }
}
