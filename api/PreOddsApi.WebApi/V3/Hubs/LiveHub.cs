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
            => Groups.AddToGroupAsync(Context.ConnectionId, FixtureGroup(fixtureId));

        public Task LeaveFixture(long fixtureId)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, FixtureGroup(fixtureId));

        // Global "any fixture changed" channel — used by the home screen so it
        // can refetch its day's fixture list whenever something moves.
        public Task JoinLiveTicker()
            => Groups.AddToGroupAsync(Context.ConnectionId, LiveTickerGroup);

        public Task LeaveLiveTicker()
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, LiveTickerGroup);

        public const string LiveTickerGroup = "live-ticker";
        public static string FixtureGroup(long fixtureId) => $"fixture:{fixtureId}";

        // Backwards-compat alias kept so existing callers keep compiling.
        public static string GroupName(long fixtureId) => FixtureGroup(fixtureId);
    }
}
